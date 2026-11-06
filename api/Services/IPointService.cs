using api.Models;

namespace api.Services
{
    /* Saves quiz attempts and gives points by the rules in PointRules */
    public interface IPointService
    {
        /* Returns null if the attempt could not be saved (the error is logged) */
        Task<PointResult?> SaveAttemptAsync(string userId, Quiz quiz, int correctAnswers, int totalQuestions);
    }
}