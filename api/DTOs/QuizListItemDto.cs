using api.Models;
using api.Services;

namespace api.DTOs
{
    /* One quiz card in the list at GET /api/quizzes. Never contains the questions or the correct answers. */
    public class QuizListItemDto
    {
        public int Id { get; set; }

        public string Title { get; set; } = string.Empty;

        public string? Description { get; set; }

        public string Difficulty { get; set; } = string.Empty;

        public int ModuleId { get; set; }

        public string ModuleName { get; set; } = string.Empty;

        public bool IsPublic { get; set; }

        /* True for teacher quizzes, which give points */
        public bool AwardsPoints { get; set; }

        public int QuestionCount { get; set; }

        /* True if the signed-in user made the quiz */
        public bool IsMine { get; set; }

        /* The part of the creator's email before @, null if the account has been deleted */
        public string? CreatedByName { get; set; }

        /* True if the user may edit and delete it. The React app uses this to show or hide the buttons. */
        public bool CanManage { get; set; }

        /* The user's best score (0-100), null if the quiz has not been taken */
        public int? BestScore { get; set; }

        public List<TagDto> Tags { get; set; } = new();

        /* Students: how many questions from this quiz are waiting in "Review mistakes" (0 for others) */
        public int MistakeCount { get; set; }

        /* Teachers and administrators: how students do on the quiz. null for students. */
        public int? Attempts { get; set; }

        /* Rounded average score of the students' attempts. null if there are none, or for students. */
        public int? AverageScore { get; set; }

        public static QuizListItemDto FromQuiz(Quiz quiz, string? userId, bool isAdmin, int? bestScore)
        {
            return new QuizListItemDto
            {
                Id = quiz.Id,
                Title = quiz.Title,
                Description = quiz.Description,
                Difficulty = quiz.Difficulty,
                ModuleId = quiz.ModuleId,
                ModuleName = quiz.Module?.Name ?? string.Empty,
                IsPublic = quiz.IsPublic,
                AwardsPoints = quiz.AwardsPoints,
                QuestionCount = quiz.Questions.Count,
                IsMine = userId != null && quiz.CreatedByUserId == userId,
                CreatedByName = quiz.CreatedBy == null ? null : DisplayNames.FromEmail(quiz.CreatedBy.Email),
                CanManage = QuizRules.CanManage(quiz, userId, isAdmin),
                BestScore = bestScore,
                Tags = VisibleTags(quiz, userId, isAdmin)
            };
        }

        /* Other users' personal tags are never sent, so they stay private */
        public static List<TagDto> VisibleTags(Quiz quiz, string? userId, bool isAdmin)
        {
            return quiz.Tags
                .Where(tag => isAdmin || tag.IsVisibleTo(userId))
                .OrderBy(tag => !tag.IsShared)
                .ThenBy(tag => tag.Name)
                .Select(TagDto.FromTag)
                .ToList();
        }
    }
}