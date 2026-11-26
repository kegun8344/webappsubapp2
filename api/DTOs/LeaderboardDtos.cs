namespace api.DTOs
{
    /* Everything the leaderboard page needs from GET /api/leaderboard */
    public class LeaderboardDto
    {
        /* True when only points since Monday are counted */
        public bool IsWeekly { get; set; }

        /* The Monday the weekly list starts on */
        public DateOnly WeekStart { get; set; }

        /* All students, best first. The React app shows the first three as a podium. */
        public List<LeaderboardEntryDto> Entries { get; set; } = new();

        /* The signed-in student's own position, null for teachers and administrators */
        public LeaderboardStandingDto? Standing { get; set; }

        /* Records among all students, e.g. the longest streak */
        public List<LeaderboardHighlightDto> Highlights { get; set; } = new();
    }

    /* One student on the leaderboard */
    public class LeaderboardEntryDto
    {
        /* Students with the same points share a rank (1, 1, 3) */
        public int Rank { get; set; }

        /* The part of the email before @, so addresses are not shown to everyone */
        public string DisplayName { get; set; } = string.Empty;

        public string Initial { get; set; } = "?";

        /* 0-7, used for the avatar colour class lb-avatar-N */
        public int AvatarColor { get; set; }

        /* Points in the period that is shown (this week or all time) */
        public int Points { get; set; }

        /* Length of the points bar compared to first place, 0-100 */
        public int BarPercent { get; set; }

        /* Always from all-time points, also on the weekly list */
        public int Level { get; set; }

        public int CurrentStreak { get; set; }

        public int BadgeCount { get; set; }

        /* The best unlocked badges, highest tier first */
        public List<BadgeDto> TopBadges { get; set; } = new();

        public bool IsCurrentUser { get; set; }
    }

    /* "Your standing": rank, level progress and how far behind the next student */
    public class LeaderboardStandingDto
    {
        public int Rank { get; set; }

        public int StudentCount { get; set; }

        public int Points { get; set; }

        public int Level { get; set; }

        public int LevelProgressPercent { get; set; }

        public int PointsToNextLevel { get; set; }

        /* The student right above, null if the user is in the lead */
        public string? NextPlaceName { get; set; }

        public int PointsBehindNextPlace { get; set; }
    }

    /* One record in the "Hall of fame" */
    public class LeaderboardHighlightDto
    {
        public string Title { get; set; } = string.Empty;

        /* A Bootstrap Icons class, e.g. "bi-fire" */
        public string Icon { get; set; } = string.Empty;

        public string Name { get; set; } = string.Empty;

        public int Value { get; set; }

        public string Unit { get; set; } = string.Empty;
    }
}