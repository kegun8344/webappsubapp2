namespace api.DTOs
{
    /* One row on the administrator's Users page (AdminUsers/Index in Subapp 1) */
    public class AdminUserDto
    {
        public string Id { get; set; } = string.Empty;

        public string Email { get; set; } = string.Empty;

        public List<string> Roles { get; set; } = new();

        public int QuizzesCreated { get; set; }

        public int QuizzesPublished { get; set; }

        public int QuizzesCompleted { get; set; }

        public int TotalPoints { get; set; }

        public int Level { get; set; }

        /* Locked by an administrator, or for a few minutes after too many wrong passwords */
        public bool IsLocked { get; set; }

        /* Worked out with UserDeletionRules, so React only shows Delete when the server allows it */
        public bool CanDelete { get; set; }
    }

    /* What is lost when a user is deleted, shown before the administrator confirms */
    public class UserDeletionPreviewDto
    {
        public string Id { get; set; } = string.Empty;

        public string Email { get; set; } = string.Empty;

        public List<string> Roles { get; set; } = new();

        public int PrivateQuizzes { get; set; }

        public int PublicQuizzes { get; set; }

        public int Attempts { get; set; }

        public int Points { get; set; }
    }
}