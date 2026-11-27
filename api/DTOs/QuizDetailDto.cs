using api.Models;
using api.Services;

namespace api.DTOs
{
    /* One quiz with its questions, for the edit page at GET /api/quizzes/{id} */
    public class QuizDetailDto
    {
        public int Id { get; set; }

        public string Title { get; set; } = string.Empty;

        public string? Description { get; set; }

        public string Difficulty { get; set; } = string.Empty;

        public int ModuleId { get; set; }

        public bool IsPublic { get; set; }

        public bool AwardsPoints { get; set; }

        public bool CanManage { get; set; }

        public List<TagDto> Tags { get; set; } = new();

        public List<QuestionDto> Questions { get; set; } = new();

        public static QuizDetailDto FromQuiz(Quiz quiz, string? userId, bool isAdmin)
        {
            var canManage = QuizRules.CanManage(quiz, userId, isAdmin);

            return new QuizDetailDto
            {
                Id = quiz.Id,
                Title = quiz.Title,
                Description = quiz.Description,
                Difficulty = quiz.Difficulty,
                ModuleId = quiz.ModuleId,
                IsPublic = quiz.IsPublic,
                AwardsPoints = quiz.AwardsPoints,
                CanManage = canManage,
                Tags = QuizListItemDto.VisibleTags(quiz, userId, isAdmin),
                Questions = quiz.Questions
                    .OrderBy(question => question.Id)
                    .Select(question => QuestionDto.FromQuestion(question, canManage))
                    .ToList()
            };
        }
    }

    /* A question with its answer options */
    public class QuestionDto
    {
        public int Id { get; set; }

        public string Text { get; set; } = string.Empty;

        public string? Context { get; set; }

        public List<AnswerOptionDto> Options { get; set; } = new();

        /* The correct answers are only sent to people who may edit the quiz */
        public static QuestionDto FromQuestion(Question question, bool includeCorrectAnswers)
        {
            return new QuestionDto
            {
                Id = question.Id,
                Text = question.Text,
                Context = question.Context,
                Options = question.AnswerOptions
                    .OrderBy(option => option.Id)
                    .Select(option => new AnswerOptionDto
                    {
                        Id = option.Id,
                        Text = option.Text,
                        IsCorrect = includeCorrectAnswers ? option.IsCorrect : null
                    })
                    .ToList()
            };
        }
    }

    public class AnswerOptionDto
    {
        public int Id { get; set; }

        public string Text { get; set; } = string.Empty;

        /* Null when the user may not see which answer is correct */
        public bool? IsCorrect { get; set; }
    }
}