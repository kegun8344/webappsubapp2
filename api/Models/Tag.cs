using System.ComponentModel.DataAnnotations;

namespace api.Models
{
    /* A label that can be put on quizzes, e.g. "Exam relevant". One tag can be on many quizzes (N:M). */
    public class Tag
    {
        /* Bootstrap colors a tag can have. Only these are allowed, so users cannot add their own CSS. */
        public static readonly string[] AllowedColors =
            { "primary", "secondary", "success", "danger", "warning", "info", "dark" };
        /* Names shown to the user, so "danger" is shown as "Red" */
        public static readonly Dictionary<string, string> ColorNames = new()
        {
            ["primary"] = "Blue",
            ["secondary"] = "Gray",
            ["success"] = "Green",
            ["danger"] = "Red",
            ["warning"] = "Yellow",
            ["info"] = "Light blue",
            ["dark"] = "Black"
        };

        public int Id { get; set; }

        [Required(ErrorMessage = "Write a name for the tag.")]
        [StringLength(30)]
        public string Name { get; set; } = string.Empty;

        [Required]
        [StringLength(20)]
        [RegularExpression("^(primary|secondary|success|danger|warning|info|dark)$",
            ErrorMessage = "Choose one of the colors in the list.")]
        public string Color { get; set; } = "primary";

        /*
         * Who owns the tag.
         * Null means a shared tag in the catalog, created by a teacher or an administrator,
         * which every user can see and use. Otherwise it is a personal tag that only
         * its owner can see and use.
         */
        public string? OwnerUserId { get; set; }

        public ApplicationUser? Owner { get; set; }

        /* A shared tag has no owner */
        public bool IsShared => OwnerUserId == null;

        /* The rule for who may see and use a tag: shared tags, or your own personal tags */
        public bool IsVisibleTo(string? userId) => OwnerUserId == null || OwnerUserId == userId;

        /* Many-to-many: all quizzes that have this tag */
        public List<Quiz> Quizzes { get; set; } = new();
    }
}