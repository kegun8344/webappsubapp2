using System.ComponentModel.DataAnnotations;

namespace api.Models
{
    /* Why a user got points */
    public enum PointReason
    {
        QuizScore,        // Points for a better result on a quiz (only the improvement)
        FirstCompletion,  // First time a user completes a quiz
        DailyGoal,        // 5 daily goal stars collected in one day
        Streak            // Daily bonus for completing quizzes several days in a row
    }

    /*
     * One row per time a user gets points (a "points log").
     * A user's total points is the sum of all their rows.
     */
    public class PointAward
    {
        public int Id { get; set; }

        [Required]
        public string UserId { get; set; } = string.Empty;
        public ApplicationUser User { get; set; } = null!;

        public int Points { get; set; }

        public PointReason Reason { get; set; }

        /* The quiz the points came from. Null for DailyGoal and Streak, or if the quiz was deleted. */
        public int? QuizId { get; set; }
        public Quiz? Quiz { get; set; }

        /* The day in Norwegian time. Used so DailyGoal and Streak are only given once per day. */
        public DateOnly AwardedOn { get; set; }

        /* Exact time, stored in UTC */
        public DateTime AwardedAt { get; set; } = DateTime.UtcNow;
    }
}