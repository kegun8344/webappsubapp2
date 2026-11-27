namespace api.DAL
{
    /* One of the user's own tags and how many quizzes it is on (the My tags page) */
    public record TagUsage(int Id, string Name, string Color, int QuizCount);
}