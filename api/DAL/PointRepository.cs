using api.Data;
using api.Models;
using api.Services;
using Microsoft.EntityFrameworkCore;

namespace api.DAL
{
    public class PointRepository : IPointRepository
    {
        private readonly ApplicationDbContext _db;
        private readonly ILogger<PointRepository> _logger;

        public PointRepository(ApplicationDbContext db, ILogger<PointRepository> logger)
        {
            _db = db;
            _logger = logger;
        }

        public async Task<int?> GetTotalPointsAsync(string userId)
        {
            try
            {
                return await _db.PointAwards
                    .AsNoTracking()
                    .Where(p => p.UserId == userId)
                    .SumAsync(p => (int?)p.Points) ?? 0;
            }
            catch (Exception e)
            {
                _logger.LogError(e, "[PointRepository] Could not load the points of user {UserId}.", userId);
                return null;
            }
        }

        public async Task<List<DateOnly>?> GetStreakDaysAsync(string userId)
        {
            try
            {
                return await _db.PointAwards
                    .AsNoTracking()
                    .Where(p => p.UserId == userId && p.Reason == PointReason.Streak)
                    .Select(p => p.AwardedOn)
                    .ToListAsync();
            }
            catch (Exception e)
            {
                _logger.LogError(e, "[PointRepository] Could not load the streak days of user {UserId}.", userId);
                return null;
            }
        }

        public async Task<List<int>?> GetBestScoresAsync(string userId)
        {
            try
            {
                return await _db.QuizAttempts
                    .AsNoTracking()
                    .Where(a => a.UserId == userId)
                    .GroupBy(a => a.QuizId)
                    .Select(g => g.Max(a => a.Score))
                    .ToListAsync();
            }
            catch (Exception e)
            {
                _logger.LogError(e, "[PointRepository] Could not load the best scores of user {UserId}.", userId);
                return null;
            }
        }

        public async Task<List<AttemptSummary>?> GetRecentAttemptsAsync(string userId, int count)
        {
            try
            {
                return await _db.QuizAttempts
                    .AsNoTracking()
                    .Where(a => a.UserId == userId)
                    .OrderByDescending(a => a.CompletedAt)
                    .Take(count)
                    .Select(a => new AttemptSummary(
                        a.QuizId,
                        a.Quiz.Title,
                        a.Score,
                        a.CompletedAt,
                        a.Quiz.AwardsPoints))
                    .ToListAsync();
            }
            catch (Exception e)
            {
                _logger.LogError(e, "[PointRepository] Could not load the recent attempts of user {UserId}.", userId);
                return null;
            }
        }

        public async Task<List<(int QuizId, int Score)>?> GetAttemptsTodayAsync(string userId, DateTime startUtc, DateTime endUtc)
        {
            try
            {
                var attempts = await _db.QuizAttempts
                    .AsNoTracking()
                    .Where(a => a.UserId == userId && a.Quiz.AwardsPoints &&
                                a.CompletedAt >= startUtc && a.CompletedAt < endUtc)
                    .Select(a => new { a.QuizId, a.Score })
                    .ToListAsync();

                return attempts.Select(a => (a.QuizId, a.Score)).ToList();
            }
            catch (Exception e)
            {
                _logger.LogError(e, "[PointRepository] Could not load today's attempts for user {UserId}.", userId);
                return null;
            }
        }

        public async Task<StudentRank?> GetStudentRankAsync(int totalPoints)
        {
            try
            {
                var studentRoleId = await _db.Roles
                    .AsNoTracking()
                    .Where(r => r.Name == "Student")
                    .Select(r => r.Id)
                    .FirstOrDefaultAsync();

                /* The total points of every student (0 for students without points) */
                var studentTotals = _db.UserRoles
                    .Where(ur => ur.RoleId == studentRoleId)
                    .Select(ur => _db.PointAwards
                        .Where(p => p.UserId == ur.UserId)
                        .Sum(p => (int?)p.Points) ?? 0);

                var studentCount = await studentTotals.CountAsync();

                /* Rank: 1 + the number of students with more points */
                var rank = await studentTotals.CountAsync(total => total > totalPoints) + 1;

                return new StudentRank(studentCount, rank);
            }
            catch (Exception e)
            {
                _logger.LogError(e, "[PointRepository] Could not calculate the student rank.");
                return null;
            }
        }

        public async Task<List<AttemptRow>?> GetAttemptRowsAsync(string userId)
        {
            try
            {
                return await _db.QuizAttempts
                    .AsNoTracking()
                    .Where(a => a.UserId == userId)
                    .Select(a => new AttemptRow(a.QuizId, a.Score, a.CompletedAt, a.Quiz.AwardsPoints))
                    .ToListAsync();
            }
            catch (Exception e)
            {
                _logger.LogError(e, "[PointRepository] Could not load the attempts of user {UserId}.", userId);
                return null;
            }
        }

        public async Task<int?> CountDailyGoalDaysAsync(string userId)
        {
            try
            {
                return await _db.PointAwards
                    .AsNoTracking()
                    .CountAsync(p => p.UserId == userId && p.Reason == PointReason.DailyGoal);
            }
            catch (Exception e)
            {
                _logger.LogError(e, "[PointRepository] Could not count the daily goals of user {UserId}.", userId);
                return null;
            }
        }

        public async Task<AttemptContext?> GetAttemptContextAsync(string userId, int quizId, DateOnly today)
        {
            try
            {
                /* Best result before this attempt, null if the user has never taken this quiz */
                var previousBest = await _db.QuizAttempts
                    .AsNoTracking()
                    .Where(a => a.UserId == userId && a.QuizId == quizId)
                    .MaxAsync(a => (int?)a.Score);

                /* Total points before this attempt, used to see if the user levels up */
                var totalPoints = await _db.PointAwards
                    .AsNoTracking()
                    .Where(p => p.UserId == userId)
                    .SumAsync(p => (int?)p.Points) ?? 0;

                var dailyGoalGiven = await _db.PointAwards
                    .AsNoTracking()
                    .AnyAsync(p => p.UserId == userId && p.Reason == PointReason.DailyGoal && p.AwardedOn == today);

                /* Today's attempts on point quizzes with their score, for the daily goal stars (a day is a Norwegian day) */
                var (startUtc, endUtc) = PointRules.DayInUtc(today);

                var attemptsToday = (await _db.QuizAttempts
                    .AsNoTracking()
                    .Where(a => a.UserId == userId && a.Quiz.AwardsPoints &&
                                a.CompletedAt >= startUtc && a.CompletedAt < endUtc)
                    .Select(a => new { a.QuizId, a.Score })
                    .ToListAsync())
                    .Select(a => (a.QuizId, a.Score))
                    .ToList();

                var streakGiven = await _db.PointAwards
                    .AsNoTracking()
                    .AnyAsync(p => p.UserId == userId && p.Reason == PointReason.Streak && p.AwardedOn == today);

                /* Days the user got streak points before today */
                var earlierStreakDays = await _db.PointAwards
                    .AsNoTracking()
                    .Where(p => p.UserId == userId && p.Reason == PointReason.Streak && p.AwardedOn < today)
                    .Select(p => p.AwardedOn)
                    .ToListAsync();

                return new AttemptContext(
                    previousBest, totalPoints, dailyGoalGiven, attemptsToday, streakGiven, earlierStreakDays);
            }
            catch (Exception e)
            {
                _logger.LogError(e, "[PointRepository] Could not load the point history of user {UserId}.", userId);
                return null;
            }
        }

        public async Task<bool> AddAttemptAsync(QuizAttempt attempt, IEnumerable<PointAward> awards)
        {
            try
            {
                _db.QuizAttempts.Add(attempt);
                _db.PointAwards.AddRange(awards);

                /* The attempt and all its points are saved together, so it is all or nothing */
                await _db.SaveChangesAsync();
                return true;
            }
            catch (Exception e)
            {
                _logger.LogError(e, "[PointRepository] Could not save the attempt of user {UserId} on quiz ID {QuizId}.",
                    attempt.UserId, attempt.QuizId);
                return false;
            }
        }

        public async Task<Dictionary<string, StudentActivity>?> GetStudentActivityAsync()
        {
            try
            {
                var studentRoleId = await _db.Roles
                    .AsNoTracking()
                    .Where(r => r.Name == "Student")
                    .Select(r => r.Id)
                    .FirstOrDefaultAsync();

                var studentIds = _db.UserRoles
                    .Where(ur => ur.RoleId == studentRoleId)
                    .Select(ur => ur.UserId);

                var attempts = await _db.QuizAttempts
                    .AsNoTracking()
                    .Where(a => studentIds.Contains(a.UserId))
                    .Select(a => new { a.UserId, a.QuizId, a.Score, a.CompletedAt, a.Quiz.AwardsPoints })
                    .ToListAsync();

                var awards = await _db.PointAwards
                    .AsNoTracking()
                    .Where(p => studentIds.Contains(p.UserId) &&
                                (p.Reason == PointReason.Streak || p.Reason == PointReason.DailyGoal))
                    .Select(p => new { p.UserId, p.Reason, p.AwardedOn })
                    .ToListAsync();

                var attemptsByUser = attempts
                    .GroupBy(a => a.UserId)
                    .ToDictionary(
                        g => g.Key,
                        g => g.Select(a => new AttemptRow(a.QuizId, a.Score, a.CompletedAt, a.AwardsPoints)).ToList());

                var awardsByUser = awards.GroupBy(a => a.UserId).ToDictionary(g => g.Key, g => g.ToList());

                var result = new Dictionary<string, StudentActivity>();

                foreach (var userId in attemptsByUser.Keys.Union(awardsByUser.Keys))
                {
                    var streakDays = new List<DateOnly>();
                    var dailyGoalDays = 0;

                    if (awardsByUser.TryGetValue(userId, out var userAwards))
                    {
                        streakDays = userAwards
                            .Where(a => a.Reason == PointReason.Streak)
                            .Select(a => a.AwardedOn)
                            .ToList();

                        dailyGoalDays = userAwards.Count(a => a.Reason == PointReason.DailyGoal);
                    }

                    attemptsByUser.TryGetValue(userId, out var userAttempts);

                    result[userId] = new StudentActivity(
                        userAttempts ?? new List<AttemptRow>(),
                        streakDays,
                        dailyGoalDays);
                }

                return result;
            }
            catch (Exception e)
            {
                _logger.LogError(e, "[PointRepository] Could not load the activity of the students.");
                return null;
            }
        }

        public async Task<List<StudentPoints>?> GetLeaderboardAsync(DateOnly? since)
        {
            try
            {
                var studentRoleId = await _db.Roles
                    .AsNoTracking()
                    .Where(r => r.Name == "Student")
                    .Select(r => r.Id)
                    .FirstOrDefaultAsync();

                /* The awards that count in the chosen period */
                var allAwards = _db.PointAwards.AsNoTracking();
                var sinceDate = since ?? DateOnly.MinValue;
                var periodAwards = since.HasValue
                    ? allAwards.Where(p => p.AwardedOn >= sinceDate)
                    : allAwards;

                /*
                 * All students with the sum of their points (0 if they have none), highest first.
                 * The query uses an anonymous type, because EF cannot sort on a record that is
                 * built with a constructor. The records are created after the query has run.
                 */
                var rows = await _db.Users
                    .AsNoTracking()
                    .Where(u => _db.UserRoles.Any(ur => ur.UserId == u.Id && ur.RoleId == studentRoleId))
                    .Select(u => new
                    {
                        u.Id,
                        u.UserName,
                        PeriodPoints = periodAwards
                            .Where(p => p.UserId == u.Id)
                            .Sum(p => (int?)p.Points) ?? 0,
                        AllTimePoints = allAwards
                            .Where(p => p.UserId == u.Id)
                            .Sum(p => (int?)p.Points) ?? 0
                    })
                    .OrderByDescending(s => s.PeriodPoints)
                    .ThenBy(s => s.UserName)
                    .ToListAsync();

                return rows
                    .Select(r => new StudentPoints(r.Id, r.UserName, r.PeriodPoints, r.AllTimePoints))
                    .ToList();
            }
            catch (Exception e)
            {
                _logger.LogError(e, "[PointRepository] Could not load the leaderboard.");
                return null;
            }
        }
    }
}