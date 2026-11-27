using System.ComponentModel.DataAnnotations;

namespace api.DTOs
{
    /* The body of POST /api/review: the answers to a review quiz */
    public class SubmitReviewDto
    {
        /* The quiz the review was for, or null for mistakes from all quizzes */
        public int? QuizId { get; set; }

        [Required]
        [MaxLength(200)]
        public List<SubmittedAnswerDto> Answers { get; set; } = new();
    }

    /* The result of a review (Review/Result in Subapp 1). A review never gives points. */
    public class ReviewResultDto
    {
        public int? QuizId { get; set; }

        public int CorrectAnswers { get; set; }

        public int TotalQuestions { get; set; }

        /* Mistakes still left to review (in this quiz, or in all quizzes) */
        public int Remaining { get; set; }

        /* False if the answers could not be saved, so the questions stay in the review list */
        public bool ResultSaved { get; set; } = true;
    }

    /* The answer after "Clear mistakes" */
    public class ClearMistakesResultDto
    {
        public int Cleared { get; set; }
    }
}