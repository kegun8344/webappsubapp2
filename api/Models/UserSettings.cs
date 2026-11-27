using System.ComponentModel.DataAnnotations;

namespace api.Models
{
    /*
     * A user's own preferences, shown on the Settings page.
     * One row per user (one-to-one). A user without a row uses the default values below.
     */
    public class UserSettings
    {
        /* The colour vision modes a user can choose. Only these are allowed. */
        public static readonly string[] ColorVisionModes = { "None", "RedGreen", "BlueYellow" };

        /* The user ID is also the primary key, so a user can never have two rows */
        [Key]
        public string UserId { get; set; } = string.Empty;
        public ApplicationUser User { get; set; } = null!;

        public bool DarkModeEnabled { get; set; }

        /* Turns off animations */
        public bool ReduceMotion { get; set; }

        [Required]
        [StringLength(20)]
        [RegularExpression("^(None|RedGreen|BlueYellow)$",
            ErrorMessage = "Choose one of the colour vision options.")]
        public string ColorVisionMode { get; set; } = "None";
    }
}
