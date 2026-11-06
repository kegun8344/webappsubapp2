namespace api.DAL
{
    /* Which part of the visible quizzes to list */
    public enum QuizScope
    {
        All,
        Public,
        Mine
    }

    /*
     * Everything the quiz list can be filtered on. All filters are optional and
     * combine with AND. The controllers and the repository keep the same simple signature.
     * SearchText: part of the title or description, not case-sensitive
     * Difficulty: Easy, Medium or Hard
     * TagId:      only quizzes that have this tag
     */
    public record QuizQuery(
        string UserId,
        bool IsAdmin,
        QuizScope Scope = QuizScope.All,
        int? ModuleId = null,
        string? SearchText = null,
        string? Difficulty = null,
        int? TagId = null);
}