using api.Models;

namespace api.DAL
{
    /*
     * All database access for questions and their answer options.
     * Read methods return null if the question was not found or the database call failed
     * (the error is logged), and write methods return false if the change could not be saved.
     */
    public interface IQuestionRepository
    {
        /* One question with its quiz and answer options. Not tracked, for display only. */
        Task<Question?> GetWithQuizAndOptionsAsync(int id);

        /* One question with its quiz and answer options that can be changed. Tracked. */
        Task<Question?> GetForUpdateAsync(int id);

        /* One question with its quiz, for deleting. Tracked. */
        Task<Question?> GetForDeleteAsync(int id);

        Task<bool> AddAsync(Question question);

        /* Replaces all answer options of a tracked question with newOptions and saves */
        Task<bool> ReplaceAnswersAsync(Question question, IEnumerable<AnswerOption> newOptions);

        /* Deletes the question. Its answer options are deleted by cascade. */
        Task<bool> DeleteAsync(Question question);
    }
}