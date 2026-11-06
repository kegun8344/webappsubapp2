using api.Data;
using api.Models;
using api.Services;
using Microsoft.EntityFrameworkCore;

namespace api.DAL
{
    public class ReviewRepository : IReviewRepository
    {
        private readonly ApplicationDbContext _db;
        private readonly ILogger<ReviewRepository> _logger;

        public ReviewRepository(ApplicationDbContext db, ILogger<ReviewRepository> logger)
        {
            _db = db;
            _logger = logger;
        }

        public async Task<List<int>> GetMistakeQuestionIdsAsync(string userId, bool isAdmin, int limit, int? quizId = null)
        {
            try
            {
                /*
                 * Only questions in quizzes the user may see are included (administrators see
                 * everything, others see public quizzes and their own), so a quiz that was made
                 * private later cannot leak its questions through a review.
                 */
                var answers = await _db.UserAnswers
                    .AsNoTracking()
                    .Where(a => a.UserId == userId &&
                                (isAdmin ||
                                 a.Question.Quiz.IsPublic ||
                                 a.Question.Quiz.CreatedByUserId == userId))
                    /* Only one quiz when a quiz ID is given */
                    .Where(a => quizId == null || a.Question.QuizId == quizId)
                    /*
                    * Answers from before the user cleared the mistakes in that quiz are skipped.
                    * A new wrong answer after the clear is newer, so that question comes back.
                    */
                    .Where(a => !_db.ReviewDismissals.Any(d =>
                        d.UserId == userId &&
                        d.QuizId == a.Question.QuizId &&
                        d.DismissedAt >= a.AnsweredAt))
                    .Select(a => new AnswerRecord(a.Id, a.QuestionId, a.IsCorrect, a.AnsweredAt))
                    .ToListAsync();

                /* The rule for which answers count as open mistakes is in ReviewRules */
                return ReviewRules.SelectMistakes(answers, limit);
            }
            catch (Exception e)
            {
                _logger.LogError(e, "[ReviewRepository] Could not load the review questions for user {UserId}.", userId);
                return new List<int>();
            }
        }

        public async Task<int> CountMistakesAsync(string userId, bool isAdmin)
        {
            return (await GetMistakeQuestionIdsAsync(userId, isAdmin, int.MaxValue)).Count;
        }

        public async Task<Dictionary<int, int>> CountMistakesPerQuizAsync(string userId, bool isAdmin)
        {
            /* Same rule as the review itself, so the count on the card always matches the questions shown */
            var questionIds = await GetMistakeQuestionIdsAsync(userId, isAdmin, int.MaxValue);

            if (questionIds.Count == 0)
            {
                return new Dictionary<int, int>();
            }

            try
            {
                return await _db.Questions
                    .AsNoTracking()
                    .Where(q => questionIds.Contains(q.Id))
                    .GroupBy(q => q.QuizId)
                    .Select(g => new { QuizId = g.Key, Count = g.Count() })
                    .ToDictionaryAsync(x => x.QuizId, x => x.Count);
            }
            catch (Exception e)
            {
                _logger.LogError(e, "[ReviewRepository] Could not count the mistakes per quiz for user {UserId}.", userId);
                return new Dictionary<int, int>();
            }
        }

        public async Task<List<Question>> GetQuestionsWithOptionsAsync(IEnumerable<int> questionIds)
        {
            try
            {
                var ids = questionIds.ToList();

                return await _db.Questions
                    .AsNoTracking()
                    .Include(q => q.AnswerOptions)
                    .Include(q => q.Quiz)
                    .Where(q => ids.Contains(q.Id))
                    .ToListAsync();
            }
            catch (Exception e)
            {
                _logger.LogError(e, "[ReviewRepository] Could not load the questions for review.");
                return new List<Question>();
            }
        }

        public async Task<bool> AddAnswersAsync(IEnumerable<UserAnswer> answers)
        {
            try
            {
                _db.UserAnswers.AddRange(answers);
                await _db.SaveChangesAsync();
                return true;
            }
            catch (Exception e)
            {
                _logger.LogError(e, "[ReviewRepository] Could not store the answers.");
                return false;
            }
        }

        public async Task<bool> AddDismissalAsync(string userId, int quizId)
        {
            try
            {
                _db.ReviewDismissals.Add(new ReviewDismissal
                {
                    UserId = userId,
                    QuizId = quizId,
                    DismissedAt = DateTime.UtcNow
                });

                await _db.SaveChangesAsync();
                return true;
            }
            catch (Exception e)
            {
                _logger.LogError(e, "[ReviewRepository] Could not clear the mistakes in quiz ID {QuizId} for user {UserId}.", quizId, userId);
                return false;
            }
        }
    }
}