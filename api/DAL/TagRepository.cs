using api.Data;
using api.Models;
using Microsoft.EntityFrameworkCore;

namespace api.DAL
{
    public class TagRepository : ITagRepository
    {
        private readonly ApplicationDbContext _db;
        private readonly ILogger<TagRepository> _logger;

        public TagRepository(ApplicationDbContext db, ILogger<TagRepository> logger)
        {
            _db = db;
            _logger = logger;
        }

        public async Task<List<Tag>?> GetAvailableForUserAsync(string? userId, IEnumerable<int> excludeTagIds)
        {
            try
            {
                var excluded = excludeTagIds.ToList();

                return await _db.Tags
                    .AsNoTracking()
                    .Where(tag =>
                        !excluded.Contains(tag.Id) &&
                        (tag.OwnerUserId == null || tag.OwnerUserId == userId))
                    .ToListAsync();
            }
            catch (Exception e)
            {
                _logger.LogError(e, "[TagRepository] Could not load the available tags for user {UserId}.", userId);
                return null;
            }
        }

        public async Task<Tag?> GetByIdAsync(int id)
        {
            try
            {
                return await _db.Tags.FirstOrDefaultAsync(t => t.Id == id);
            }
            catch (Exception e)
            {
                _logger.LogError(e, "[TagRepository] Could not load tag ID {TagId}.", id);
                return null;
            }
        }

        public async Task<Tag?> FindSharedByNameAsync(string lowerName)
        {
            try
            {
                return await _db.Tags.FirstOrDefaultAsync(t =>
                    t.OwnerUserId == null && t.Name.ToLower() == lowerName);
            }
            catch (Exception e)
            {
                _logger.LogError(e, "[TagRepository] Could not look up the shared tag '{TagName}'.", lowerName);
                return null;
            }
        }

        public async Task<Tag?> FindAvailableByNameAsync(string? userId, string lowerName)
        {
            try
            {
                return await _db.Tags.FirstOrDefaultAsync(t =>
                    (t.OwnerUserId == null || t.OwnerUserId == userId) &&
                    t.Name.ToLower() == lowerName);
            }
            catch (Exception e)
            {
                _logger.LogError(e, "[TagRepository] Could not look up the tag '{TagName}'.", lowerName);
                return null;
            }
        }
    }
}