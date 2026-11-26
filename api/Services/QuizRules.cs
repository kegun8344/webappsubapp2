using api.Models;

namespace api.Services
{
    /*
     * Rules about who may see and change a quiz, and which filters are allowed.
     * They are plain methods without a database, so they are easy to unit test.
     * The same rules as the private checks in the controllers of Subapp 1.
     */
    public static class QuizRules
    {
        /* The difficulty levels a quiz can have, used by the list filter and the form */
        public static readonly string[] DifficultyLevels = { "Easy", "Medium", "Hard" };

        /*
         * Administrators may see every quiz.
         * Other users may see public quizzes and the quizzes they created themselves.
         */
        public static bool CanView(Quiz quiz, string? userId, bool isAdmin)
        {
            return isAdmin
                || quiz.IsPublic
                || (userId != null && quiz.CreatedByUserId == userId);
        }

        /*
         * Administrators may manage every quiz. Other users may only manage their own.
         * If the creator account has been deleted (CreatedByUserId is null),
         * only an administrator can manage the quiz.
         */
        public static bool CanManage(Quiz quiz, string? userId, bool isAdmin)
        {
            return isAdmin
                || (userId != null && quiz.CreatedByUserId == userId);
        }

        /* Teacher and administrator quizzes are public and give points. Student quizzes are private practice. */
        public static bool IsOfficialAuthor(bool isTeacher, bool isAdmin)
        {
            return isTeacher || isAdmin;
        }

        /* Unknown values fall back to "all", so a hand-made request cannot break the list */
        public static string NormalizeView(string? view)
        {
            var value = (view ?? "all").Trim().ToLowerInvariant();

            return value == "public" || value == "mine" ? value : "all";
        }

        /* Search text is trimmed and cut to 100 characters. Empty text means no search. */
        public static string? NormalizeSearch(string? search)
        {
            var text = (search ?? string.Empty).Trim();

            if (text.Length > 100)
            {
                text = text[..100];
            }

            return text.Length == 0 ? null : text;
        }

        /* Returns the difficulty with the correct capital letter, or null if it is not one of the levels */
        public static string? NormalizeDifficulty(string? difficulty)
        {
            return DifficultyLevels.FirstOrDefault(level =>
                string.Equals(level, difficulty, StringComparison.OrdinalIgnoreCase));
        }
    }
}