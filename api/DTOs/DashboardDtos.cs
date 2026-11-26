using api.Services;

namespace api.DTOs
{
    /* The student's own dashboard from GET /api/dashboard */
    public class DashboardDto
    {
        public string DisplayName { get; set; } = string.Empty;

        public int TotalPoints { get; set; }

        public int Level { get; set; }

        /* How far into the current level, e.g. 150 of 400 */
        public int PointsIntoLevel { get; set; }

        public int PointsForLevel { get; set; }

        /* Days in a row with a completed quiz that gives points */
        public int StreakDays { get; set; }

        /* Different quizzes the student has completed */
        public int QuizzesCompleted { get; set; }

        /* Average of the best score on each quiz, null if none completed */
        public int? AverageScore { get; set; }

        public int LeaderboardRank { get; set; }

        public int StudentCount { get; set; }

        public List<RecentAttemptDto> RecentAttempts { get; set; } = new();

        /* All badges, also the locked ones, with progress towards the next tier */
        public List<BadgeProgressDto> Badges { get; set; } = new();
    }

    /* One row in "Recent quizzes" */
    public class RecentAttemptDto
    {
        public int QuizId { get; set; }

        public string QuizTitle { get; set; } = string.Empty;

        public int Score { get; set; }

        /* UTC. The React app shows it in the user's own time zone. */
        public DateTime CompletedAt { get; set; }

        public bool AwardsPoints { get; set; }
    }

    /* One badge with the tier the user has and how far it is to the next */
    public class BadgeProgressDto
    {
        public string Type { get; set; } = string.Empty;

        public string Name { get; set; } = string.Empty;

        public string Description { get; set; } = string.Empty;

        /* Word for the numbers, e.g. "quizzes" or "days" */
        public string Unit { get; set; } = string.Empty;

        /* None, Bronze, Silver, Gold or Diamond */
        public string Tier { get; set; } = string.Empty;

        public int Value { get; set; }

        /* Null at the highest tier */
        public int? NextThreshold { get; set; }

        public string? NextTier { get; set; }

        public int ProgressPercent { get; set; }

        public bool IsLocked { get; set; }

        public static BadgeProgressDto FromProgress(BadgeProgress badge)
        {
            return new BadgeProgressDto
            {
                Type = badge.Type.ToString(),
                Name = badge.Name,
                Description = badge.Description,
                Unit = badge.Unit,
                Tier = badge.Tier.ToString(),
                Value = badge.Value,
                NextThreshold = badge.NextThreshold,
                NextTier = badge.NextTier?.ToString(),
                ProgressPercent = badge.ProgressPercent,
                IsLocked = badge.IsLocked
            };
        }
    }
}