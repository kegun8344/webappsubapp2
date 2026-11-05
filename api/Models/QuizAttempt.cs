using System.ComponentModel.DataAnnotations;

namespace api.Models
{
    /*
     * One finished attempt at a quiz by a user.
     * Every attempt is saved (history), but only the best score per quiz counts towards the user's points.
     */
    public class QuizAttempt
    {
        public int Id { get; set; }

        /* Who took the quiz (foreign key to the Identity user) */
        [Required]
        public string UserId { get; set; } = string.Empty;
        public ApplicationUser User { get; set; } = null!;

        /* Which quiz was taken */
        public int QuizId { get; set; }
        public Quiz Quiz { get; set; } = null!;

        public int CorrectAnswers { get; set; }

        public int TotalQuestions { get; set; }

        /* Percentage correct, 0-100. This is also the number of points the attempt is worth. */
        [Range(0, 100)]
        public int Score { get; set; }

        public DateTime CompletedAt { get; set; } = DateTime.UtcNow;
    }
}