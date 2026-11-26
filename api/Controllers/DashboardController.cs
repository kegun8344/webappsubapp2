using System.Security.Claims;
using api.DAL;
using api.DTOs;
using api.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace api.Controllers
{
    /* The student's own dashboard: points, level, streak, rank, recent quizzes and badges */
    [ApiController]
    [Route("api/[controller]")]
    [Authorize(Roles = "Student")]
    public class DashboardController : ControllerBase
    {
        private const int RecentAttemptCount = 5;

        private readonly IPointRepository pointRepository;
        private readonly IBadgeService badgeService;
        private readonly TimeProvider timeProvider;
        private readonly ILogger<DashboardController> logger;

        public DashboardController(
            IPointRepository pointRepo,
            IBadgeService badges,
            TimeProvider clock,
            ILogger<DashboardController> dashboardLogger)
        {
            pointRepository = pointRepo;
            badgeService = badges;
            timeProvider = clock;
            logger = dashboardLogger;
        }

        // GET: /api/dashboard
        [HttpGet]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<ActionResult<DashboardDto>> Get()
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            if (userId == null)
            {
                return Unauthorized();
            }

            /* If a database call fails the repository logs it and returns null */
            var totalPoints = await pointRepository.GetTotalPointsAsync(userId);
            var streakDays = await pointRepository.GetStreakDaysAsync(userId);
            var bestScores = await pointRepository.GetBestScoresAsync(userId);
            var recentAttempts = await pointRepository.GetRecentAttemptsAsync(userId, RecentAttemptCount);

            if (totalPoints == null || streakDays == null || bestScores == null || recentAttempts == null)
            {
                logger.LogError("The dashboard could not be loaded for user {UserId}.", userId);
                return Problem("The dashboard could not be loaded.", statusCode: StatusCodes.Status500InternalServerError);
            }

            /* Rank: 1 + the number of students with more points, the same rule as the leaderboard */
            var rank = await pointRepository.GetStudentRankAsync(totalPoints.Value);

            if (rank == null)
            {
                logger.LogError("The dashboard rank could not be loaded for user {UserId}.", userId);
                return Problem("The dashboard could not be loaded.", statusCode: StatusCodes.Status500InternalServerError);
            }

            var today = PointRules.NorwegianDate(timeProvider.GetUtcNow().UtcDateTime);
            var (level, pointsIntoLevel, pointsForLevel) = LevelCalculator.GetLevel(totalPoints.Value);

            var dashboard = new DashboardDto
            {
                /* The token has the email, not a user name */
                DisplayName = DisplayNames.FromEmail(User.FindFirstValue(ClaimTypes.Email)),
                TotalPoints = totalPoints.Value,
                Level = level,
                PointsIntoLevel = pointsIntoLevel,
                PointsForLevel = pointsForLevel,
                StreakDays = PointRules.CurrentStreak(streakDays, today),
                QuizzesCompleted = bestScores.Count,
                AverageScore = bestScores.Count == 0 ? null : (int)Math.Round(bestScores.Average()),
                LeaderboardRank = rank.Rank,
                StudentCount = rank.StudentCount,
                RecentAttempts = recentAttempts
                    .Select(attempt => new RecentAttemptDto
                    {
                        QuizId = attempt.QuizId,
                        QuizTitle = attempt.QuizTitle,
                        Score = attempt.Score,
                        CompletedAt = attempt.CompletedAt,
                        AwardsPoints = attempt.AwardsPoints
                    })
                    .ToList()
            };

            /* Badges are a bonus. If they fail, the rest of the dashboard is still shown. */
            var badges = await badgeService.GetBadgesAsync(userId);

            if (badges == null)
            {
                logger.LogWarning("The badges could not be loaded for user {UserId}.", userId);
            }
            else
            {
                dashboard.Badges = badges.Select(BadgeProgressDto.FromProgress).ToList();
            }

            return dashboard;
        }
    }
}