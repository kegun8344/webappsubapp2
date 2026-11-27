namespace api.DTOs
{
    /* The body of PUT /api/quizzes/{id}/visibility: publish (true) or unpublish (false) */
    public class QuizVisibilityDto
    {
        public bool IsPublic { get; set; }
    }
}