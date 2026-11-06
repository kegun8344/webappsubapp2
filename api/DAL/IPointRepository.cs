using api.Models;

namespace api.DAL
{
    /*
     * Database access for points and quiz attempts, used by PointService, BadgeService,
     * the dashboard, the start page and the leaderboard.
     * Read methods return null if the database call failed (the error is logged),
     * and AddAttemptAsync returns false if the attempt could not be saved.
     */
    public interface IPointRepository
    {
        /* The sum of all points a user has */
        Task<int?> GetTotalPointsAsync(string userId);

        /* The days a user got streak points */
        Task<List<DateOnly>?> GetStreakDaysAsync(string userId);

        /* The best score on each quiz the user has taken */
        Task<List<int>?> GetBestScoresAsync(string userId);

        /* The user's latest attempts, newest first */
        Task<List<AttemptSummary>?> GetRecentAttemptsAsync(string userId, int count);

        /* The user's attempts on point quizzes between startUtc and endUtc, quiz and score, for the daily goal stars */
        Task<List<(int QuizId, int Score)>?> GetAttemptsTodayAsync(string userId, DateTime startUtc, DateTime endUtc);

        /* The number of students and the rank of a student with this many points */
        Task<StudentRank?> GetStudentRankAsync(int totalPoints);

        /* All attempts of a user, for the badges */
        Task<List<AttemptRow>?> GetAttemptRowsAsync(string userId);

        /* The number of days a user reached the daily goal */
        Task<int?> CountDailyGoalDaysAsync(string userId);

        /* What the point rules need to know before an attempt on this quiz is saved */
        Task<AttemptContext?> GetAttemptContextAsync(string userId, int quizId, DateOnly today);

        /*
         * Saves an attempt together with the points it gave, in one SaveChanges,
         * so it is all or nothing. Returns false if it could not be saved.
         */
        Task<bool> AddAttemptAsync(QuizAttempt attempt, IEnumerable<PointAward> awards);

        /* The attempts, streak days and daily goals of every student, by user ID */
        Task<Dictionary<string, StudentActivity>?> GetStudentActivityAsync();

        /*
         * All students with their points, highest first.
         * With a date, PeriodPoints only counts points awarded on or after that date.
         */
        Task<List<StudentPoints>?> GetLeaderboardAsync(DateOnly? since);
    }
}