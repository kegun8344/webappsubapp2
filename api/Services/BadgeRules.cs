namespace api.Services
{
    /*
     * Badge rules, without any database. Badges are calculated from the user's stats, not stored.
     * Every badge has four tiers: Bronze, Silver, Gold and Diamond (almost unreachable).
     * A badge can only be upgraded, never lost, because the stats only go up.
     */
    public static class BadgeRules
    {
        /* Name, how to earn it, unit, and the number needed for Bronze, Silver, Gold and Diamond */
        private record BadgeDefinition(BadgeType Type, string Name, string Description, string Unit, int[] Thresholds);

        private static readonly BadgeDefinition[] Definitions =
        {
            new(BadgeType.Explorer, "Explorer", "Complete different quizzes", "quizzes", new[] { 1, 5, 15, 40 }),
            new(BadgeType.Perfectionist, "Perfectionist", "Get 100% on quizzes", "quizzes", new[] { 1, 3, 10, 25 }),
            new(BadgeType.OnFire, "On Fire", "Complete quizzes several days in a row", "days", new[] { 3, 5, 10, 30 }),
            new(BadgeType.Comeback, "Comeback", "Improve a quiz score by 30 points or more", "quizzes", new[] { 1, 3, 5, 15 }),
            new(BadgeType.BusyBee, "Busy Bee", "Reach the daily goal of 5 stars", "days", new[] { 1, 3, 7, 20 }),
            new(BadgeType.PracticeMakesPerfect, "Practice Makes Perfect", "Take quizzes, practice quizzes count too", "attempts", new[] { 10, 25, 50, 150 })
        };

        /* All badges for a user, in the same order every time */
        public static List<BadgeProgress> GetBadges(BadgeStats stats)
        {
            return Definitions
                .Select(definition => Evaluate(definition, ValueFor(definition.Type, stats)))
                .ToList();
        }

        /* Badges that were unlocked or upgraded between two moments, e.g. before and after a quiz attempt */
        public static List<BadgeProgress> NewBadges(BadgeStats before, BadgeStats after)
        {
            var tiersBefore = GetBadges(before).ToDictionary(b => b.Type, b => b.Tier);

            return GetBadges(after)
                .Where(badge => badge.Tier > tiersBefore[badge.Type])
                .ToList();
        }

        /* How many thresholds the value has reached: 0 = locked, 1 = Bronze ... 4 = Diamond */
        public static BadgeTier TierFor(int value, IReadOnlyList<int> thresholds)
        {
            if (value < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(value), "A badge value cannot be negative.");
            }

            var tier = 0;
            while (tier < thresholds.Count && value >= thresholds[tier])
            {
                tier++;
            }

            return (BadgeTier)tier;
        }

        private static BadgeProgress Evaluate(BadgeDefinition definition, int value)
        {
            var tier = TierFor(value, definition.Thresholds);
            var isMaxed = (int)tier >= definition.Thresholds.Length;

            return new BadgeProgress
            {
                Type = definition.Type,
                Name = definition.Name,
                Description = definition.Description,
                Unit = definition.Unit,
                Tier = tier,
                Value = value,
                NextThreshold = isMaxed ? null : definition.Thresholds[(int)tier],
                NextTier = isMaxed ? null : tier + 1
            };
        }

        private static int ValueFor(BadgeType type, BadgeStats stats) => type switch
        {
            BadgeType.Explorer => stats.QuizzesCompleted,
            BadgeType.Perfectionist => stats.PerfectQuizzes,
            BadgeType.OnFire => stats.LongestStreak,
            BadgeType.Comeback => stats.ComebackQuizzes,
            BadgeType.BusyBee => stats.DailyGoalDays,
            BadgeType.PracticeMakesPerfect => stats.TotalAttempts,
            _ => 0
        };
    }
}