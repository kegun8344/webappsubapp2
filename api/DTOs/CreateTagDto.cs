using System.ComponentModel.DataAnnotations;

namespace api.DTOs
{
    /* The body of POST /api/quizzes/{quizId}/tags: a new tag, or an existing one with the same name */
    public class CreateTagDto
    {
        [Required(ErrorMessage = "Write a name for the tag.")]
        [StringLength(30, MinimumLength = 1, ErrorMessage = "A tag name must be between 1 and 30 characters.")]
        public string Name { get; set; } = string.Empty;

        [Required]
        [RegularExpression("^(primary|secondary|success|danger|warning|info|dark)$",
            ErrorMessage = "Choose one of the colors in the list.")]
        public string Color { get; set; } = "primary";

        /* Only used for teachers and administrators. For everyone else the server ignores it. */
        public bool Shared { get; set; }
    }
}