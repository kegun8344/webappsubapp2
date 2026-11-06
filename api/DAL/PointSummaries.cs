namespace api.DAL
{
    /* One finished quiz attempt, as shown in "Recent quizzes" on the dashboard */
    public record AttemptSummary(
        int QuizId,
        string QuizTitle,
        int Score,
        DateTime CompletedAt,
        bool AwardsPoints);

    /* A student with the points they have in the chosen period and in total */
    public record StudentPoints(
        string UserId,
        string? UserName,
        int PeriodPoints,
        int AllTimePoints);

    /* How many students there are and which place one student has */
    public record StudentRank(
        int StudentCount,
        int Rank);

    /*
     * What the point rules need to know about a user before a new attempt is saved:
     * the best earlier score on the quiz, total points, today's attempts on point quizzes and what was already given today.
     */
    public record AttemptContext(
        int? PreviousBest,
        int TotalPoints,
        bool DailyGoalGiven,
        List<(int QuizId, int Score)> AttemptsToday,
        bool StreakGiven,
        List<DateOnly> EarlierStreakDays);

    /* One attempt, the numbers the badges are based on */
    public record AttemptRow(
        int QuizId,
        int Score,
        DateTime CompletedAt,
        bool AwardsPoints);

    /* What the leaderboard needs to know about one student to work out their badges and streak */
    public record StudentActivity(
        List<AttemptRow> Attempts,
        List<DateOnly> StreakDays,
        int DailyGoalDays);
}