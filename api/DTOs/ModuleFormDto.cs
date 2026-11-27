using System.ComponentModel.DataAnnotations;
using api.Services;

namespace api.DTOs
{
    /*
     * The body when creating or editing a module. Only fields the teacher may change.
     * The icon and the colour are checked against the fixed lists in the controller.
     */
    public class ModuleFormDto
    {
        [Required]
        [StringLength(100, MinimumLength = 2, ErrorMessage = "Module name must be between 2 and 100 characters.")]
        public string Name { get; set; } = string.Empty;

        [StringLength(500, ErrorMessage = "Description cannot exceed 500 characters.")]
        public string? Description { get; set; }

        /* One of the keys in ModuleIcons.All */
        [Required]
        public string IconKey { get; set; } = ModuleIcons.Default;

        /* One of the colours in ModuleColors (0-7). Null means the colour comes from the name. */
        public int? ColorIndex { get; set; }
    }
}