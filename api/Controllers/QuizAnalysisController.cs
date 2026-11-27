using System.Security.Claims;
using api.DAL;
using api.DTOs;
using api.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace api.Controllers
{
    /*
     * The question analysis for teachers: how students answered each question of a quiz.
     * Same rule as editing: only the quiz owner or an administrator (QuizRules.CanManage).
     * Same as Quiz/Analysis in Subapp 1.
     */
    [ApiController]
    [Route("api/quizzes")]
    [Authorize(Roles = "Teacher,Admin")]
    public class QuizAnalysisController : ControllerBase
    {
        private readonly IQuizRepository quizRepository;
        private readonly ILogger<QuizAnalysisController> logger;

        public QuizAnalysisController(IQuizRepository quizRepo, ILogger<QuizAnalysisController> analysisLogger)
        {
            quizRepository = quizRepo;
            logger = analysisLogger;
        }

        // GET: /api/quizzes/5/analysis
        [HttpGet("{id:int}/analysis")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<ActionResult<QuizAnalysisDto>> Get(int id)
        {
            var quiz = await quizRepository.GetByIdAsync(id);

            if (quiz == null)
            {
                logger.LogWarning("Analysis requested for quiz ID {QuizId}, but the quiz was not found.", id);
                return NotFound();
            }

            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            if (!QuizRules.CanManage(quiz, userId, User.IsInRole("Admin")))
            {
                logger.LogWarning("User {UserId} attempted to see the analysis of quiz ID {QuizId} without permission.", userId, id);
                return StatusCode(StatusCodes.Status403Forbidden);
            }

            var analysis = await quizRepository.GetQuestionAnalysisAsync(id);

            if (analysis == null)
            {
                /* The repository has already logged the database error */
                return Problem("The analysis could not be loaded. Please try again.", statusCode: StatusCodes.Status500InternalServerError);
            }

            return new QuizAnalysisDto
            {
                QuizId = quiz.Id,
                Title = quiz.Title,
                MinAnswersForFlag = QuestionAnalysisRules.MinAnswersForFlag,
                HardBelowPercent = QuestionAnalysisRules.HardBelowPercent,
                EasyAbovePercent = QuestionAnalysisRules.EasyAbovePercent,
                Questions = analysis
                    .Select((question, index) => new QuestionAnalysisDto
                    {
                        Number = index + 1,
                        Text = question.Text,
                        Answers = question.Answers,
                        Correct = question.Correct,
                        PercentCorrect = QuestionAnalysisRules.PercentCorrect(question.Answers, question.Correct),
                        Flag = QuestionAnalysisRules.Flag(question.Answers, question.Correct),
                        MostChosenWrongAnswer = question.MostChosenWrongAnswer,
                        MostChosenWrongCount = question.MostChosenWrongCount
                    })
                    .ToList()
            };
        }
    }
}