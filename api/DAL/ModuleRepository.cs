using api.Data;
using api.Models;
using Microsoft.EntityFrameworkCore;

namespace api.DAL
{
    public class ModuleRepository : IModuleRepository
    {
        private readonly ApplicationDbContext _db;
        private readonly ILogger<ModuleRepository> _logger;

        public ModuleRepository(ApplicationDbContext db, ILogger<ModuleRepository> logger)
        {
            _db = db;
            _logger = logger;
        }

        public async Task<List<Module>?> GetAllAsync()
        {
            try
            {
                return await _db.Modules
                    .AsNoTracking()
                    .OrderBy(m => m.Name)
                    .ToListAsync();
            }
            catch (Exception e)
            {
                _logger.LogError(e, "[ModuleRepository] Could not load the modules.");
                return null;
            }
        }

        public async Task<List<ModuleOverview>?> GetOverviewAsync(string? userId, bool isAdmin)
        {
            try
            {
                var modules = await _db.Modules
                    .AsNoTracking()
                    .OrderBy(m => m.Name)
                    .ToListAsync();

                /*
                 * Count only the quizzes the user may see: administrators see everything,
                 * others see public quizzes and their own. Otherwise the count would reveal
                 * that other users' private quizzes exist.
                 */
                var visibleCounts = await _db.Quizzes
                    .AsNoTracking()
                    .Where(quiz => isAdmin || quiz.IsPublic || quiz.CreatedByUserId == userId)
                    .GroupBy(quiz => quiz.ModuleId)
                    .Select(group => new { ModuleId = group.Key, Count = group.Count() })
                    .ToDictionaryAsync(row => row.ModuleId, row => row.Count);

                /* Progress only counts quizzes from teachers/admins, because only those give points */
                var officialQuizzes = await _db.Quizzes
                    .AsNoTracking()
                    .Where(quiz => quiz.AwardsPoints &&
                                   (isAdmin || quiz.IsPublic || quiz.CreatedByUserId == userId))
                    .Select(quiz => new
                    {
                        quiz.Id,
                        quiz.ModuleId,
                        quiz.Title,
                        quiz.Difficulty,
                        HasQuestions = quiz.Questions.Any()
                    })
                    .ToListAsync();

                /* The user's best score on each official quiz they have taken (several attempts count once) */
                var attempts = await _db.QuizAttempts
                    .AsNoTracking()
                    .Where(attempt => attempt.UserId == userId &&
                                      attempt.Quiz.AwardsPoints &&
                                      (isAdmin || attempt.Quiz.IsPublic || attempt.Quiz.CreatedByUserId == userId))
                    .Select(attempt => new { attempt.QuizId, attempt.Score })
                    .ToListAsync();

                var bestScores = attempts
                    .GroupBy(attempt => attempt.QuizId)
                    .ToDictionary(group => group.Key, group => group.Max(attempt => attempt.Score));

                return modules
                    .Select(module =>
                    {
                        var moduleQuizzes = officialQuizzes.Where(quiz => quiz.ModuleId == module.Id).ToList();
                        var taken = moduleQuizzes.Where(quiz => bestScores.ContainsKey(quiz.Id)).ToList();

                        /* The easiest quiz with questions that the user has not taken yet */
                        var next = moduleQuizzes
                            .Where(quiz => quiz.HasQuestions && !bestScores.ContainsKey(quiz.Id))
                            .OrderBy(quiz => DifficultyRank(quiz.Difficulty))
                            .ThenBy(quiz => quiz.Id)
                            .FirstOrDefault();

                        return new ModuleOverview(
                            module,
                            visibleCounts.GetValueOrDefault(module.Id),
                            moduleQuizzes.Count,
                            taken.Count,
                            taken.Count == 0
                                ? null
                                : (int)Math.Round(taken.Average(quiz => bestScores[quiz.Id])),
                            next?.Id,
                            next?.Title);
                    })
                    .ToList();
            }
            catch (Exception e)
            {
                _logger.LogError(e, "[ModuleRepository] Could not load the module overview for user {UserId}.", userId);
                return null;
            }
        }

        /* Easy quizzes come first, then Medium, then Hard */
        private static int DifficultyRank(string difficulty) => difficulty switch
        {
            "Easy" => 0,
            "Medium" => 1,
            "Hard" => 2,
            _ => 3
        };

        public async Task<Module?> GetByIdAsync(int id)
        {
            try
            {
                return await _db.Modules
                    .AsNoTracking()
                    .FirstOrDefaultAsync(m => m.Id == id);
            }
            catch (Exception e)
            {
                _logger.LogError(e, "[ModuleRepository] Could not load module ID {ModuleId}.", id);
                return null;
            }
        }

        public async Task<Module?> GetByIdWithQuizzesAsync(int id)
        {
            try
            {
                return await _db.Modules
                    .AsNoTracking()
                    .Include(m => m.Quizzes)
                    .FirstOrDefaultAsync(m => m.Id == id);
            }
            catch (Exception e)
            {
                _logger.LogError(e, "[ModuleRepository] Could not load module ID {ModuleId} with its quizzes.", id);
                return null;
            }
        }

        public async Task<Module?> GetForUpdateAsync(int id)
        {
            try
            {
                return await _db.Modules.FirstOrDefaultAsync(m => m.Id == id);
            }
            catch (Exception e)
            {
                _logger.LogError(e, "[ModuleRepository] Could not load module ID {ModuleId} for update.", id);
                return null;
            }
        }

        public async Task<bool> ExistsAsync(int id)
        {
            try
            {
                return await _db.Modules
                    .AsNoTracking()
                    .AnyAsync(m => m.Id == id);
            }
            catch (Exception e)
            {
                _logger.LogError(e, "[ModuleRepository] Could not check if module ID {ModuleId} exists.", id);
                return false;
            }
        }

        public async Task<bool> NameExistsAsync(string name, int? excludeId = null)
        {
            try
            {
                var lowerName = name.ToLower();

                return await _db.Modules
                    .AsNoTracking()
                    .AnyAsync(m => m.Name.ToLower() == lowerName &&
                                   (!excludeId.HasValue || m.Id != excludeId.Value));
            }
            catch (Exception e)
            {
                _logger.LogError(e, "[ModuleRepository] Could not check if the module name '{ModuleName}' is taken.", name);
                return false;
            }
        }

        public async Task<bool> HasQuizzesAsync(int id)
        {
            try
            {
                return await _db.Quizzes
                    .AsNoTracking()
                    .AnyAsync(q => q.ModuleId == id);
            }
            catch (Exception e)
            {
                _logger.LogError(e, "[ModuleRepository] Could not check if module ID {ModuleId} has quizzes.", id);
                return true;
            }
        }

        public async Task<bool> AddAsync(Module module)
        {
            try
            {
                _db.Modules.Add(module);
                await _db.SaveChangesAsync();
                return true;
            }
            catch (Exception e)
            {
                _logger.LogError(e, "[ModuleRepository] Could not create the module '{ModuleName}'.", module.Name);
                return false;
            }
        }

        public async Task<bool> UpdateAsync(Module module)
        {
            try
            {
                /* The module is already tracked, so SaveChanges only writes what really changed */
                await _db.SaveChangesAsync();
                return true;
            }
            catch (Exception e)
            {
                _logger.LogError(e, "[ModuleRepository] Could not update module ID {ModuleId}.", module.Id);
                return false;
            }
        }

        public async Task<bool> DeleteAsync(Module module)
        {
            try
            {
                /* DeleteBehavior.Restrict is a safety net if quizzes were added after the check */
                _db.Modules.Remove(module);
                await _db.SaveChangesAsync();
                return true;
            }
            catch (Exception e)
            {
                _logger.LogError(e, "[ModuleRepository] Could not delete module ID {ModuleId}.", module.Id);
                return false;
            }
        }
    }
}