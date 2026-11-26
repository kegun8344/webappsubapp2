namespace api.Services
{
    /* One problem found in a question form: which field, and what is wrong */
    public record QuestionError(string Field, string Message);

    /*
     * The rules for a question and its answer options, without any database.
     * Used by QuestionsController, and easy to unit test on their own.
     */
    public static class QuestionRules
    {
        public const int MinAnswers = 2;
        public const int MaxAnswers = 10;
        public const int MaxAnswerLength = 300;

        /*
         * A question must have 2-10 answers, none of them empty or longer than 300 characters,
         * no two answers with the same text (that would make the quiz unfair),
         * and exactly one correct answer that points to one of the answers.
         * Returns an empty list when everything is fine.
         */
        public static List<QuestionError> Validate(IReadOnlyList<string?> answers, int? correctAnswerIndex)
        {
            var errors = new List<QuestionError>();

            if (answers.Count < MinAnswers || answers.Count > MaxAnswers)
            {
                errors.Add(new QuestionError("Answers", $"A question must have between {MinAnswers} and {MaxAnswers} answers."));
            }

            var trimmed = answers.Select(answer => (answer ?? string.Empty).Trim()).ToList();

            if (trimmed.Any(answer => answer.Length == 0))
            {
                errors.Add(new QuestionError("Answers", "Write an answer in every field, or remove the empty ones."));
            }

            if (trimmed.Any(answer => answer.Length > MaxAnswerLength))
            {
                errors.Add(new QuestionError("Answers", $"An answer cannot be longer than {MaxAnswerLength} characters."));
            }

            /* "Controller" and "controller " count as the same answer */
            var hasDuplicates = trimmed
                .Where(answer => answer.Length > 0)
                .GroupBy(answer => answer.ToLowerInvariant())
                .Any(group => group.Count() > 1);

            if (hasDuplicates)
            {
                errors.Add(new QuestionError("Answers", "Two answers cannot have the same text."));
            }

            if (correctAnswerIndex is not int index || index < 0 || index >= answers.Count)
            {
                errors.Add(new QuestionError("CorrectAnswerIndex", "Choose which answer is correct."));
            }

            return errors;
        }
    }
}