using api.Models;

namespace api.DAL
{
    /*
     * All database access for modules.
     * Read methods return null if the database call failed (the error is logged),
     * and write methods return false if the change could not be saved.
     */
    public interface IModuleRepository
    {
        /* All modules sorted by name */
        Task<List<Module>?> GetAllAsync();

        /* All modules with the quiz numbers for one user, sorted by name */
        Task<List<ModuleOverview>?> GetOverviewAsync(string? userId, bool isAdmin);

        /* One module without related data. Not tracked, for display only. */
        Task<Module?> GetByIdAsync(int id);

        /* One module with its quizzes. Not tracked, for display only. */
        Task<Module?> GetByIdWithQuizzesAsync(int id);

        /* One module that can be changed or deleted. Tracked. */
        Task<Module?> GetForUpdateAsync(int id);

        /* True if a module with this ID exists (false if the lookup failed) */
        Task<bool> ExistsAsync(int id);

        /* True if another module already uses the name. The module excludeId is ignored (for editing). */
        Task<bool> NameExistsAsync(string name, int? excludeId = null);

        /* True if the module contains quizzes. If the lookup fails it returns true, so nothing is deleted by mistake. */
        Task<bool> HasQuizzesAsync(int id);

        Task<bool> AddAsync(Module module);

        /* Saves changes made to a module that was loaded with GetForUpdateAsync */
        Task<bool> UpdateAsync(Module module);

        Task<bool> DeleteAsync(Module module);
    }
}