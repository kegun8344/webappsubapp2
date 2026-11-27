// for user
using Microsoft.AspNetCore.Identity;

namespace api.Models
{
    public class ApplicationUser : IdentityUser
    {
        /* When the account was created (UTC), used for "New users" on the admin Home page. Same as Subapp 1. */
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        /* All quizzes this user has taken. Together with Quiz.Attempts this links users and quizzes (N:M). */
        public List<QuizAttempt> QuizAttempts { get; set; } = new();
    }
}