using System.ComponentModel.DataAnnotations;

namespace api.Models
{
    // Represents a quiz, the top-level entity that owns a set of questions.
    public class Quiz
    {
        public int Id { get; set; }

        [Required]
        [StringLength(100)]
        public string Title { get; set; } = string.Empty;

        [StringLength(500)]
        public string? Description { get; set; } = string.Empty;

        [Required]
        [StringLength(20)]
        [RegularExpression(
            "^(Easy|Medium|Hard)$",
            ErrorMessage = "Difficulty must be Easy, Medium, or Hard.")]
        public string Difficulty { get; set; } = "Easy";


        /*
         * Every quiz belongs to one learning module.
         * A module can contain multiple quizzes.
         */
        public int ModuleId { get; set; }

        public Module Module { get; set; } = null!;


        /*
         * The Identity user who created the quiz.
         * The value may become null if the creator account is deleted.
         */
        public string? CreatedByUserId { get; set; }

        public ApplicationUser? CreatedBy { get; set; }


        /*
         * Controls whether the quiz is visible to other users.
         * Student-created quizzes are private by default.
         * Teacher and administrator quizzes are public by default.
         */
        public bool IsPublic { get; set; }


        /*
         * Teacher and administrator quizzes award points.
         * Personal student quizzes do not.
         */
        public bool AwardsPoints { get; set; }


        // One-to-many: all questions belonging to this quiz.
        public List<Question> Questions { get; set; } = new();


        // One-to-many: all attempts at this quiz, by any user.
        public List<QuizAttempt> Attempts { get; set; } = new();

        // Many-to-many: tags on this quiz, e.g. "Exam relevant".
        public List<Tag> Tags { get; set; } = new();
    }
}