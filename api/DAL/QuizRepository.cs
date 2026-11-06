using api.Data;
using api.Models;
using Microsoft.EntityFrameworkCore;

namespace api.DAL
{
    public class QuizRepository : IQuizRepository
    {
        private readonly ApplicationDbContext _db;
        private readonly ILogger<QuizRepository> _logger;

        public QuizRepository(ApplicationDbContext db, ILogger<QuizRepository> logger)
        {
            _db = db;
            _logger = logger;
        }

        public async Task<List<Quiz>?> GetVisibleAsync(QuizQuery filter)
        {
            try
            {
                var userId = filter.UserId;

                var query = _db.Quizzes
                    .AsNoTracking()
                    .AsQueryable();

                /*
                 * Base privacy rule: administrators see every quiz, other users
                 * only see public quizzes and the quizzes they created themselves.
                 */
                if (!filter.IsAdmin)
                {
                    query = query.Where(q => q.IsPublic || q.CreatedByUserId == userId);
                }

                /* The scope is applied after the privacy rule, so it can only narrow the list */
                query = filter.Scope switch
                {
                    QuizScope.Public => query.Where(q => q.IsPublic),
                    QuizScope.Mine => query.Where(q => q.CreatedByUserId == userId),
                    _ => query
                };

                if (filter.ModuleId.HasValue)
                {
                    var moduleId = filter.ModuleId.Value;
                    query = query.Where(q => q.ModuleId == moduleId);
                }

                /* Search in the title and the description, not case-sensitive */
                if (!string.IsNullOrWhiteSpace(filter.SearchText))
                {
                    var text = filter.SearchText.Trim().ToLower();

                    query = query.Where(q =>
                        q.Title.ToLower().Contains(text) ||
                        (q.Description != null && q.Description.ToLower().Contains(text)));
                }

                if (!string.IsNullOrWhiteSpace(filter.Difficulty))
                {
                    var difficulty = filter.Difficulty;
                    query = query.Where(q => q.Difficulty == difficulty);
                }

                /*
                 * Only tags the user may see count, so a manually entered ID of another
                 * user's personal tag never shows which quizzes have that tag.
                 */
                if (filter.TagId.HasValue)
                {
                    var tagId = filter.TagId.Value;
                    var isAdmin = filter.IsAdmin;

                    query = query.Where(q => q.Tags.Any(t =>
                        t.Id == tagId &&
                        (isAdmin || t.OwnerUserId == null || t.OwnerUserId == userId)));
                }

                return await query
                    .Include(q => q.Module)
                    .Include(q => q.CreatedBy)
                    .Include(q => q.Questions)
                    .Include(q => q.Tags)
                    .OrderBy(q => q.Title)
                    .ToListAsync();
            }
            catch (Exception e)
            {
                _logger.LogError(e, "[QuizRepository] Could not load the visible quizzes for user {UserId}.", filter.UserId);
                return null;
            }
        }

        public async Task<Quiz?> GetDetailsAsync(int id)
        {
            try
            {
                return await _db.Quizzes
                    .AsNoTracking()
                    .Include(q => q.Tags)
                    .Include(q => q.Questions.OrderBy(question => question.Id))
                        .ThenInclude(question => question.AnswerOptions)
                    .FirstOrDefaultAsync(q => q.Id == id);
            }
            catch (Exception e)
            {
                _logger.LogError(e, "[QuizRepository] Could not load the details of quiz ID {QuizId}.", id);
                return null;
            }
        }

        public async Task<Quiz?> GetForPlayAsync(int id)
        {
            try
            {
                return await _db.Quizzes
                    .AsNoTracking()
                    .Include(q => q.Questions)
                        .ThenInclude(q => q.AnswerOptions)
                    .FirstOrDefaultAsync(q => q.Id == id);
            }
            catch (Exception e)
            {
                _logger.LogError(e, "[QuizRepository] Could not load quiz ID {QuizId} for playing.", id);
                return null;
            }
        }

        public async Task<Quiz?> GetByIdAsync(int id)
        {
            try
            {
                return await _db.Quizzes.FindAsync(id);
            }
            catch (Exception e)
            {
                _logger.LogError(e, "[QuizRepository] Could not load quiz ID {QuizId}.", id);
                return null;
            }
        }

        public async Task<Quiz?> GetByIdWithTagsAsync(int id)
        {
            try
            {
                return await _db.Quizzes
                    .Include(q => q.Tags)
                    .FirstOrDefaultAsync(q => q.Id == id);
            }
            catch (Exception e)
            {
                _logger.LogError(e, "[QuizRepository] Could not load quiz ID {QuizId} with its tags.", id);
                return null;
            }
        }

        public async Task<List<Question>> GetQuestionsAsync(int quizId)
        {
            try
            {
                return await _db.Questions
                    .AsNoTracking()
                    .Where(q => q.QuizId == quizId)
                    .OrderBy(q => q.Id)
                    .Include(q => q.AnswerOptions)
                    .ToListAsync();
            }
            catch (Exception e)
            {
                _logger.LogError(e, "[QuizRepository] Could not load the questions of quiz ID {QuizId}.", quizId);
                return new List<Question>();
            }
        }

        public async Task<bool> AddAsync(Quiz quiz)
        {
            try
            {
                _db.Quizzes.Add(quiz);
                await _db.SaveChangesAsync();
                return true;
            }
            catch (Exception e)
            {
                _logger.LogError(e, "[QuizRepository] Could not create the quiz '{QuizTitle}'.", quiz.Title);
                return false;
            }
        }

        public async Task<bool> UpdateAsync(Quiz quiz)
        {
            try
            {
                /* The quiz is already tracked, so SaveChanges only writes what really changed */
                await _db.SaveChangesAsync();
                return true;
            }
            catch (Exception e)
            {
                _logger.LogError(e, "[QuizRepository] Could not update quiz ID {QuizId}.", quiz.Id);
                return false;
            }
        }

        public async Task<bool> DeleteAsync(Quiz quiz)
        {
            try
            {
                /* Questions and answer options are deleted by the cascade delete rule */
                _db.Quizzes.Remove(quiz);
                await _db.SaveChangesAsync();
                return true;
            }
            catch (Exception e)
            {
                _logger.LogError(e, "[QuizRepository] Could not delete quiz ID {QuizId}.", quiz.Id);
                return false;
            }
        }

        public async Task<List<QuizSuggestion>?> GetImprovableAsync(string userId, int count)
        {
            try
            {
                return await _db.Quizzes
                    .AsNoTracking()
                    .Where(q => q.AwardsPoints && q.Questions.Any() &&
                                (q.IsPublic || q.CreatedByUserId == userId))
                    .Select(q => new
                    {
                        Quiz = q,
                        BestScore = q.Attempts
                            .Where(a => a.UserId == userId)
                            .Max(a => (int?)a.Score)
                    })
                    .Where(x => x.BestScore != null && x.BestScore < 100)
                    .OrderBy(x => x.BestScore)
                    .Take(count)
                    .Select(x => new QuizSuggestion(
                        x.Quiz.Id,
                        x.Quiz.Title,
                        x.Quiz.Module.Name,
                        x.Quiz.Difficulty,
                        x.Quiz.Questions.Count,
                        x.BestScore))
                    .ToListAsync();
            }
            catch (Exception e)
            {
                _logger.LogError(e, "[QuizRepository] Could not load quizzes to improve for user {UserId}.", userId);
                return null;
            }
        }

        public async Task<List<QuizSuggestion>?> GetNotTakenAsync(string userId, int count)
        {
            try
            {
                return await _db.Quizzes
                    .AsNoTracking()
                    .Where(q => q.Questions.Any() &&
                                (q.IsPublic || q.CreatedByUserId == userId) &&
                                !q.Attempts.Any(a => a.UserId == userId))
                    .OrderByDescending(q => q.AwardsPoints)
                    .ThenByDescending(q => q.Id)
                    .Take(count)
                    .Select(q => new QuizSuggestion(
                        q.Id,
                        q.Title,
                        q.Module.Name,
                        q.Difficulty,
                        q.Questions.Count,
                        null))
                    .ToListAsync();
            }
            catch (Exception e)
            {
                _logger.LogError(e, "[QuizRepository] Could not load quizzes to try for user {UserId}.", userId);
                return null;
            }
        }
        public async Task<Dictionary<int, int>?> GetBestScoresAsync(string userId)
        {
            try
            {
                /* One row per quiz with the highest score among the user's attempts */
                return await _db.QuizAttempts
                    .AsNoTracking()
                    .Where(a => a.UserId == userId)
                    .GroupBy(a => a.QuizId)
                    .Select(g => new { QuizId = g.Key, Best = g.Max(a => a.Score) })
                    .ToDictionaryAsync(x => x.QuizId, x => x.Best);
            }
            catch (Exception e)
            {
                _logger.LogError(e, "[QuizRepository] Could not load best scores for user {UserId}.", userId);
                return null;
            }
        }
    }
}