namespace api.DAL
{
    /* A quiz suggested on the start page. BestScore is null if the user has not taken it. */
    public record QuizSuggestion(
        int QuizId,
        string Title,
        string ModuleName,
        string Difficulty,
        int QuestionCount,
        int? BestScore);
}