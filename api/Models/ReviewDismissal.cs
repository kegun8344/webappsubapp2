using System.ComponentModel.DataAnnotations;

namespace api.Models
{
    /*
     * The user cleared the mistakes in a quiz at this time ("Clear mistakes" in a review).
     * Answers from before this time no longer count as mistakes. A new wrong answer after it
     * is newer, so that question comes back by itself. No answers are deleted.
     */
    public class ReviewDismissal
    {
        public int Id { get; set; }

        [Required]
        public string UserId { get; set; } = string.Empty;
        public ApplicationUser User { get; set; } = null!;

        public int QuizId { get; set; }
        public Quiz Quiz { get; set; } = null!;

        /* Stored in UTC */
        public DateTime DismissedAt { get; set; } = DateTime.UtcNow;
    }
}