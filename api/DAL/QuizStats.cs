namespace api.DAL
{
    /*
     * How students do on one quiz, for the teacher's Home page.
     * Only attempts by students count, not teachers or admins trying the quiz.
     */
    public record QuizStats(
        int QuizId,
        string Title,
        string ModuleName,
        int QuestionCount,
        int Attempts,
        double? AverageScore);

    /* Numbers for the whole platform, for the admin Home page. Attempts only count students. */
    public record PlatformCounts(
        int Students,
        int Teachers,
        int OfficialQuizzes,
        int PracticeQuizzes,
        int AttemptsThisWeek);

    /* How students answered one question, for the teacher's question analysis. Only students' answers count. */
    public record QuestionAnalysis(
        int QuestionId,
        string Text,
        int Answers,
        int Correct,
        string? MostChosenWrongAnswer,
        int MostChosenWrongCount);

    /* One attempt in "Recent activity" */
    public record StudentAttempt(
        string UserName,
        int QuizId,
        string QuizTitle,
        int Score,
        DateTime CompletedAt);
}