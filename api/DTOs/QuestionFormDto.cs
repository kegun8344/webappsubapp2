using System.ComponentModel.DataAnnotations;

namespace api.DTOs
{
    /*
     * The body when adding or changing a question:
     * the question text, the answer texts in order, and which of them is correct.
     * The rules across the fields (2-10 answers, no duplicates, one correct) are in QuestionRules.
     */
    public class QuestionFormDto
    {
        [Required(ErrorMessage = "Please write a question.")]
        [StringLength(500, MinimumLength = 1, ErrorMessage = "The question must be between 1 and 500 characters.")]
        public string Text { get; set; } = string.Empty;

        [Required]
        public List<string?> Answers { get; set; } = new();

        /* Position (starting at 0) of the correct answer in Answers */
        [Required(ErrorMessage = "Choose which answer is correct.")]
        public int? CorrectAnswerIndex { get; set; }
    }
}