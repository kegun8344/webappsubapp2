namespace api.DAL
{
    /* One user with the numbers shown in the administrator's user overview */
    public record UserOverview(
        string Id,
        string Email,
        List<string> Roles,
        int QuizzesCreated,
        int QuizzesPublished,
        int QuizzesCompleted,
        int TotalPoints);

    /* What is lost when a user is deleted, shown on the confirmation page */
    public record UserDeletionCounts(
        int PrivateQuizzes,
        int PublicQuizzes,
        int Attempts,
        int Points);
}