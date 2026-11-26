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
     * Quizzes for the React app: the filtered list, one quiz, and create, update and delete.
     * Only signed-in users have access. Who may see or change a quiz is checked here on the server
     * (see QuizRules), so hiding a button in React is never the only protection.
     * The controller only uses the repository interfaces, never the database directly.
     */
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class QuizzesController : ControllerBase
    {
        private readonly IQuizRepository quizRepository;
        private readonly IModuleRepository moduleRepository;
        private readonly ITagRepository tagRepository;
        private readonly ILogger<QuizzesController> logger;

        public QuizzesController(
            IQuizRepository quizRepo,
            IModuleRepository moduleRepo,
            ITagRepository tagRepo,
            ILogger<QuizzesController> quizLogger)
        {
            quizRepository = quizRepo;
            moduleRepository = moduleRepo;
            tagRepository = tagRepo;
            logger = quizLogger;
        }

        // GET: /api/quizzes?view=all&moduleId=1&search=ef&difficulty=Easy&tagId=3
        // The quizzes the signed-in user may see, narrowed by the filters. All filters are optional.
        [HttpGet]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<ActionResult<List<QuizListItemDto>>> GetAll(
            string view = "all",
            int? moduleId = null,
            string? search = null,
            string? difficulty = null,
            int? tagId = null)
        {
            var userId = CurrentUserId;

            if (userId == null)
            {
                return Unauthorized();
            }

            var isAdmin = User.IsInRole("Admin");

            /* Unknown values are cleaned up or ignored, so a hand-made URL cannot break the list */
            var selectedView = QuizRules.NormalizeView(view);

            var scope = selectedView switch
            {
                "public" => QuizScope.Public,
                "mine" => QuizScope.Mine,
                _ => QuizScope.All
            };

            /* A module ID that does not exist is treated as no module filter */
            int? selectedModuleId = moduleId;

            if (selectedModuleId.HasValue && !await moduleRepository.ExistsAsync(selectedModuleId.Value))
            {
                selectedModuleId = null;
            }

            /* The tag filter only works with tags the user may see (shared tags and their own) */
            int? selectedTagId = tagId;

            if (selectedTagId.HasValue)
            {
                var availableTags = await tagRepository.GetAvailableForUserAsync(userId, Array.Empty<int>());

                if (availableTags == null || !availableTags.Any(tag => tag.Id == selectedTagId.Value))
                {
                    selectedTagId = null;
                }
            }

            var quizzes = await quizRepository.GetVisibleAsync(
                new QuizQuery(
                    userId,
                    isAdmin,
                    scope,
                    selectedModuleId,
                    QuizRules.NormalizeSearch(search),
                    QuizRules.NormalizeDifficulty(difficulty),
                    selectedTagId));

            if (quizzes == null)
            {
                logger.LogError("The quiz list could not be loaded for user {UserId}.", userId);
                return Problem("The quizzes could not be loaded.", statusCode: StatusCodes.Status500InternalServerError);
            }

            /* If the scores cannot be loaded the cards simply show no score */
            var bestScores = await quizRepository.GetBestScoresAsync(userId) ?? new Dictionary<int, int>();

            return quizzes
                .Select(quiz => QuizListItemDto.FromQuiz(
                    quiz,
                    userId,
                    isAdmin,
                    bestScores.TryGetValue(quiz.Id, out var score) ? score : null))
                .ToList();
        }

        // GET: /api/quizzes/5
        // One quiz with its questions. The correct answers are only included for people who may edit the quiz.
        [HttpGet("{id:int}")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<ActionResult<QuizDetailDto>> GetById(int id)
        {
            var userId = CurrentUserId;
            var isAdmin = User.IsInRole("Admin");

            var quiz = await quizRepository.GetDetailsAsync(id);

            if (quiz == null)
            {
                logger.LogWarning("Quiz ID {QuizId} was requested, but it was not found.", id);
                return NotFound();
            }

            /* A private quiz of someone else looks the same as a quiz that does not exist */
            if (!QuizRules.CanView(quiz, userId, isAdmin))
            {
                logger.LogWarning("User {UserId} asked for private quiz ID {QuizId} without permission.", userId, id);
                return NotFound();
            }

            return QuizDetailDto.FromQuiz(quiz, userId, isAdmin);
        }

        // POST: /api/quizzes
        [HttpPost]
        [ProducesResponseType(StatusCodes.Status201Created)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<ActionResult<QuizDetailDto>> Create(QuizFormDto dto)
        {
            var userId = CurrentUserId;

            if (userId == null)
            {
                return Unauthorized();
            }

            /* The range check only says the ID is positive. The module must also exist. */
            if (!await moduleRepository.ExistsAsync(dto.ModuleId))
            {
                ModelState.AddModelError(nameof(dto.ModuleId), "Choose a valid module.");
                return ValidationProblem(ModelState);
            }

            var isOfficial = QuizRules.IsOfficialAuthor(User.IsInRole("Teacher"), User.IsInRole("Admin"));

            /*
             * Owner, visibility and points come from the server. A student's quiz is private
             * practice, a teacher's quiz is public and gives points.
             */
            var quiz = new Quiz
            {
                Title = dto.Title.Trim(),
                Description = string.IsNullOrWhiteSpace(dto.Description) ? null : dto.Description.Trim(),
                Difficulty = dto.Difficulty,
                ModuleId = dto.ModuleId,
                CreatedByUserId = userId,
                AwardsPoints = isOfficial,
                IsPublic = isOfficial
            };

            if (!await quizRepository.AddAsync(quiz))
            {
                /* The repository has already logged the database error */
                return Problem("The quiz could not be created. Please try again.", statusCode: StatusCodes.Status500InternalServerError);
            }

            logger.LogInformation(
                "Quiz ID {QuizId} with title '{QuizTitle}' was created by user {UserId} in module ID {ModuleId}.",
                quiz.Id,
                quiz.Title,
                userId,
                quiz.ModuleId);

            return CreatedAtAction(
                nameof(GetById),
                new { id = quiz.Id },
                QuizDetailDto.FromQuiz(quiz, userId, User.IsInRole("Admin")));
        }

        // PUT: /api/quizzes/5
        [HttpPut("{id:int}")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> Update(int id, QuizFormDto dto)
        {
            var userId = CurrentUserId;

            /* The stored quiz decides who the owner is, not anything in the request */
            var quiz = await quizRepository.GetByIdAsync(id);

            if (quiz == null)
            {
                logger.LogWarning("Quiz ID {QuizId} was not found during update.", id);
                return NotFound();
            }

            if (!QuizRules.CanManage(quiz, userId, User.IsInRole("Admin")))
            {
                logger.LogWarning("User {UserId} tried to update quiz ID {QuizId} without permission.", userId, id);
                return StatusCode(StatusCodes.Status403Forbidden);
            }

            if (!await moduleRepository.ExistsAsync(dto.ModuleId))
            {
                ModelState.AddModelError(nameof(dto.ModuleId), "Choose a valid module.");
                return ValidationProblem(ModelState);
            }

            /* Only the allowed fields are copied. Owner, visibility and points stay as they are. */
            quiz.Title = dto.Title.Trim();
            quiz.Description = string.IsNullOrWhiteSpace(dto.Description) ? null : dto.Description.Trim();
            quiz.Difficulty = dto.Difficulty;
            quiz.ModuleId = dto.ModuleId;

            if (!await quizRepository.UpdateAsync(quiz))
            {
                return Problem("The quiz could not be updated. Please try again.", statusCode: StatusCodes.Status500InternalServerError);
            }

            logger.LogInformation("Quiz ID {QuizId} was updated by user {UserId}.", id, userId);

            return NoContent();
        }

        // DELETE: /api/quizzes/5
        // The questions and answer options are deleted with the quiz (cascade delete).
        [HttpDelete("{id:int}")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> Delete(int id)
        {
            var userId = CurrentUserId;

            var quiz = await quizRepository.GetByIdAsync(id);

            if (quiz == null)
            {
                logger.LogWarning("Delete requested for quiz ID {QuizId}, but the quiz was not found.", id);
                return NotFound();
            }

            if (!QuizRules.CanManage(quiz, userId, User.IsInRole("Admin")))
            {
                logger.LogWarning("User {UserId} tried to delete quiz ID {QuizId} without permission.", userId, id);
                return StatusCode(StatusCodes.Status403Forbidden);
            }

            if (!await quizRepository.DeleteAsync(quiz))
            {
                return Problem("The quiz could not be deleted. Please try again.", statusCode: StatusCodes.Status500InternalServerError);
            }

            logger.LogInformation("Quiz ID {QuizId} with title '{QuizTitle}' was deleted by user {UserId}.", id, quiz.Title, userId);

            return NoContent();
        }

        /* The signed-in user's Identity ID, read from the token */
        private string? CurrentUserId => User.FindFirstValue(ClaimTypes.NameIdentifier);
    }
}