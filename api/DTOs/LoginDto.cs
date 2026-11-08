using System.ComponentModel.DataAnnotations;

namespace api.DTOs
{
    /* What the React app sends to log in */
    public class LoginDto
    {
        [Required(ErrorMessage = "Please enter your email.")]
        [EmailAddress(ErrorMessage = "Please enter a valid email address.")]
        public string Email { get; set; } = "";

        [Required(ErrorMessage = "Please enter your password.")]
        public string Password { get; set; } = "";
    }
}