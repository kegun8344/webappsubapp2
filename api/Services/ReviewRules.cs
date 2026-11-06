namespace api.Services
{
    /* One stored answer, reduced to what the review rule needs */
    public record AnswerRecord(int Id, int QuestionId, bool IsCorrect, DateTime AnsweredAt);

    /*
     * The rule for which questions the user should review, without any database.
     * Kept separate so it is easy to unit test.
     */
    public static class ReviewRules
    {
        /* How many questions one review quiz contains */
        public const int MaxQuestions = 10;

        /*
         * A question needs review if the user's LATEST answer to it was wrong.
         * Answering it correctly (in a quiz or in a review) removes it from the list.
         * The questions that have waited the longest come first.
         */
        public static List<int> SelectMistakes(IEnumerable<AnswerRecord> answers, int limit)
        {
            if (limit < 1)
            {
                throw new ArgumentOutOfRangeException(nameof(limit), "The limit must be 1 or higher.");
            }

            return answers
                .GroupBy(answer => answer.QuestionId)
                .Select(group => group
                    .OrderByDescending(answer => answer.AnsweredAt)
                    .ThenByDescending(answer => answer.Id)
                    .First())
                .Where(latest => !latest.IsCorrect)
                .OrderBy(latest => latest.AnsweredAt)
                .ThenBy(latest => latest.Id)
                .Take(limit)
                .Select(latest => latest.QuestionId)
                .ToList();
        }
    }
}