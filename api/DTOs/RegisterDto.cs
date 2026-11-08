using System.ComponentModel.DataAnnotations;

namespace api.DTOs
{
    /*
     * What the React app sends to sign up. Same rules as RegisterViewModel in Subapp 1:
     * [EmailAddress] alone accepts "a@b", so the regex also requires a dot in the domain.
     * The role is never sent: everyone who signs up becomes a student.
     */
    public class RegisterDto
    {
        [Required(ErrorMessage = "Please enter your email.")]
        [EmailAddress(ErrorMessage = "Please enter a valid email address.")]
        [RegularExpression(@"^[^@\s]+@[^@\s]+\.[^@\s]+$", ErrorMessage = "Please enter a valid email address.")]
        [StringLength(256)]
        public string Email { get; set; } = "";

        /* Same rules as Identity in Program.cs: 8 characters, upper and lower case, a number and a symbol */
        [Required(ErrorMessage = "Please choose a password.")]
        [StringLength(100, MinimumLength = 8, ErrorMessage = "The password must be at least 8 characters.")]
        [RegularExpression(@"^(?=.*[a-z])(?=.*[A-Z])(?=.*\d)(?=.*[^a-zA-Z\d]).{8,}$",
            ErrorMessage = "The password needs an uppercase letter, a lowercase letter, a number and a symbol.")]
        public string Password { get; set; } = "";

        [Required(ErrorMessage = "Please confirm your password.")]
        [Compare(nameof(Password), ErrorMessage = "Passwords do not match.")]
        public string ConfirmPassword { get; set; } = "";
    }
}