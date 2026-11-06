using api.Models;

namespace api.DAL
{
    /*
     * All database access for tags.
     * Read methods return null if the database call failed (the error is logged).
     * The tags returned by the Find methods are tracked on purpose, so they can be added
     * to a tracked quiz without EF trying to insert them again.
     */
    public interface ITagRepository
    {
        /*
         * The tags the user can add to a quiz: shared tags and the user's own personal tags,
         * except the tags in excludeTagIds. Other users' personal tags are never included.
         */
        Task<List<Tag>?> GetAvailableForUserAsync(string? userId, IEnumerable<int> excludeTagIds);

        Task<Tag?> GetByIdAsync(int id);

        /* A shared tag with this name, case-insensitive (the name must be lower case) */
        Task<Tag?> FindSharedByNameAsync(string lowerName);

        /* A shared tag or one of the user's own tags with this name (the name must be lower case) */
        Task<Tag?> FindAvailableByNameAsync(string? userId, string lowerName);
    }
}