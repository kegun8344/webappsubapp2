namespace api.Services
{
    public enum BadgeType
    {
        Explorer,
        Perfectionist,
        OnFire,
        Comeback,
        BusyBee,
        PracticeMakesPerfect
    }

    /* None means the badge is still locked */
    public enum BadgeTier
    {
        None = 0,
        Bronze = 1,
        Silver = 2,
        Gold = 3,
        Diamond = 4
    }

    /* The numbers about a user that the badges are calculated from */
    public class BadgeStats
    {
        /* Different quizzes that give points, completed at least once */
        public int QuizzesCompleted { get; set; }

        /* Quizzes that give points where the best score is 100% */
        public int PerfectQuizzes { get; set; }

        /* Longest number of days in a row with streak points */
        public int LongestStreak { get; set; }

        /* Quizzes where the best score is at least 30 points better than the first attempt */
        public int ComebackQuizzes { get; set; }

        /* Days the user reached the daily goal */
        public int DailyGoalDays { get; set; }

        /* All attempts, also on practice quizzes */
        public int TotalAttempts { get; set; }
    }

    /* One badge for one user: the tier they have, and how far they are from the next */
    public class BadgeProgress
    {
        public BadgeType Type { get; set; }
        public string Name { get; set; } = string.Empty;

        /* How to earn it, e.g. "Complete different quizzes" */
        public string Description { get; set; } = string.Empty;

        /* Word used for the numbers, e.g. "quizzes" or "days" */
        public string Unit { get; set; } = string.Empty;

        public BadgeTier Tier { get; set; }

        /* The user's current number, e.g. 7 quizzes */
        public int Value { get; set; }

        /* The number needed for the next tier, null at Diamond */
        public int? NextThreshold { get; set; }
        public BadgeTier? NextTier { get; set; }

        public bool IsLocked => Tier == BadgeTier.None;
        public bool IsMaxed => NextThreshold == null;

        public int ProgressPercent => NextThreshold is int next && next > 0
            ? Math.Min(100, Value * 100 / next)
            : 100;
    }
}