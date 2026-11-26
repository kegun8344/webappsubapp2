using System.Security.Claims;
using api.DAL;
using api.DTOs;
using api.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace api.Controllers
{
    /*
     * Ranks all students by their points, all time or this week.
     * Any signed-in user can see it. Same rules as LeaderboardController in Subapp 1.
     */
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class LeaderboardController : ControllerBase
    {
        /* How many badges a row shows */
        private const int MaxBadgesShown = 4;

        private readonly IPointRepository pointRepository;
        private readonly TimeProvider timeProvider;
        private readonly ILogger<LeaderboardController> logger;

        /* TimeProvider instead of DateTime.Now, so tests can pretend it is another day */
        public LeaderboardController(
            IPointRepository pointRepo,
            TimeProvider clock,
            ILogger<LeaderboardController> leaderboardLogger)
        {
            pointRepository = pointRepo;
            timeProvider = clock;
            logger = leaderboardLogger;
        }

        // GET: /api/leaderboard?period=week
        // "week" counts points since Monday (Norwegian time), anything else counts all time.
        [HttpGet]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<ActionResult<LeaderboardDto>> Get(string? period = null)
        {
            var weekly = string.Equals(period, "week", StringComparison.OrdinalIgnoreCase);
            var today = PointRules.NorwegianDate(timeProvider.GetUtcNow().UtcDateTime);
            var weekStart = PointRules.StartOfWeek(today);

            var students = await pointRepository.GetLeaderboardAsync(weekly ? weekStart : null);

            if (students == null)
            {
                logger.LogError("The leaderboard could not be loaded.");
                return Problem("The leaderboard could not be loaded.", statusCode: StatusCodes.Status500InternalServerError);
            }

            /* Badges and streaks are a bonus. If they fail, the list is still shown without them. */
            var activity = await pointRepository.GetStudentActivityAsync();

            if (activity == null)
            {
                logger.LogWarning("The badges for the leaderboard could not be loaded.");
                activity = new Dictionary<string, StudentActivity>();
            }

            var currentUserId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var topPoints = students.Count == 0 ? 0 : students.Max(student => student.PeriodPoints);

            var entries = new List<LeaderboardEntryDto>();
            var statsByEntry = new List<BadgeStats>();

            for (var i = 0; i < students.Count; i++)
            {
                var student = students[i];
                var studentActivity = activity.GetValueOrDefault(student.UserId);
                var streakDays = studentActivity?.StreakDays ?? new List<DateOnly>();

                var stats = BadgeStatsBuilder.Build(
                    studentActivity?.Attempts ?? new List<AttemptRow>(),
                    streakDays,
                    studentActivity?.DailyGoalDays ?? 0);

                var unlocked = BadgeRules.GetBadges(stats)
                    .Where(badge => !badge.IsLocked)
                    .OrderByDescending(badge => badge.Tier)
                    .ThenBy(badge => badge.Type)
                    .ToList();

                var displayName = DisplayNames.FromEmail(student.UserName);

                /* The same points as the student above gives the same rank */
                var rank = i > 0 && student.PeriodPoints == students[i - 1].PeriodPoints
                    ? entries[i - 1].Rank
                    : i + 1;

                entries.Add(new LeaderboardEntryDto
                {
                    Rank = rank,
                    DisplayName = displayName,
                    Initial = displayName[..1].ToUpperInvariant(),
                    AvatarColor = DisplayNames.AvatarColor(displayName),
                    Points = student.PeriodPoints,
                    BarPercent = topPoints == 0 ? 0 : student.PeriodPoints * 100 / topPoints,
                    Level = LevelCalculator.GetLevel(student.AllTimePoints).Level,
                    CurrentStreak = PointRules.CurrentStreak(streakDays, today),
                    BadgeCount = unlocked.Count,
                    TopBadges = unlocked.Take(MaxBadgesShown).Select(BadgeDto.FromProgress).ToList(),
                    IsCurrentUser = student.UserId == currentUserId
                });

                statsByEntry.Add(stats);
            }

            var result = new LeaderboardDto
            {
                IsWeekly = weekly,
                WeekStart = weekStart,
                Entries = entries,
                Highlights = BuildHighlights(entries, statsByEntry)
            };

            var currentIndex = entries.FindIndex(entry => entry.IsCurrentUser);

            if (currentIndex >= 0)
            {
                result.Standing = BuildStanding(entries, currentIndex, students[currentIndex].AllTimePoints);
            }

            return result;
        }

        /* The signed-in student's rank, level progress and the gap to the student right above */
        private static LeaderboardStandingDto BuildStanding(List<LeaderboardEntryDto> entries, int currentIndex, int allTimePoints)
        {
            var current = entries[currentIndex];
            var (level, pointsIntoLevel, pointsForLevel) = LevelCalculator.GetLevel(allTimePoints);

            /* The list is sorted, so the last entry with more points is the one right above */
            var above = entries.LastOrDefault(entry => entry.Points > current.Points);

            return new LeaderboardStandingDto
            {
                Rank = current.Rank,
                StudentCount = entries.Count,
                Points = current.Points,
                Level = level,
                LevelProgressPercent = pointsForLevel == 0 ? 0 : pointsIntoLevel * 100 / pointsForLevel,
                PointsToNextLevel = pointsForLevel - pointsIntoLevel,
                NextPlaceName = above?.DisplayName,
                PointsBehindNextPlace = above == null ? 0 : above.Points - current.Points
            };
        }

        /* Records among the students: longest streak, most perfect quizzes, most badges */
        private static List<LeaderboardHighlightDto> BuildHighlights(List<LeaderboardEntryDto> entries, List<BadgeStats> stats)
        {
            var highlights = new List<LeaderboardHighlightDto>();

            AddHighlight(highlights, entries, stats.Select(s => s.LongestStreak).ToList(), "Longest streak", "bi-fire", "days in a row");
            AddHighlight(highlights, entries, stats.Select(s => s.PerfectQuizzes).ToList(), "Sharpshooter", "bi-bullseye", "perfect quizzes");
            AddHighlight(highlights, entries, entries.Select(e => e.BadgeCount).ToList(), "Badge collector", "bi-award", "badges");

            return highlights;
        }

        /* Adds the student with the highest value. On a tie the student with the most points wins (they come first). */
        private static void AddHighlight(
            List<LeaderboardHighlightDto> highlights,
            List<LeaderboardEntryDto> entries,
            List<int> values,
            string title,
            string icon,
            string unit)
        {
            if (values.Count == 0 || values.Max() <= 0)
            {
                return;
            }

            var best = values.IndexOf(values.Max());

            highlights.Add(new LeaderboardHighlightDto
            {
                Title = title,
                Icon = icon,
                Name = entries[best].DisplayName,
                Value = values[best],
                Unit = unit
            });
        }
    }
}