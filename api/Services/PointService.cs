using api.DAL;
using api.Models;

namespace api.Services
{
    /*
     * Saves a quiz attempt and gives points for it:
     * - QuizScore: improvement on the user's best result on the quiz, multiplied by the quiz difficulty
     * - FirstCompletion: the first time the user completes the quiz
     * - DailyGoal: 5 stars collected today, up to 2 per quiz (once per day, see PointRules.DailyGoalStars)
     * - Streak: completed a quiz today, more points for several days in a row (once per day)
     * Only quizzes made by teachers/admins (AwardsPoints) give points. The attempt is always saved.
     */
    public class PointService : IPointService
    {
        /* "A day" means a Norwegian day, not a UTC day */
        private static readonly TimeZoneInfo NorwayTime = TimeZoneInfo.FindSystemTimeZoneById("Europe/Oslo");

        private readonly IPointRepository pointRepository;
        private readonly ILogger<PointService> logger;
        private readonly TimeProvider timeProvider;

        /* TimeProvider instead of DateTime.Now, so tests can pretend it is a different day */
        public PointService(IPointRepository pointRepo, ILogger<PointService> pointsLogger, TimeProvider clock)
        {
            pointRepository = pointRepo;
            logger = pointsLogger;
            timeProvider = clock;
        }

        public async Task<PointResult?> SaveAttemptAsync(string userId, Quiz quiz, int correctAnswers, int totalQuestions)
        {
            var score = PointRules.CalculateScore(correctAnswers, totalQuestions);
            var nowUtc = timeProvider.GetUtcNow().UtcDateTime;
            var today = DateOnly.FromDateTime(TimeZoneInfo.ConvertTimeFromUtc(nowUtc, NorwayTime));

            /* What the user has done before: best score, total points, today's bonuses */
            var history = await pointRepository.GetAttemptContextAsync(userId, quiz.Id, today);
            if (history == null)
            {
                /* The repository has already logged the database error */
                return null;
            }

            var previousBest = history.PreviousBest;

            /* Every attempt is saved, also for practice quizzes, so the user has a history */
            var attempt = new QuizAttempt
            {
                UserId = userId,
                QuizId = quiz.Id,
                CorrectAnswers = correctAnswers,
                TotalQuestions = totalQuestions,
                Score = score,
                CompletedAt = nowUtc
            };

            var result = new PointResult
            {
                Score = score,
                PreviousBest = previousBest,
                BestScore = Math.Max(score, previousBest ?? 0),
                AwardsPoints = quiz.AwardsPoints,
                TotalPointsBefore = history.TotalPoints
            };

            var awards = new List<PointAward>();

            if (quiz.AwardsPoints)
            {
                /* Harder quizzes are worth more: the improvement is multiplied by the difficulty */
                var improvement = PointRules.ApplyDifficulty(
                    PointRules.ImprovementPoints(score, previousBest),
                    quiz.Difficulty);
                if (improvement > 0)
                {
                    result.Awards.Add(new AwardLine(PointReason.QuizScore, improvement));
                }

                if (previousBest == null)
                {
                    result.Awards.Add(new AwardLine(PointReason.FirstCompletion, PointRules.FirstCompletionBonus));
                }

                AddDailyGoal(history, quiz.Id, score, result);
                AddStreak(history, today, result);

                foreach (var award in result.Awards)
                {
                    awards.Add(new PointAward
                    {
                        UserId = userId,
                        Points = award.Points,
                        Reason = award.Reason,
                        /* Only quiz points are tied to a quiz, the daily bonuses are not */
                        QuizId = award.Reason is PointReason.QuizScore or PointReason.FirstCompletion ? quiz.Id : null,
                        AwardedOn = today,
                        AwardedAt = nowUtc
                    });
                }
            }

            /* The attempt and all its points are saved together, so it is all or nothing */
            if (!await pointRepository.AddAttemptAsync(attempt, awards))
            {
                return null;
            }

            logger.LogInformation(
                "User {UserId} scored {Score}% on quiz ID {QuizId} and got {Points} points.",
                userId, score, quiz.Id, result.TotalPoints);

            return result;
        }

        /*
        * Counts the daily goal stars before and after this attempt, so the result page can show them,
        * and gives +50 the first time today the user has collected 5 stars (see PointRules.DailyGoalStars).
        */
        private static void AddDailyGoal(AttemptContext history, int quizId, int score, PointResult result)
        {
            /* The current attempt is not saved yet, so add it here */
            var attemptsToday = history.AttemptsToday.Append((quizId, score));

            result.DailyGoalStarsBefore = PointRules.DailyGoalStars(history.AttemptsToday);
            result.DailyGoalStarsAfter = PointRules.DailyGoalStars(attemptsToday);

            if (history.DailyGoalGiven)
            {
                return;
            }

            if (result.DailyGoalStarsAfter >= PointRules.DailyGoalStarsNeeded)
            {
                result.Awards.Add(new AwardLine(PointReason.DailyGoal, PointRules.DailyGoalBonus));
            }
        }

        /* Streak points for the first quiz completed today. More points the more days in a row, max at day 5. */
        private static void AddStreak(AttemptContext history, DateOnly today, PointResult result)
        {
            if (history.StreakGiven)
            {
                return;
            }

            result.StreakDay = PointRules.CountStreakDay(history.EarlierStreakDays, today);
            result.Awards.Add(new AwardLine(PointReason.Streak, PointRules.StreakPoints(result.StreakDay)));
        }
    }
}