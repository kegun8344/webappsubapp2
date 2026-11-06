namespace api.Services
{
    /* Gets a user's badges, calculated from their attempts and points */
    public interface IBadgeService
    {
        /* Returns null if the numbers could not be loaded (the error is logged) */
        Task<BadgeStats?> GetStatsAsync(string userId);

        Task<List<BadgeProgress>?> GetBadgesAsync(string userId);
    }
}