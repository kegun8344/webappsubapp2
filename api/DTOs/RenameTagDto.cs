using System.ComponentModel.DataAnnotations;

namespace api.DTOs
{
    /* The body of PUT /api/tags/{id}: the new name of one of the user's own tags */
    public class RenameTagDto
    {
        [Required(ErrorMessage = "Write a name for the tag.")]
        [StringLength(30, MinimumLength = 1, ErrorMessage = "A tag name must be between 1 and 30 characters.")]
        public string Name { get; set; } = string.Empty;
    }
}