using api.DAL;

namespace api.Services
{
    /* Collects the numbers the badges are based on. The rules themselves are in BadgeRules. */
    public class BadgeService : IBadgeService
    {
        /* How many points better than the first attempt a quiz must be to count for Comeback */
        public const int ComebackImprovement = BadgeStatsBuilder.ComebackImprovement;

        private readonly IPointRepository pointRepository;

        public BadgeService(IPointRepository pointRepo)
        {
            pointRepository = pointRepo;
        }

        public async Task<BadgeStats?> GetStatsAsync(string userId)
        {
            /* All the user's attempts. A student has at most a few hundred, so they are grouped in memory. */
            var attempts = await pointRepository.GetAttemptRowsAsync(userId);
            var streakDays = await pointRepository.GetStreakDaysAsync(userId);
            var dailyGoalDays = await pointRepository.CountDailyGoalDaysAsync(userId);

            if (attempts == null || streakDays == null || dailyGoalDays == null)
            {
                /* The repository has already logged the database error */
                return null;
            }

            return BadgeStatsBuilder.Build(attempts, streakDays, dailyGoalDays.Value);
        }

        public async Task<List<BadgeProgress>?> GetBadgesAsync(string userId)
        {
            var stats = await GetStatsAsync(userId);

            return stats == null ? null : BadgeRules.GetBadges(stats);
        }
    }
}