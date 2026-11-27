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
     * "Review mistakes": a practice quiz built from the questions the user got wrong earlier.
     * It gives no points and is not an attempt at a quiz, so it never changes scores,
     * leaderboard, streaks or badges. The same rules as ReviewController in Subapp 1.
     */
    [ApiController]
    [Route("api/review")]
    [Authorize]
    public class ReviewController : ControllerBase
    {
        private readonly IReviewRepository reviewRepository;
        private readonly ILogger<ReviewController> logger;

        public ReviewController(IReviewRepository reviewRepo, ILogger<ReviewController> reviewLogger)
        {
            reviewRepository = reviewRepo;
            logger = reviewLogger;
        }

        // GET: /api/review or /api/review?quizId=3
        // The user's oldest open mistakes (from one quiz when quizId is given). No questions means nothing to review.
        [HttpGet]
        [ProducesResponseType(StatusCodes.Status200OK)]
        public async Task<ActionResult<QuizPlayDto>> Get(int? quizId = null)
        {
            var userId = CurrentUserId;

            if (userId == null)
            {
                return Unauthorized();
            }

            var questionIds = await reviewRepository.GetMistakeQuestionIdsAsync(
                userId, User.IsInRole("Admin"), ReviewRules.MaxQuestions, quizId);

            var questions = questionIds.Count == 0
                ? new List<Question>()
                : await reviewRepository.GetQuestionsWithOptionsAsync(questionIds);

            /* Keep the order from the review rule (oldest mistake first). IsCorrect is never sent. */
            var ordered = questionIds
                .Select(id => questions.FirstOrDefault(q => q.Id == id))
                .OfType<Question>()
                .ToList();

            return new QuizPlayDto
            {
                /* 0 means mistakes from all quizzes */
                QuizId = quizId ?? 0,
                Title = quizId.HasValue && ordered.Count > 0
                    ? $"Review: {ordered[0].Quiz.Title}"
                    : "Review your mistakes",
                Questions = ordered
                    .Select(question => new PlayQuestionDto
                    {
                        QuestionId = question.Id,
                        Text = question.Text,
                        Options = question.AnswerOptions
                            .OrderBy(option => option.Id)
                            .Select(option => new PlayOptionDto { Id = option.Id, Text = option.Text })
                            .ToList()
                    })
                    .ToList()
            };
        }

        // POST: /api/review
        // Grades the answers on the server and stores them, so solved questions leave the review list.
        [HttpPost]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<ActionResult<ReviewResultDto>> Submit(SubmitReviewDto dto)
        {
            var userId = CurrentUserId;

            if (userId == null)
            {
                return Unauthorized();
            }

            var isAdmin = User.IsInRole("Admin");

            /* Only questions that really are open mistakes of this user can be answered here */
            var allowedIds = await reviewRepository.GetMistakeQuestionIdsAsync(userId, isAdmin, int.MaxValue, dto.QuizId);

            var submittedIds = dto.Answers
                .Select(answer => answer.QuestionId)
                .Where(allowedIds.Contains)
                .Distinct()
                .ToList();

            if (submittedIds.Count == 0)
            {
                return Problem("None of these questions are waiting for review.", statusCode: StatusCodes.Status400BadRequest);
            }

            var questions = await reviewRepository.GetQuestionsWithOptionsAsync(submittedIds);

            /* The same grading as a normal attempt, but only over the reviewed questions */
            var grade = QuizGrader.Grade(
                new Quiz { Questions = questions },
                dto.Answers.Select(answer => new SubmittedAnswer(answer.QuestionId, answer.SelectedAnswerId)));

            var nowUtc = DateTime.UtcNow;

            foreach (var answer in grade.Answers)
            {
                answer.UserId = userId;
                answer.AnsweredAt = nowUtc;
            }

            var result = new ReviewResultDto
            {
                QuizId = dto.QuizId,
                CorrectAnswers = grade.CorrectAnswers,
                TotalQuestions = grade.TotalQuestions
            };

            if (await reviewRepository.AddAnswersAsync(grade.Answers))
            {
                result.Remaining = (await reviewRepository.GetMistakeQuestionIdsAsync(userId, isAdmin, int.MaxValue, dto.QuizId)).Count;

                logger.LogInformation(
                    "User {UserId} reviewed {Total} questions and got {Correct} right.",
                    userId, result.TotalQuestions, result.CorrectAnswers);
            }
            else
            {
                /* The repository has already logged the database error */
                result.ResultSaved = false;
            }

            return result;
        }

        // POST: /api/review/3/clear
        // Clears the user's current mistakes in one quiz. They come back if the user answers wrong again.
        [HttpPost("{quizId:int}/clear")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<ActionResult<ClearMistakesResultDto>> Clear(int quizId)
        {
            var userId = CurrentUserId;

            if (userId == null)
            {
                return Unauthorized();
            }

            /* Only a quiz the user really has open mistakes in can be cleared, so made-up IDs are refused */
            var mistakes = await reviewRepository.GetMistakeQuestionIdsAsync(userId, User.IsInRole("Admin"), int.MaxValue, quizId);

            if (mistakes.Count == 0)
            {
                return Problem("There are no mistakes to clear in this quiz.", statusCode: StatusCodes.Status400BadRequest);
            }

            if (!await reviewRepository.AddDismissalAsync(userId, quizId))
            {
                /* The repository has already logged the database error */
                return Problem("The mistakes could not be cleared. Please try again.", statusCode: StatusCodes.Status500InternalServerError);
            }

            logger.LogInformation("User {UserId} cleared {Count} mistakes in quiz ID {QuizId}.", userId, mistakes.Count, quizId);

            return new ClearMistakesResultDto { Cleared = mistakes.Count };
        }

        private string? CurrentUserId => User.FindFirstValue(ClaimTypes.NameIdentifier);
    }
}