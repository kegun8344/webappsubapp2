// for user
using Microsoft.AspNetCore.Identity;

namespace api.Models
{
    public class ApplicationUser : IdentityUser
    {
        /* All quizzes this user has taken. Together with Quiz.Attempts this links users and quizzes (N:M). */
        public List<QuizAttempt> QuizAttempts { get; set; } = new();
    }
}