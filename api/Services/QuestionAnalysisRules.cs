namespace api.Services
{
    /*
     * Flags for the teacher's question analysis. Pure logic without database access, so it is easy to unit test.
     * A question needs a few answers before it is flagged, so one student cannot make it look hard or easy.
     * Same rules as Subapp 1.
     */
    public static class QuestionAnalysisRules
    {
        public const int MinAnswersForFlag = 5;
        public const int HardBelowPercent = 40;
        public const int EasyAbovePercent = 95;

        public const string Hard = "Hard";
        public const string TooEasy = "Too easy";

        /* Percent of the answers that were correct, rounded. Null when nobody has answered. */
        public static int? PercentCorrect(int answers, int correct)
        {
            return answers == 0 ? null : (int)Math.Round(correct * 100.0 / answers);
        }

        /* "Hard", "Too easy" or null */
        public static string? Flag(int answers, int correct)
        {
            if (answers < MinAnswersForFlag)
            {
                return null;
            }

            var percent = PercentCorrect(answers, correct)!.Value;

            if (percent < HardBelowPercent)
            {
                return Hard;
            }

            return percent > EasyAbovePercent ? TooEasy : null;
        }
    }
}