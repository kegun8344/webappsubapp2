using System.ComponentModel.DataAnnotations;

namespace api.Models
{
    /*
     * One answer a user gave to one question, in a quiz or in a review.
     * The answers are used to build the "Review mistakes" quiz: a question the user
     * got wrong last time (and has not answered correctly since) can be practised again.
     */
    public class UserAnswer
    {
        public int Id { get; set; }

        [Required]
        public string UserId { get; set; } = string.Empty;
        public ApplicationUser User { get; set; } = null!;

        public int QuestionId { get; set; }
        public Question Question { get; set; } = null!;

        /* The answer option the user chose. Null if the question was left unanswered. */
        public int? SelectedAnswerId { get; set; }

        public bool IsCorrect { get; set; }

        /* Stored in UTC */
        public DateTime AnsweredAt { get; set; } = DateTime.UtcNow;
    }
}