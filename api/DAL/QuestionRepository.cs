using api.Data;
using api.Models;
using Microsoft.EntityFrameworkCore;

namespace api.DAL
{
    public class QuestionRepository : IQuestionRepository
    {
        private readonly ApplicationDbContext _db;
        private readonly ILogger<QuestionRepository> _logger;

        public QuestionRepository(ApplicationDbContext db, ILogger<QuestionRepository> logger)
        {
            _db = db;
            _logger = logger;
        }

        public async Task<Question?> GetWithQuizAndOptionsAsync(int id)
        {
            try
            {
                return await _db.Questions
                    .AsNoTracking()
                    .Include(q => q.Quiz)
                    .Include(q => q.AnswerOptions)
                    .FirstOrDefaultAsync(q => q.Id == id);
            }
            catch (Exception e)
            {
                _logger.LogError(e, "[QuestionRepository] Could not load question ID {QuestionId}.", id);
                return null;
            }
        }

        public async Task<Question?> GetForUpdateAsync(int id)
        {
            try
            {
                return await _db.Questions
                    .Include(q => q.Quiz)
                    .Include(q => q.AnswerOptions)
                    .FirstOrDefaultAsync(q => q.Id == id);
            }
            catch (Exception e)
            {
                _logger.LogError(e, "[QuestionRepository] Could not load question ID {QuestionId} for update.", id);
                return null;
            }
        }

        public async Task<Question?> GetForDeleteAsync(int id)
        {
            try
            {
                return await _db.Questions
                    .Include(q => q.Quiz)
                    .FirstOrDefaultAsync(q => q.Id == id);
            }
            catch (Exception e)
            {
                _logger.LogError(e, "[QuestionRepository] Could not load question ID {QuestionId} for delete.", id);
                return null;
            }
        }

        public async Task<bool> AddAsync(Question question)
        {
            try
            {
                _db.Questions.Add(question);
                await _db.SaveChangesAsync();
                return true;
            }
            catch (Exception e)
            {
                _logger.LogError(e, "[QuestionRepository] Could not add a question to quiz ID {QuizId}.", question.QuizId);
                return false;
            }
        }

        public async Task<bool> ReplaceAnswersAsync(Question question, IEnumerable<AnswerOption> newOptions)
        {
            try
            {
                /*
                 * Replace the answer options rather than trying to match old and new rows.
                 * The form allows answers to be added, removed and reordered.
                 */
                _db.AnswerOptions.RemoveRange(question.AnswerOptions.ToList());
                question.AnswerOptions = newOptions.ToList();

                await _db.SaveChangesAsync();
                return true;
            }
            catch (Exception e)
            {
                _logger.LogError(e, "[QuestionRepository] Could not update question ID {QuestionId}.", question.Id);
                return false;
            }
        }

        public async Task<bool> DeleteAsync(Question question)
        {
            try
            {
                _db.Questions.Remove(question);
                await _db.SaveChangesAsync();
                return true;
            }
            catch (Exception e)
            {
                _logger.LogError(e, "[QuestionRepository] Could not delete question ID {QuestionId}.", question.Id);
                return false;
            }
        }
    }
}