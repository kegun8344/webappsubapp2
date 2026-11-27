using System.Security.Claims;
using api.DAL;
using api.DTOs;
using api.Models;
using api.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace api.Controllers
{
    /*
     * Questions and their answer options, used by the quiz edit page.
     * Only the quiz creator or an administrator may add, change or delete questions
     * (the same rule as QuestionController in Subapp 1). Every request checks this against
     * the quiz stored in the database, never against anything sent from the browser.
     */
    [ApiController]
    [Route("api")]
    [Authorize]
    public class QuestionsController : ControllerBase
    {
        private readonly IQuestionRepository questionRepository;
        private readonly IQuizRepository quizRepository;
        private readonly ILogger<QuestionsController> logger;

        public QuestionsController(
            IQuestionRepository questionRepo,
            IQuizRepository quizRepo,
            ILogger<QuestionsController> questionLogger)
        {
            questionRepository = questionRepo;
            quizRepository = quizRepo;
            logger = questionLogger;
        }

        // GET: /api/questions/5
        // One question with the correct answer marked, for the edit form.
        [HttpGet("questions/{id:int}")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<ActionResult<QuestionDto>> GetById(int id)
        {
            var question = await questionRepository.GetWithQuizAndOptionsAsync(id);

            if (question == null)
            {
                logger.LogWarning("Question ID {QuestionId} was requested, but it was not found.", id);
                return NotFound();
            }

            if (!CanManage(question.Quiz))
            {
                logger.LogWarning("User {UserId} asked for question ID {QuestionId} without permission.", CurrentUserId, id);
                return StatusCode(StatusCodes.Status403Forbidden);
            }

            return QuestionDto.FromQuestion(question, includeCorrectAnswers: true);
        }

        // POST: /api/quizzes/5/questions
        [HttpPost("quizzes/{quizId:int}/questions")]
        [ProducesResponseType(StatusCodes.Status201Created)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<ActionResult<QuestionDto>> Create(int quizId, QuestionFormDto dto)
        {
            var quiz = await quizRepository.GetByIdAsync(quizId);

            if (quiz == null)
            {
                logger.LogWarning("A question could not be added because quiz ID {QuizId} was not found.", quizId);
                return NotFound();
            }

            if (!CanManage(quiz))
            {
                logger.LogWarning("User {UserId} tried to add a question to quiz ID {QuizId} without permission.", CurrentUserId, quizId);
                return StatusCode(StatusCodes.Status403Forbidden);
            }

            if (!ValidateAnswers(dto))
            {
                logger.LogWarning("A question for quiz ID {QuizId} was not saved because the answers were invalid.", quizId);
                return ValidationProblem(ModelState);
            }

            var question = new Question
            {
                QuizId = quiz.Id,
                Text = dto.Text.Trim(),
                Context = CleanContext(dto.Context),
                AnswerOptions = BuildOptions(dto)
            };

            if (!await questionRepository.AddAsync(question))
            {
                /* The repository has already logged the database error */
                return Problem("The question could not be saved. Please try again.", statusCode: StatusCodes.Status500InternalServerError);
            }

            logger.LogInformation(
                "Question ID {QuestionId} with {AnswerCount} answers was added to quiz ID {QuizId} by user {UserId}.",
                question.Id,
                question.AnswerOptions.Count,
                quizId,
                CurrentUserId);

            return CreatedAtAction(
                nameof(GetById),
                new { id = question.Id },
                QuestionDto.FromQuestion(question, includeCorrectAnswers: true));
        }

        // PUT: /api/questions/5
        // Changes the text and replaces all answer options.
        [HttpPut("questions/{id:int}")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<ActionResult<QuestionDto>> Update(int id, QuestionFormDto dto)
        {
            var question = await questionRepository.GetForUpdateAsync(id);

            if (question == null)
            {
                logger.LogWarning("Question ID {QuestionId} could not be updated because it was not found.", id);
                return NotFound();
            }

            if (!CanManage(question.Quiz))
            {
                logger.LogWarning(
                    "User {UserId} tried to update question ID {QuestionId} in quiz ID {QuizId} without permission.",
                    CurrentUserId,
                    id,
                    question.QuizId);
                return StatusCode(StatusCodes.Status403Forbidden);
            }

            if (!ValidateAnswers(dto))
            {
                logger.LogWarning("Question ID {QuestionId} was not updated because the answers were invalid.", id);
                return ValidationProblem(ModelState);
            }

            question.Text = dto.Text.Trim();
            question.Context = CleanContext(dto.Context);

            /* The old options are removed and the new ones saved, in one call */
            if (!await questionRepository.ReplaceAnswersAsync(question, BuildOptions(dto)))
            {
                return Problem("The question could not be saved. Please try again.", statusCode: StatusCodes.Status500InternalServerError);
            }

            logger.LogInformation(
                "Question ID {QuestionId} in quiz ID {QuizId} was updated by user {UserId}.",
                id,
                question.QuizId,
                CurrentUserId);

            return QuestionDto.FromQuestion(question, includeCorrectAnswers: true);
        }

        // DELETE: /api/questions/5
        // The answer options are deleted with the question (cascade delete).
        [HttpDelete("questions/{id:int}")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> Delete(int id)
        {
            var question = await questionRepository.GetForDeleteAsync(id);

            if (question == null)
            {
                logger.LogWarning("Delete requested for question ID {QuestionId}, but it was not found.", id);
                return NotFound();
            }

            if (!CanManage(question.Quiz))
            {
                logger.LogWarning(
                    "User {UserId} tried to delete question ID {QuestionId} from quiz ID {QuizId} without permission.",
                    CurrentUserId,
                    id,
                    question.QuizId);
                return StatusCode(StatusCodes.Status403Forbidden);
            }

            var quizId = question.QuizId;

            if (!await questionRepository.DeleteAsync(question))
            {
                return Problem("The question could not be deleted. Please try again.", statusCode: StatusCodes.Status500InternalServerError);
            }

            logger.LogInformation("Question ID {QuestionId} was deleted from quiz ID {QuizId} by user {UserId}.", id, quizId, CurrentUserId);

            return NoContent();
        }

        /* Applies QuestionRules and puts every problem into ModelState. Returns true if the answers are valid. */
        private bool ValidateAnswers(QuestionFormDto dto)
        {
            foreach (var error in QuestionRules.Validate(dto.Answers, dto.CorrectAnswerIndex))
            {
                ModelState.AddModelError(error.Field, error.Message);
            }

            return ModelState.IsValid;
        }

        /* One AnswerOption per answer text, in the same order. Only the chosen one is correct. */
        private static List<AnswerOption> BuildOptions(QuestionFormDto dto)
        {
            return dto.Answers
                .Select((text, index) => new AnswerOption
                {
                    Text = (text ?? string.Empty).Trim(),
                    IsCorrect = index == dto.CorrectAnswerIndex
                })
                .ToList();
        }

        /* Empty or whitespace-only context is saved as null */
        private static string? CleanContext(string? context) =>
            string.IsNullOrWhiteSpace(context) ? null : context.Trim();

        private bool CanManage(Quiz quiz)
        {
            return QuizRules.CanManage(quiz, CurrentUserId, User.IsInRole("Admin"));
        }

        /* The signed-in user's Identity ID, read from the token */
        private string? CurrentUserId => User.FindFirstValue(ClaimTypes.NameIdentifier);
    }
}