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
        int TotalPoints,
        bool IsLocked);

    /* What is lost when a user is deleted, shown on the confirmation page */
    public record UserDeletionCounts(
        int PrivateQuizzes,
        int PublicQuizzes,
        int Attempts,
        int Points);

    /* An account that cannot log in, shown under "Locked accounts" on the admin Home page */
    public record LockedUser(
        string Id,
        string Email,
        DateTimeOffset LockedUntil);

    /* A teacher in the "Move to" dropdowns */
    public record TeacherOption(
        string Id,
        string Email);

    /* A student or teacher under "New users" on the admin Home page */
    public record NewUser(
        string Id,
        string Email,
        bool IsTeacher,
        DateTime CreatedAt);
}