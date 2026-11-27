using api.Data;
using api.Models;
using Microsoft.EntityFrameworkCore;

namespace api.DAL
{
    public class SettingsRepository : ISettingsRepository
    {
        private readonly ApplicationDbContext _db;
        private readonly ILogger<SettingsRepository> _logger;

        public SettingsRepository(ApplicationDbContext db, ILogger<SettingsRepository> logger)
        {
            _db = db;
            _logger = logger;
        }

        public async Task<UserSettings?> GetUserSettingsAsync(string userId)
        {
            try
            {
                var settings = await _db.UserSettings
                    .AsNoTracking()
                    .FirstOrDefaultAsync(s => s.UserId == userId);

                /* No row yet means the user has never saved settings, so the defaults are used */
                return settings ?? new UserSettings { UserId = userId };
            }
            catch (Exception e)
            {
                _logger.LogError(e, "[SettingsRepository] Could not load the settings for user {UserId}.", userId);
                return null;
            }
        }

        public async Task<bool> UpdateUserSettingsAsync(UserSettings settings)
        {
            try
            {
                var existing = await _db.UserSettings
                    .FirstOrDefaultAsync(s => s.UserId == settings.UserId);

                if (existing == null)
                {
                    _db.UserSettings.Add(settings);
                }
                else
                {
                    /* Copy the values, so the row that is already tracked is updated */
                    existing.DarkModeEnabled = settings.DarkModeEnabled;
                    existing.ReduceMotion = settings.ReduceMotion;
                    existing.ColorVisionMode = settings.ColorVisionMode;
                }

                await _db.SaveChangesAsync();
                return true;
            }
            catch (Exception e)
            {
                _logger.LogError(e, "[SettingsRepository] Could not save the settings for user {UserId}.", settings.UserId);
                return false;
            }
        }
    }
}
