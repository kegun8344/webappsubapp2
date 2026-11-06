using api.Models;

namespace api.Services
{
    /* One line on the result page, e.g. "Streak day 3: +30" */
    public record AwardLine(PointReason Reason, int Points);

    /* What happened when an attempt was saved: the score, and which points were given and why */
    public class PointResult
    {
        public int Score { get; set; }

        /* Null if this was the first attempt at the quiz */
        public int? PreviousBest { get; set; }

        public int BestScore { get; set; }

        /* False for practice quizzes made by students */
        public bool AwardsPoints { get; set; }

        /* Which day of the streak this is, 0 if no streak points were given now */
        public int StreakDay { get; set; }
        /* Daily goal stars today before and after this attempt (see PointRules.DailyGoalStars) */
        public int DailyGoalStarsBefore { get; set; }
        public int DailyGoalStarsAfter { get; set; }
        public int DailyGoalStarsEarned => DailyGoalStarsAfter - DailyGoalStarsBefore;

        public List<AwardLine> Awards { get; set; } = new();

        public int TotalPoints => Awards.Sum(a => a.Points);


        /* The user's total points before this attempt */
        public int TotalPointsBefore { get; set; }

        public int TotalPointsAfter => TotalPointsBefore + TotalPoints;

        /* True if the points from this attempt took the user to a new level */
        public bool LeveledUp =>
            LevelCalculator.GetLevel(TotalPointsAfter).Level > LevelCalculator.GetLevel(TotalPointsBefore).Level;
    }
}