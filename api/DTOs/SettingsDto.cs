using System.ComponentModel.DataAnnotations;
using api.Models;

namespace api.DTOs
{
    /* Sent to and from the Settings page */
    public class SettingsDto
    {
        public bool DarkModeEnabled { get; set; }
        public bool ReduceMotion { get; set; }

        [Required]
        [RegularExpression("^(None|RedGreen|BlueYellow)$",
            ErrorMessage = "Choose one of the colour vision options.")]
        public string ColorVisionMode { get; set; } = "None";

        public static SettingsDto FromSettings(UserSettings s) => new()
        {
            DarkModeEnabled = s.DarkModeEnabled,
            ReduceMotion = s.ReduceMotion,
            ColorVisionMode = s.ColorVisionMode
        };
    }

    /* Same rules as RegisterDto / Subapp 1's ChangePasswordViewModel */
    public class ChangePasswordDto
    {
        [Required(ErrorMessage = "Please enter your current password.")]
        public string CurrentPassword { get; set; } = "";

        [Required(ErrorMessage = "Please choose a new password.")]
        [StringLength(100, MinimumLength = 8, ErrorMessage = "The password must be at least 8 characters.")]
        [RegularExpression(@"^(?=.*[a-z])(?=.*[A-Z])(?=.*\d)(?=.*[^a-zA-Z\d]).{8,}$",
            ErrorMessage = "The password needs an uppercase letter, a lowercase letter, a number and a symbol.")]
        public string NewPassword { get; set; } = "";

        [Required(ErrorMessage = "Please confirm your new password.")]
        [Compare(nameof(NewPassword), ErrorMessage = "Passwords do not match.")]
        public string ConfirmPassword { get; set; } = "";
    }

    /* What the delete page shows before the user confirms */
    public class DeleteAccountInfoDto
    {
        public string? BlockReason { get; set; }
        public int PrivateQuizzes { get; set; }
        public int PublicQuizzes { get; set; }
        public int Attempts { get; set; }
        public int Points { get; set; }
    }

    public class DeleteAccountDto
    {
        [Required(ErrorMessage = "Please enter your password.")]
        public string Password { get; set; } = "";
    }
}