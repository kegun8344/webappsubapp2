using System.ComponentModel.DataAnnotations;

namespace api.DTOs
{
    /*
     * The body when creating or updating a quiz.
     * Only fields the user may change are here. The owner, visibility and whether the quiz
     * gives points are decided by the server, never by the request.
     */
    public class QuizFormDto
    {
        [Required]
        [StringLength(100, MinimumLength = 1, ErrorMessage = "The title must be between 1 and 100 characters.")]
        public string Title { get; set; } = string.Empty;

        [StringLength(500, ErrorMessage = "The description cannot exceed 500 characters.")]
        public string? Description { get; set; }

        [Required]
        [RegularExpression("^(Easy|Medium|Hard)$", ErrorMessage = "Difficulty must be Easy, Medium, or Hard.")]
        public string Difficulty { get; set; } = "Easy";

        [Range(1, int.MaxValue, ErrorMessage = "Choose a module.")]
        public int ModuleId { get; set; }
    }
}