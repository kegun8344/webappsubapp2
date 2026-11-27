namespace api.DTOs
{
    /* How students answered each question of one quiz (Quiz/Analysis in Subapp 1) */
    public class QuizAnalysisDto
    {
        public int QuizId { get; set; }

        public string Title { get; set; } = string.Empty;

        /* The rules, so the page can explain the flags with the server's numbers */
        public int MinAnswersForFlag { get; set; }

        public int HardBelowPercent { get; set; }

        public int EasyAbovePercent { get; set; }

        public List<QuestionAnalysisDto> Questions { get; set; } = new();
    }

    public class QuestionAnalysisDto
    {
        /* 1, 2, 3 ... in the same order as on the edit page */
        public int Number { get; set; }

        public string Text { get; set; } = string.Empty;

        public int Answers { get; set; }

        public int Correct { get; set; }

        /* null when no student has answered */
        public int? PercentCorrect { get; set; }

        /* "Hard", "Too easy" or null (QuestionAnalysisRules) */
        public string? Flag { get; set; }

        public string? MostChosenWrongAnswer { get; set; }

        public int MostChosenWrongCount { get; set; }
    }
}