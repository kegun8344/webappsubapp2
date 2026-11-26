using api.Models;

namespace api.Services
{
    /* One answer from the client: which question, and which option the student chose (null = skipped) */
    public record SubmittedAnswer(int QuestionId, int? SelectedAnswerId);

    /* The result of grading: the numbers, and one stored answer per question for the review feature */
    public record GradeResult(int CorrectAnswers, int TotalQuestions, List<UserAnswer> Answers);

    /*
     * Grades a quiz attempt against the trusted data from the database.
     * Nothing from the client is believed: unknown questions are ignored, a question counts
     * only once, and an option only counts if it belongs to that question.
     */
    public static class QuizGrader
    {
        public static GradeResult Grade(Quiz quiz, IEnumerable<SubmittedAnswer> submitted)
        {
            var correctAnswers = 0;
            var storedAnswers = new List<UserAnswer>();
            var answeredQuestionIds = new HashSet<int>();

            foreach (var answer in submitted)
            {
                var question = quiz.Questions.FirstOrDefault(q => q.Id == answer.QuestionId);

                /* An unknown question ID can come from a tampered request */
                if (question == null)
                {
                    continue;
                }

                /* Without this, a request could repeat a correct question and score above 100 % */
                if (!answeredQuestionIds.Add(question.Id))
                {
                    continue;
                }

                var correctOption = question.AnswerOptions.FirstOrDefault(option => option.IsCorrect);

                var isCorrect = correctOption != null
                    && answer.SelectedAnswerId == correctOption.Id;

                if (isCorrect)
                {
                    correctAnswers++;
                }

                /* Only keep the chosen option if it really belongs to this question */
                var chosenOption = question.AnswerOptions.FirstOrDefault(option => option.Id == answer.SelectedAnswerId);

                storedAnswers.Add(new UserAnswer
                {
                    QuestionId = question.Id,
                    SelectedAnswerId = chosenOption?.Id,
                    IsCorrect = isCorrect
                });
            }

            /* Questions that were not submitted at all count as wrong */
            foreach (var unanswered in quiz.Questions.Where(q => !answeredQuestionIds.Contains(q.Id)))
            {
                storedAnswers.Add(new UserAnswer
                {
                    QuestionId = unanswered.Id,
                    SelectedAnswerId = null,
                    IsCorrect = false
                });
            }

            /* The question count always comes from the database, never from the client */
            return new GradeResult(correctAnswers, quiz.Questions.Count, storedAnswers);
        }
    }
}