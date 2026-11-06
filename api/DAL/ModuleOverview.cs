using api.Models;

namespace api.DAL
{
    /*
     * One module with the numbers shown on its card.
     * Only quizzes the user may see are counted, so private quizzes of others stay hidden.
     *
     * AverageBestScore: average of the user's best score on each official quiz they have taken (null if none)
     * NextQuiz...: the easiest official quiz with questions that the user has not taken yet (null if none)
     */
    public record ModuleOverview(
        Module Module,
        int VisibleQuizCount,
        int OfficialQuizCount,
        int CompletedOfficialQuizCount,
        int? AverageBestScore = null,
        int? NextQuizId = null,
        string? NextQuizTitle = null);
}