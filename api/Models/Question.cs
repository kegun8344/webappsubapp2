using System.ComponentModel.DataAnnotations;

namespace api.Models
{
    // Represents a single question that belongs to a quiz
    public class Question
    {
        public int Id { get; set; }

        [Required]
        [StringLength(500)] // Cap question text at 500 characters
        public string Text { get; set; } = string.Empty;

        public int QuizId { get; set; } // Foreign key to parent Quiz

        public Quiz Quiz { get; set; } = null!;

        // One-to-many: answer options belonging to this question
        public List<AnswerOption> AnswerOptions { get; set; } = new();
    }
}