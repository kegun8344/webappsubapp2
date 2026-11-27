using System.ComponentModel.DataAnnotations;
using api.Models;
using api.Services;

namespace api.DTOs
{
    /* A quiz as it is shown while playing it. There is no IsCorrect anywhere in here. */
    public class QuizPlayDto
    {
        public int QuizId { get; set; }

        public string Title { get; set; } = string.Empty;

        public List<PlayQuestionDto> Questions { get; set; } = new();

        public static QuizPlayDto FromQuiz(Quiz quiz)
        {
            return new QuizPlayDto
            {
                QuizId = quiz.Id,
                Title = quiz.Title,
                Questions = quiz.Questions
                    .OrderBy(question => question.Id)
                    .Select(question => new PlayQuestionDto
                    {
                        QuestionId = question.Id,
                        Text = question.Text,
                        Context = question.Context,
                        Options = question.AnswerOptions
                            .OrderBy(option => option.Id)
                            .Select(option => new PlayOptionDto { Id = option.Id, Text = option.Text })
                            .ToList()
                    })
                    .ToList()
            };
        }
    }

    public class PlayQuestionDto
    {
        public int QuestionId { get; set; }

        public string Text { get; set; } = string.Empty;

        /* Optional scenario or code example shown above the question */
        public string? Context { get; set; }

        public List<PlayOptionDto> Options { get; set; } = new();
    }

    public class PlayOptionDto
    {
        public int Id { get; set; }

        public string Text { get; set; } = string.Empty;
    }

    /* The body of POST /api/quizzes/{quizId}/attempts */
    public class SubmitAnswersDto
    {
        /* A quiz never has more questions than this, so a larger list can only be a bad request */
        [Required]
        [MaxLength(200)]
        public List<SubmittedAnswerDto> Answers { get; set; } = new();
    }

    public class SubmittedAnswerDto
    {
        public int QuestionId { get; set; }

        /* Null if the student skipped the question */
        public int? SelectedAnswerId { get; set; }
    }

    /* What the student sees after submitting: the score, the points and why, and the level */
    public class QuizResultDto
    {
        public int QuizId { get; set; }

        public string QuizTitle { get; set; } = string.Empty;

        public int CorrectAnswers { get; set; }

        public int TotalQuestions { get; set; }

        /* 0-100, rounded */
        public int ScorePercentage { get; set; }

        /* False for practice quizzes made by students */
        public bool AwardsPoints { get; set; }

        /* False if the attempt could not be saved. The score is still shown. */
        public bool ResultSaved { get; set; } = true;

        public List<AwardDto> Awards { get; set; } = new();

        public int TotalPoints { get; set; }

        /* Null for a first attempt */
        public int? PreviousBest { get; set; }

        public int BestScore { get; set; }

        public int StreakDay { get; set; }

        public int DailyGoalStarsEarned { get; set; }

        public int DailyGoalStarsToday { get; set; }

        public int Level { get; set; }

        public int PointsIntoLevel { get; set; }

        public int PointsForLevel { get; set; }

        public bool LeveledUp { get; set; }

        public List<BadgeDto> NewBadges { get; set; } = new();

        /* Open mistakes in this quiz after the attempt. Above 0 shows "Review mistakes" on the result. */
        public int ReviewCount { get; set; }
    }

    /* One line of points, e.g. Reason "Streak", Points 30 */
    public class AwardDto
    {
        public string Reason { get; set; } = string.Empty;

        public int Points { get; set; }
    }

    /* A badge that was unlocked or upgraded by the attempt */
    public class BadgeDto
    {
        public string Type { get; set; } = string.Empty;

        public string Name { get; set; } = string.Empty;

        public string Description { get; set; } = string.Empty;

        public string Tier { get; set; } = string.Empty;

        public static BadgeDto FromProgress(BadgeProgress badge)
        {
            return new BadgeDto
            {
                Type = badge.Type.ToString(),
                Name = badge.Name,
                Description = badge.Description,
                Tier = badge.Tier.ToString()
            };
        }
    }
}