using api.Models;

namespace api.DAL
{
    /*
     * Database access for the users that administrators manage.
     * Read methods return null if the database call failed (the error is logged).
     */
    public interface IUserRepository
    {
        /* All users with roles and quiz/point numbers, sorted by email */
        Task<List<UserOverview>?> GetOverviewAsync();

        /* One user, or null if the user does not exist. Not tracked. */
        Task<ApplicationUser?> GetByIdAsync(string id);

        /* The names of the roles a user has */
        Task<List<string>?> GetRolesAsync(string userId);

        /* How many quizzes, attempts and points the user has */
        Task<UserDeletionCounts?> GetDeletionCountsAsync(string userId);

        /*
         * Deletes the user. Private quizzes are deleted too, public quizzes stay without
         * a creator. Attempts, points and personal tags are removed by the database (cascade).
         * Returns the number of private quizzes that were deleted, or null if it failed.
         */
        Task<int?> DeleteWithQuizzesAsync(string userId);
    }
}