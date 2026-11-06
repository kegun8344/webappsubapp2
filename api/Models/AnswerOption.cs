using System.ComponentModel.DataAnnotations;

namespace api.Models
{
    public class AnswerOption
    {
        public int Id { get; set; }

        [Required]
        [StringLength(300)] // Cap answer text at 300 characters
        public string Text { get; set; } = string.Empty;

        public bool IsCorrect { get; set; } // Flags this option as the right answer

        public int QuestionId { get; set; } // Foreign key to parent Question

        public Question Question { get; set; } = null!;
    }
}