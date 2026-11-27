using System.ComponentModel.DataAnnotations;

namespace api.DTOs
{
    /* A teacher in the "Move to" dropdowns */
    public class TeacherOptionDto
    {
        public string Id { get; set; } = string.Empty;

        public string Email { get; set; } = string.Empty;
    }

    /* The Change teacher page (QuizTeacher/Move in Subapp 1) */
    public class MoveQuizInfoDto
    {
        public int QuizId { get; set; }

        public string Title { get; set; } = string.Empty;

        /* null when the quiz has no teacher */
        public string? CurrentTeacherEmail { get; set; }

        /* Every teacher except the current owner */
        public List<TeacherOptionDto> Teachers { get; set; } = new();
    }

    /* The teacher chosen in the dropdown. The server checks that the user really is a teacher. */
    public class MoveQuizDto
    {
        [Required(ErrorMessage = "Choose a teacher.")]
        public string? TeacherId { get; set; }
    }

    /* The answer after "Move all" */
    public class MoveAllResultDto
    {
        public int Moved { get; set; }

        public string TeacherEmail { get; set; } = string.Empty;
    }
}