using api.Models;

namespace api.DTOs
{
    /* A tag as the React app receives it */
    public class TagDto
    {
        public int Id { get; set; }

        public string Name { get; set; } = string.Empty;

        /* A Bootstrap colour name, one of Tag.AllowedColors */
        public string Color { get; set; } = "primary";

        /* True for tags in the shared catalog (made by teachers and administrators) */
        public bool IsShared { get; set; }

        public static TagDto FromTag(Tag tag)
        {
            return new TagDto
            {
                Id = tag.Id,
                Name = tag.Name,
                Color = tag.Color,
                IsShared = tag.IsShared
            };
        }
    }
}