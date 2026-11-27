using api.Models;

namespace api.DAL
{
    /*
     * All database access for quizzes.
     * Read methods return null if the database call failed (the error is logged),
     * and write methods return false if the change could not be saved.
     */
    public interface IQuizRepository
    {
        /* The quizzes the user may see, filtered by the query. Sorted by title. */
        Task<List<Quiz>?> GetVisibleAsync(QuizQuery query);

        /* One quiz with tags, questions and answer options. Not tracked, for display only. */
        Task<Quiz?> GetDetailsAsync(int id);

        /* One quiz with questions and answer options for taking the quiz. Not tracked. */
        Task<Quiz?> GetForPlayAsync(int id);

        /* One quiz without related data. Tracked, so it can be changed or deleted. */
        Task<Quiz?> GetByIdAsync(int id);

        /* One quiz with its tags. Tracked, so it can be updated. */
        Task<Quiz?> GetByIdWithTagsAsync(int id);

        /* The questions of a quiz with their answer options, in the order they were added */
        Task<List<Question>> GetQuestionsAsync(int quizId);

        Task<bool> AddAsync(Quiz quiz);

        /* Saves changes made to a quiz that was loaded with a tracked method above */
        Task<bool> UpdateAsync(Quiz quiz);

        Task<bool> DeleteAsync(Quiz quiz);

        /* Point quizzes the user has taken but not got 100 % on, lowest best score first */
        Task<List<QuizSuggestion>?> GetImprovableAsync(string userId, int count);

        /* Visible quizzes with questions the user has never taken, point quizzes and newest first */
        Task<List<QuizSuggestion>?> GetNotTakenAsync(string userId, int count);

        /* The user's best score (0-100) on each quiz they have taken, by quiz ID */
        Task<Dictionary<int, int>?> GetBestScoresAsync(string userId);
    

        /* The latest attempts by students. ownerId: only on this teacher's quizzes, null: all quizzes that give points. */
        Task<List<StudentAttempt>?> GetRecentStudentAttemptsAsync(string? ownerId, int count);

        /* Attempts and average score by students on each quiz. ownerId: this teacher's quizzes, null: all point quizzes. */
        Task<List<QuizStats>?> GetQuizStatsAsync(string? ownerId);

        /* How many different students have taken the quizzes (same ownerId rule as above) */
        Task<int?> CountStudentsReachedAsync(string? ownerId);

        /* Numbers for the whole platform. Attempts are counted since sinceUtc. */
        Task<PlatformCounts?> GetPlatformCountsAsync(DateTime sinceUtc);

        /* The point quizzes students took most since sinceUtc */
        Task<List<QuizStats>?> GetPopularQuizzesAsync(DateTime sinceUtc, int count);

        /* Gives every quiz without a teacher to this teacher. Returns how many were moved, or null if it failed. */
        Task<int?> MoveQuizzesWithoutTeacherAsync(string teacherId);

        /* How students answered each question of the quiz, in the same order as on the edit page */
        Task<List<QuestionAnalysis>?> GetQuestionAnalysisAsync(int quizId);
    }
}