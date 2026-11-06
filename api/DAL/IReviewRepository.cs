using api.Models;

namespace api.DAL
{
    /*
     * Database access for the "Review mistakes" feature.
     * Read methods return an empty result if the database call failed (the error is logged),
     * and write methods return false if the change could not be saved.
     */
    public interface IReviewRepository
    {
        /* Question IDs the user should review, oldest mistake first. With a quiz ID, only mistakes from that quiz. */
        Task<List<int>> GetMistakeQuestionIdsAsync(string userId, bool isAdmin, int limit, int? quizId = null);

        /* How many questions the user can review right now */
        Task<int> CountMistakesAsync(string userId, bool isAdmin);

        /* How many questions the user can review in each quiz, by quiz ID. Quizzes without mistakes are left out. */
        Task<Dictionary<int, int>> CountMistakesPerQuizAsync(string userId, bool isAdmin);

        /* The given questions with their answer options. Not tracked. */
        Task<List<Question>> GetQuestionsWithOptionsAsync(IEnumerable<int> questionIds);

        /* Stores the answers a user gave */
        Task<bool> AddAnswersAsync(IEnumerable<UserAnswer> answers);

        /* Clears the user's current mistakes in a quiz. They come back if the user answers wrong again. */
        Task<bool> AddDismissalAsync(string userId, int quizId);
    }
}