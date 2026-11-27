using api.Models;

namespace api.Services
{
    /*
     * What is missing on a quiz, shown under "Needs attention" on the teacher and admin Home page.
     * Pure logic without database access, so it is easy to unit test. Same rules as Subapp 1.
     */
    public static class QuizAttentionRules
    {
        public const string NoTeacher = "No teacher";
        public const string NoQuestions = "No questions";
        public const string NoDescription = "No description";

        /*
         * forAdmin: teachers look after their own quizzes, so administrators only see
         * quizzes without a teacher (the teacher's account was deleted).
         */
        public static List<string> Problems(Quiz quiz, bool forAdmin)
        {
            var problems = new List<string>();

            if (forAdmin && quiz.CreatedByUserId != null)
            {
                return problems;
            }

            /* The teacher's account was deleted, so nobody can edit the quiz except an administrator */
            if (quiz.CreatedByUserId == null)
            {
                problems.Add(NoTeacher);
            }

            /* A quiz without questions cannot be started */
            if (quiz.Questions.Count == 0)
            {
                problems.Add(NoQuestions);
            }

            if (string.IsNullOrWhiteSpace(quiz.Description))
            {
                problems.Add(NoDescription);
            }

            return problems;
        }
    }
}