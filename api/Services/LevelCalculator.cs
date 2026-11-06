namespace api.Services
{
    /*
     * Levels are calculated from total points, not stored.
     * Points needed to go from level n to n + 1 = BasePoints + IncreasePerLevel * (n - 1).
     * Level 1 -> 2: 200, level 2 -> 3: 300, level 3 -> 4: 400, ...
     */
    public static class LevelCalculator
    {
        public const int BasePoints = 200;
        public const int IncreasePerLevel = 100;

        /* Points needed to go from this level to the next */
        public static int PointsToNextLevel(int level)
        {
            if (level < 1)
            {
                throw new ArgumentOutOfRangeException(nameof(level), "Level must be 1 or higher.");
            }

            return BasePoints + IncreasePerLevel * (level - 1);
        }

        /* The level, how far into it the user is, and how many points the level needs in total */
        public static (int Level, int PointsIntoLevel, int PointsForLevel) GetLevel(int totalPoints)
        {
            if (totalPoints < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(totalPoints), "Points cannot be negative.");
            }

            var level = 1;
            var remaining = totalPoints;

            /* Climb one level at a time while there are enough points left */
            while (remaining >= PointsToNextLevel(level))
            {
                remaining -= PointsToNextLevel(level);
                level++;
            }

            return (level, remaining, PointsToNextLevel(level));
        }
    }
}