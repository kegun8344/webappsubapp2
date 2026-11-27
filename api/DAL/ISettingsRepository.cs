using api.Models;

namespace api.DAL
{
    /*
     * Database access for a user's own settings.
     * Methods return null or false if the database call failed (the error is logged).
     */
    public interface ISettingsRepository
    {
        /* The user's settings, or the default settings if the user has not saved any yet */
        Task<UserSettings?> GetUserSettingsAsync(string userId);

        /* Saves the settings: adds a row the first time, updates it after that */
        Task<bool> UpdateUserSettingsAsync(UserSettings settings);
    }
}
