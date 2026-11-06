using System.ComponentModel.DataAnnotations;
using api.Services;

namespace api.Models
{
    /*
     * Represents a learning module within the ITPE3200 course.
     * Modules organise related quizzes into meaningful subject areas,
     */
    public class Module
    {
        public int Id { get; set; }

        [Required]
        [StringLength(
            100,
            MinimumLength = 2,
            ErrorMessage = "Module name must be between 2 and 100 characters.")]
        public string Name { get; set; } = string.Empty;

        [StringLength(
            500,
            ErrorMessage = "Description cannot exceed 500 characters.")]
        public string? Description { get; set; }

        /*
         * The Bootstrap Icons class chosen by the teacher, e.g. "bi-database".
         * Only keys from ModuleIcons.All are accepted, so it is safe to put in a CSS class.
         */
        [StringLength(40)]
        public string IconKey { get; set; } = ModuleIcons.Default;

        /*
         * The colour chosen by the teacher, 0 to 7 (see ModuleColors).
         * Null means it is worked out from the name, like before colours could be chosen.
         */
        [Range(0, 7)]
        public int? ColorIndex { get; set; }

        // One-to-many: a module can contain multiple quizzes.
        public List<Quiz> Quizzes { get; set; } = new();
    }
}