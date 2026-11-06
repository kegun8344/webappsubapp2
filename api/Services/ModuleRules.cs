namespace api.Services
{
    /*
     * Rules for the personal parts of the module page, without any database.
     * "Average score" is the average of the best score on each official quiz the student has taken.
     */
    public static class ModuleRules
    {
        /* Average best score needed for 1, 2 and 3 mastery stars */
        public const int OneStarScore = 60;
        public const int TwoStarScore = 80;
        public const int ThreeStarScore = 95;

        /* 0-3 stars from the average best score, 0 if the student has not taken any quizzes */
        public static int MasteryStars(int? averageScore)
        {
            if (averageScore is not int score)
            {
                return 0;
            }

            if (score < 0 || score > 100)
            {
                throw new ArgumentOutOfRangeException(nameof(averageScore), "A score must be between 0 and 100.");
            }

            return score >= ThreeStarScore ? 3
                : score >= TwoStarScore ? 2
                : score >= OneStarScore ? 1
                : 0;
        }

        /* A short encouraging line for the progress ring on the module page */
        public static string JourneyMessage(int progressPercent)
        {
            return progressPercent switch
            {
                >= 100 => "You have completed everything. Impressive!",
                >= 75 => "Almost there. Just a few quizzes left.",
                >= 40 => "Great pace. Keep the streak going.",
                > 0 => "Good start. Every quiz counts.",
                _ => "Your journey starts here. Take your first quiz!"
            };
        }
    }
}