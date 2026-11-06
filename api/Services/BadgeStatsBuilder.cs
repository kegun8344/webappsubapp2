using api.DAL;

namespace api.Services
{
    /*
     * Calculates the numbers the badges are based on, from a user's attempts and point log.
     * Pure logic without database access, so it is used both for one user (BadgeService)
     * and for every student at once (leaderboard), and is easy to unit test.
     */
    public static class BadgeStatsBuilder
    {
        /* How many points better than the first attempt a quiz must be to count for Comeback */
        public const int ComebackImprovement = 30;

        public static BadgeStats Build(
            IReadOnlyCollection<AttemptRow> attempts,
            IEnumerable<DateOnly> streakDays,
            int dailyGoalDays)
        {
            /* Best and first score on each quiz that gives points */
            var officialQuizzes = attempts
                .Where(a => a.AwardsPoints)
                .GroupBy(a => a.QuizId)
                .Select(g => new
                {
                    Best = g.Max(a => a.Score),
                    First = g.OrderBy(a => a.CompletedAt).First().Score
                })
                .ToList();

            return new BadgeStats
            {
                QuizzesCompleted = officialQuizzes.Count,
                PerfectQuizzes = officialQuizzes.Count(q => q.Best >= 100),
                LongestStreak = PointRules.LongestStreak(streakDays),
                ComebackQuizzes = officialQuizzes.Count(q => q.Best - q.First >= ComebackImprovement),
                DailyGoalDays = dailyGoalDays,
                /* Practice quizzes count here, but nowhere else */
                TotalAttempts = attempts.Count
            };
        }
    }
}