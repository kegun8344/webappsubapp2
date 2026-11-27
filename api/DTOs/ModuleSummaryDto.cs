namespace api.DTOs
{
    /*
     * The "learning journey" at the top of the module page: the student's progress over all modules.
     * Worked out on the server from the same overview as the module cards, so the numbers always match.
     */
    public class ModuleSummaryDto
    {
        /* The name in the greeting, e.g. "sofie" */
        public string DisplayName { get; set; } = string.Empty;

        public int TotalOfficialQuizzes { get; set; }

        public int TotalCompletedQuizzes { get; set; }

        public int ProgressPercent { get; set; }

        public int ModulesWithQuizzes { get; set; }

        public int ModulesCompleted { get; set; }

        /* Average best score over all quizzes taken, null if none */
        public int? AverageScore { get; set; }

        /* A short encouraging line (ModuleRules.JourneyMessage) */
        public string Message { get; set; } = string.Empty;

        /* Where to continue: a module already started first, then the one furthest along. Null if nothing is left. */
        public int? ContinueQuizId { get; set; }

        public string? ContinueQuizTitle { get; set; }

        public string? ContinueModuleName { get; set; }
    }
}