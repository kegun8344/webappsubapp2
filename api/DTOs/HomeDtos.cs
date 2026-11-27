namespace api.DTOs
{
    /*
     * Everything the signed-in Home page needs from GET /api/home.
     * Students get the Student part, teachers and administrators the Staff part. The other one is null.
     */
    public class HomeDto
    {
        /* "Student", "Teacher" or "Admin", the role the page is built for */
        public string Role { get; set; } = string.Empty;

        public StudentHomeDto? Student { get; set; }

        public StaffHomeDto? Staff { get; set; }
    }

    /* Today's streak and daily goal, and quiz suggestions */
    public class StudentHomeDto
    {
        public int StreakDays { get; set; }

        /* True if a quiz already counted for the streak today */
        public bool StreakKeptToday { get; set; }

        /* Kept today: the points given today. Not yet: what today's first quiz will give. */
        public int StreakPointsToday { get; set; }

        public int DailyGoalStars { get; set; }

        /* The numbers from PointRules, so the texts always match the real bonus */
        public int DailyGoalStarsNeeded { get; set; }

        public int DailyGoalBonus { get; set; }

        public bool DailyGoalReached { get; set; }

        /* "Beat your best": quizzes where the student can still win points */
        public List<HomeQuizDto> ImproveQuizzes { get; set; } = new();

        /* "Quizzes to try": quizzes the student has not taken */
        public List<HomeQuizDto> QuizzesToTry { get; set; } = new();
    }

    public class HomeQuizDto
    {
        public int QuizId { get; set; }

        public string Title { get; set; } = string.Empty;

        public string ModuleName { get; set; } = string.Empty;

        public string Difficulty { get; set; } = string.Empty;

        public int QuestionCount { get; set; }

        /* Only used in "Beat your best" */
        public int BestScore { get; set; }

        public int PointsLeft => 100 - BestScore;
    }

    /* The Home page for teachers and administrators */
    public class StaffHomeDto
    {
        /* Quizzes with something missing (QuizAttentionRules) */
        public List<AttentionQuizDto> NeedsAttention { get; set; } = new();

        /* The latest attempts by students */
        public List<ActivityDto> RecentActivity { get; set; } = new();

        /* Teachers: how students do on each of their quizzes, and the totals for the stat cards */
        public List<QuizStatsDto> YourQuizzes { get; set; } = new();

        public int StudentsReached { get; set; }

        public int TotalAttempts { get; set; }

        /* null if no student has taken the quizzes */
        public int? AverageScore { get; set; }

        /* Administrators: numbers for the whole platform. null for teachers. */
        public PlatformCountsDto? Platform { get; set; }

        public List<QuizStatsDto> PopularThisWeek { get; set; } = new();

        public List<NewUserDto> NewUsers { get; set; } = new();

        public List<LockedUserDto> LockedUsers { get; set; } = new();

        /* Administrators: the teachers in the "Move all to" dropdown under Needs attention */
        public List<TeacherOptionDto> TeacherOptions { get; set; } = new();
    }

    public class AttentionQuizDto
    {
        public int QuizId { get; set; }

        public string Title { get; set; } = string.Empty;

        public string ModuleName { get; set; } = string.Empty;

        /* E.g. "No questions" or "No teacher" */
        public List<string> Problems { get; set; } = new();
    }

    public class ActivityDto
    {
        /* The part of the email before @ */
        public string DisplayName { get; set; } = string.Empty;

        public int QuizId { get; set; }

        public string QuizTitle { get; set; } = string.Empty;

        public int Score { get; set; }

        /* E.g. "2 h ago", worked out on the server (TimeAgo) */
        public string TimeAgo { get; set; } = string.Empty;
    }

    public class QuizStatsDto
    {
        public int QuizId { get; set; }

        public string Title { get; set; } = string.Empty;

        public string ModuleName { get; set; } = string.Empty;

        public int QuestionCount { get; set; }

        public int Attempts { get; set; }

        /* Rounded, null if no student has taken the quiz */
        public int? AverageScore { get; set; }
    }

    public class PlatformCountsDto
    {
        public int Students { get; set; }

        public int Teachers { get; set; }

        public int OfficialQuizzes { get; set; }

        public int PracticeQuizzes { get; set; }

        public int AttemptsThisWeek { get; set; }
    }

    public class NewUserDto
    {
        public string Id { get; set; } = string.Empty;

        public string Email { get; set; } = string.Empty;

        public bool IsTeacher { get; set; }

        public string TimeAgo { get; set; } = string.Empty;
    }

    public class LockedUserDto
    {
        public string Id { get; set; } = string.Empty;

        public string Email { get; set; } = string.Empty;

        /* true: locked by an administrator. false: locked for a while after too many wrong passwords. */
        public bool LockedByAdmin { get; set; }

        /* UTC. Only meaningful when LockedByAdmin is false. */
        public DateTimeOffset LockedUntil { get; set; }
    }
}