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
     * Taking a quiz: GET gives the questions without the correct answers,
     * and POST grades the answers on the server and gives points.
     * The answers are never graded in the browser, so the score cannot be faked.
     */
    [ApiController]
    [Route("api/quizzes/{quizId:int}")]
    [Authorize]
    public class PlayController : ControllerBase
    {
        private readonly IQuizRepository quizRepository;
        private readonly IReviewRepository reviewRepository;
        private readonly IPointService pointService;
        private readonly IBadgeService badgeService;
        private readonly ILogger<PlayController> logger;

        public PlayController(
            IQuizRepository quizRepo,
            IReviewRepository reviewRepo,
            IPointService points,
            IBadgeService badges,
            ILogger<PlayController> playLogger)
        {
            quizRepository = quizRepo;
            reviewRepository = reviewRepo;
            pointService = points;
            badgeService = badges;
            logger = playLogger;
        }

        // GET: /api/quizzes/5/play
        [HttpGet("play")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<ActionResult<QuizPlayDto>> Take(int quizId)
        {
            var quiz = await quizRepository.GetForPlayAsync(quizId);

            if (quiz == null)
            {
                logger.LogWarning("Quiz ID {QuizId} was requested for playing, but it was not found.", quizId);
                return NotFound();
            }

            /* A private quiz of someone else looks the same as a quiz that does not exist */
            if (!QuizRules.CanView(quiz, CurrentUserId, User.IsInRole("Admin")))
            {
                logger.LogWarning("User {UserId} tried to play private quiz ID {QuizId} without permission.", CurrentUserId, quizId);
                return NotFound();
            }

            /* A quiz without questions cannot be taken */
            if (quiz.Questions.Count == 0)
            {
                return Problem("This quiz does not contain any questions.", statusCode: StatusCodes.Status400BadRequest);
            }

            return QuizPlayDto.FromQuiz(quiz);
        }

        // POST: /api/quizzes/5/attempts
        [HttpPost("attempts")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<ActionResult<QuizResultDto>> Submit(int quizId, SubmitAnswersDto dto)
        {
            var userId = CurrentUserId;

            if (userId == null)
            {
                return Unauthorized();
            }

            /* The quiz is loaded again from the database. Nothing about the questions is trusted from the request. */
            var quiz = await quizRepository.GetForPlayAsync(quizId);

            if (quiz == null)
            {
                return NotFound();
            }

            /* Visibility is checked on POST too, because a hand-made request could skip the GET */
            if (!QuizRules.CanView(quiz, userId, User.IsInRole("Admin")))
            {
                logger.LogWarning("User {UserId} tried to submit private quiz ID {QuizId} without permission.", userId, quizId);
                return NotFound();
            }

            if (quiz.Questions.Count == 0)
            {
                logger.LogWarning("Quiz ID {QuizId} was submitted but has no questions.", quizId);
                return Problem("This quiz does not contain any questions.", statusCode: StatusCodes.Status400BadRequest);
            }

            var grade = QuizGrader.Grade(
                quiz,
                dto.Answers.Select(answer => new SubmittedAnswer(answer.QuestionId, answer.SelectedAnswerId)));

            var result = new QuizResultDto
            {
                QuizId = quiz.Id,
                QuizTitle = quiz.Title,
                CorrectAnswers = grade.CorrectAnswers,
                TotalQuestions = grade.TotalQuestions,
                ScorePercentage = (int)Math.Round(grade.CorrectAnswers * 100.0 / grade.TotalQuestions),
                AwardsPoints = quiz.AwardsPoints
            };

            /* Badge numbers before the attempt, so new or upgraded badges can be shown afterwards */
            var badgesBefore = await TryGetBadgeStatsAsync(userId);

            /* Saves the attempt and applies the point rules in PointRules */
            var points = await pointService.SaveAttemptAsync(userId, quiz, grade.CorrectAnswers, grade.TotalQuestions);

            if (points == null)
            {
                /* The score is still shown. The service has already logged the reason. */
                logger.LogError("Could not save the attempt for user {UserId} on quiz ID {QuizId}.", userId, quizId);
                result.ResultSaved = false;

                return result;
            }

            result.Awards = points.Awards
                .Select(award => new AwardDto { Reason = award.Reason.ToString(), Points = award.Points })
                .ToList();
            result.TotalPoints = points.TotalPoints;
            result.PreviousBest = points.PreviousBest;
            result.BestScore = points.BestScore;
            result.StreakDay = points.StreakDay;
            result.DailyGoalStarsEarned = points.DailyGoalStarsEarned;
            result.DailyGoalStarsToday = points.DailyGoalStarsAfter;
            result.LeveledUp = points.LeveledUp;

            (result.Level, result.PointsIntoLevel, result.PointsForLevel) = LevelCalculator.GetLevel(points.TotalPointsAfter);

            await TrySaveAnswersAsync(userId, grade.Answers);

            /* Open mistakes in this quiz, so the result can offer "Review mistakes" (same as Subapp 1) */
            result.ReviewCount = (await reviewRepository.GetMistakeQuestionIdsAsync(
                userId, User.IsInRole("Admin"), int.MaxValue, quizId)).Count;

            var badgesAfter = await TryGetBadgeStatsAsync(userId);

            if (badgesBefore != null && badgesAfter != null)
            {
                result.NewBadges = BadgeRules.NewBadges(badgesBefore, badgesAfter)
                    .Select(BadgeDto.FromProgress)
                    .ToList();
            }

            logger.LogInformation(
                "User {UserId} took quiz ID {QuizId}: {Correct} of {Total} correct, {Points} points.",
                userId,
                quizId,
                grade.CorrectAnswers,
                grade.TotalQuestions,
                points.TotalPoints);

            return result;
        }

        /*
         * The stored answers are a bonus (used by a review feature). If they cannot be saved,
         * the repository logs the error and the result is still shown.
         */
        private async Task TrySaveAnswersAsync(string userId, List<UserAnswer> answers)
        {
            var now = DateTime.UtcNow;

            foreach (var answer in answers)
            {
                answer.UserId = userId;
                answer.AnsweredAt = now;
            }

            await reviewRepository.AddAnswersAsync(answers);
        }

        /* Badges are a bonus. If they cannot be loaded, the result is shown without new badges. */
        private async Task<BadgeStats?> TryGetBadgeStatsAsync(string userId)
        {
            try
            {
                return await badgeService.GetStatsAsync(userId);
            }
            catch (Exception exception)
            {
                logger.LogWarning(exception, "Could not load badge stats for user {UserId}.", userId);
                return null;
            }
        }

        /* The signed-in user's Identity ID, read from the token */
        private string? CurrentUserId => User.FindFirstValue(ClaimTypes.NameIdentifier);
    }
}