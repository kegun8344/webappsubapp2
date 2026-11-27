using System.Security.Claims;
using api.DAL;
using api.DTOs;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace api.Controllers
{
    /*
     * Moves quizzes to another teacher, e.g. when a teacher's account has been deleted.
     * Only administrators can do this. Questions, attempts and points stay with the quiz.
     * The same rules as QuizTeacherController in Subapp 1.
     */
    [ApiController]
    [Route("api/quizzes")]
    [Authorize(Roles = "Admin")]
    public class QuizTeacherController : ControllerBase
    {
        private readonly IQuizRepository quizRepository;
        private readonly IUserRepository userRepository;
        private readonly ILogger<QuizTeacherController> logger;

        public QuizTeacherController(
            IQuizRepository quizRepo,
            IUserRepository userRepo,
            ILogger<QuizTeacherController> teacherLogger)
        {
            quizRepository = quizRepo;
            userRepository = userRepo;
            logger = teacherLogger;
        }

        // GET: /api/quizzes/5/teacher - the quiz, its teacher and the teachers it can move to
        [HttpGet("{id:int}/teacher")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<ActionResult<MoveQuizInfoDto>> GetMoveInfo(int id)
        {
            var quiz = await quizRepository.GetByIdAsync(id);

            if (quiz == null)
            {
                logger.LogWarning("Move requested for quiz ID {QuizId}, but the quiz was not found.", id);
                return NotFound();
            }

            var teachers = await userRepository.GetTeachersAsync();

            if (teachers == null)
            {
                return Problem("The teachers could not be loaded. Please try again.", statusCode: StatusCodes.Status500InternalServerError);
            }

            return new MoveQuizInfoDto
            {
                QuizId = quiz.Id,
                Title = quiz.Title,
                CurrentTeacherEmail = quiz.CreatedByUserId == null
                    ? null
                    : teachers.FirstOrDefault(t => t.Id == quiz.CreatedByUserId)?.Email ?? "an administrator",
                Teachers = teachers
                    .Where(t => t.Id != quiz.CreatedByUserId)
                    .Select(t => new TeacherOptionDto { Id = t.Id, Email = t.Email })
                    .ToList()
            };
        }

        // PUT: /api/quizzes/5/teacher
        [HttpPut("{id:int}/teacher")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> Move(int id, MoveQuizDto dto)
        {
            /* Tracked, so the change can be saved with UpdateAsync */
            var quiz = await quizRepository.GetByIdAsync(id);

            if (quiz == null)
            {
                logger.LogWarning("Move sent for quiz ID {QuizId}, but the quiz was not found.", id);
                return NotFound();
            }

            var teachers = await userRepository.GetTeachersAsync();

            if (teachers == null)
            {
                return Problem("The teachers could not be loaded. Please try again.", statusCode: StatusCodes.Status500InternalServerError);
            }

            /* Only a real teacher can own the quiz, whatever the request says */
            var teacher = teachers.FirstOrDefault(t => t.Id == dto.TeacherId);

            if (teacher == null)
            {
                ModelState.AddModelError(nameof(MoveQuizDto.TeacherId), "Choose a teacher.");
                return ValidationProblem(ModelState);
            }

            quiz.CreatedByUserId = teacher.Id;

            if (!await quizRepository.UpdateAsync(quiz))
            {
                /* The repository has already logged the database error */
                return Problem("The quiz could not be moved. Please try again.", statusCode: StatusCodes.Status500InternalServerError);
            }

            logger.LogInformation(
                "Administrator {AdminName} moved quiz ID {QuizId} to teacher {TeacherEmail}.",
                User.FindFirstValue(ClaimTypes.Email),
                quiz.Id,
                teacher.Email);

            return NoContent();
        }

        // POST: /api/quizzes/without-teacher/move - gives every quiz without a teacher to one teacher
        [HttpPost("without-teacher/move")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<ActionResult<MoveAllResultDto>> MoveAll(MoveQuizDto dto)
        {
            var teachers = await userRepository.GetTeachersAsync();

            if (teachers == null)
            {
                return Problem("The teachers could not be loaded. Please try again.", statusCode: StatusCodes.Status500InternalServerError);
            }

            var teacher = teachers.FirstOrDefault(t => t.Id == dto.TeacherId);

            if (teacher == null)
            {
                logger.LogWarning("Move all quizzes without a teacher failed: user ID {TeacherId} is not a teacher.", dto.TeacherId);
                ModelState.AddModelError(nameof(MoveQuizDto.TeacherId), "Choose a teacher to move the quizzes to.");
                return ValidationProblem(ModelState);
            }

            var moved = await quizRepository.MoveQuizzesWithoutTeacherAsync(teacher.Id);

            if (moved == null)
            {
                return Problem("The quizzes could not be moved. Please try again.", statusCode: StatusCodes.Status500InternalServerError);
            }

            logger.LogInformation(
                "Administrator {AdminName} moved {Count} quizzes without a teacher to {TeacherEmail}.",
                User.FindFirstValue(ClaimTypes.Email),
                moved,
                teacher.Email);

            return new MoveAllResultDto { Moved = moved.Value, TeacherEmail = teacher.Email };
        }
    }
}