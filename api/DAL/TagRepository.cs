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

        public async Task<List<TagUsage>?> GetOwnWithUsageAsync(string userId)
        {
            try
            {
                return await _db.Tags
                    .AsNoTracking()
                    .Where(t => t.OwnerUserId == userId)
                    .OrderBy(t => t.Name)
                    .Select(t => new TagUsage(t.Id, t.Name, t.Color, t.Quizzes.Count))
                    .ToListAsync();
            }
            catch (Exception e)
            {
                _logger.LogError(e, "[TagRepository] Could not load the tags of user {UserId}.", userId);
                return null;
            }
        }

        public async Task<bool> UpdateAsync(Tag tag)
        {
            try
            {
                _db.Tags.Update(tag);
                await _db.SaveChangesAsync();
                return true;
            }
            catch (Exception e)
            {
                _logger.LogError(e, "[TagRepository] Could not update tag ID {TagId}.", tag.Id);
                return false;
            }
        }

        public async Task<bool> DeleteAsync(Tag tag)
        {
            try
            {
                _db.Tags.Remove(tag);
                await _db.SaveChangesAsync();
                return true;
            }
            catch (Exception e)
            {
                _logger.LogError(e, "[TagRepository] Could not delete tag ID {TagId}.", tag.Id);
                return false;
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