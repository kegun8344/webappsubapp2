using System.Security.Claims;
using api.DAL;
using api.DTOs;
using api.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace api.Controllers
{
    /*
     * The signed-in Home page, different for each role (same as HomeController in Subapp 1):
     * students get their streak, daily goal and quiz suggestions,
     * teachers their quiz numbers, and administrators the whole platform.
     */
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class HomeController : ControllerBase
    {
        /* How many items the lists on the Home page show */
        private const int SuggestionCount = 3;
        private const int ActivityCount = 8;
        private const int PopularCount = 5;
        private const int NewUserCount = 5;

        private readonly IPointRepository pointRepository;
        private readonly IQuizRepository quizRepository;
        private readonly IUserRepository userRepository;
        private readonly TimeProvider timeProvider;
        private readonly ILogger<HomeController> logger;

        public HomeController(
            IPointRepository pointRepo,
            IQuizRepository quizRepo,
            IUserRepository userRepo,
            TimeProvider clock,
            ILogger<HomeController> homeLogger)
        {
            pointRepository = pointRepo;
            quizRepository = quizRepo;
            userRepository = userRepo;
            timeProvider = clock;
            logger = homeLogger;
        }

        // GET: /api/home
        [HttpGet]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<ActionResult<HomeDto>> Get()
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            if (userId == null)
            {
                return Unauthorized();
            }

            if (User.IsInRole("Student"))
            {
                var student = await BuildStudentHomeAsync(userId);

                return student == null
                    ? Problem("Your streak and quiz suggestions could not be loaded.", statusCode: StatusCodes.Status500InternalServerError)
                    : new HomeDto { Role = "Student", Student = student };
            }

            var isAdmin = User.IsInRole("Admin");
            var staff = await BuildStaffHomeAsync(userId, isAdmin);

            return staff == null
                ? Problem("The overview could not be loaded.", statusCode: StatusCodes.Status500InternalServerError)
                : new HomeDto { Role = isAdmin ? "Admin" : "Teacher", Staff = staff };
        }

        /* Streak, daily goal and quiz suggestions. null if something could not be loaded (it is logged). */
        private async Task<StudentHomeDto?> BuildStudentHomeAsync(string userId)
        {
            var today = PointRules.NorwegianDate(timeProvider.GetUtcNow().UtcDateTime);

            /* Streak: the days the student got streak points, same as on the dashboard */
            var streakDays = await pointRepository.GetStreakDaysAsync(userId);

            /* Daily goal: stars from today's attempts, same rule as in PointService */
            var (startUtc, endUtc) = PointRules.DayInUtc(today);
            var attemptsToday = await pointRepository.GetAttemptsTodayAsync(userId, startUtc, endUtc);

            var improvable = await quizRepository.GetImprovableAsync(userId, SuggestionCount);
            var notTaken = await quizRepository.GetNotTakenAsync(userId, SuggestionCount);

            if (streakDays == null || attemptsToday == null || improvable == null || notTaken == null)
            {
                logger.LogError("The Home page could not be loaded for student {UserId}.", userId);
                return null;
            }

            var currentStreak = PointRules.CurrentStreak(streakDays, today);
            var keptToday = streakDays.Contains(today);
            var stars = PointRules.DailyGoalStars(attemptsToday);

            return new StudentHomeDto
            {
                StreakDays = currentStreak,
                StreakKeptToday = keptToday,
                StreakPointsToday = PointRules.StreakPoints(keptToday ? currentStreak : currentStreak + 1),
                DailyGoalStars = stars,
                DailyGoalStarsNeeded = PointRules.DailyGoalStarsNeeded,
                DailyGoalBonus = PointRules.DailyGoalBonus,
                DailyGoalReached = stars >= PointRules.DailyGoalStarsNeeded,
                ImproveQuizzes = improvable.Select(ToHomeQuiz).ToList(),
                QuizzesToTry = notTaken.Select(ToHomeQuiz).ToList()
            };
        }

        /* Needs attention, recent activity, and the teacher's or the platform's numbers */
        private async Task<StaffHomeDto?> BuildStaffHomeAsync(string userId, bool isAdmin)
        {
            var nowUtc = timeProvider.GetUtcNow().UtcDateTime;

            /* Teachers check their own quizzes, administrators every quiz */
            var quizzes = await quizRepository.GetVisibleAsync(
                new QuizQuery(userId, isAdmin, isAdmin ? QuizScope.All : QuizScope.Mine));

            /* Teachers see their own quizzes, administrators the whole platform (ownerId null) */
            var ownerId = isAdmin ? null : userId;
            var recent = await quizRepository.GetRecentStudentAttemptsAsync(ownerId, ActivityCount);

            if (quizzes == null || recent == null)
            {
                logger.LogError("The staff Home page could not be loaded for user {UserId}.", userId);
                return null;
            }

            var home = new StaffHomeDto
            {
                NeedsAttention = quizzes
                    .Select(quiz => new AttentionQuizDto
                    {
                        QuizId = quiz.Id,
                        Title = quiz.Title,
                        ModuleName = quiz.Module?.Name ?? string.Empty,
                        Problems = QuizAttentionRules.Problems(quiz, isAdmin)
                    })
                    .Where(item => item.Problems.Count > 0)
                    .ToList(),
                RecentActivity = recent
                    .Select(attempt => new ActivityDto
                    {
                        DisplayName = DisplayNames.FromEmail(attempt.UserName),
                        QuizId = attempt.QuizId,
                        QuizTitle = attempt.QuizTitle,
                        Score = attempt.Score,
                        TimeAgo = TimeAgo.Format(attempt.CompletedAt, nowUtc)
                    })
                    .ToList()
            };

            if (isAdmin)
            {
                /* "This week" starts on Monday in Norwegian time, same as the weekly leaderboard */
                var weekStart = PointRules.StartOfWeek(PointRules.NorwegianDate(nowUtc));
                var (weekStartUtc, _) = PointRules.DayInUtc(weekStart);

                var platform = await quizRepository.GetPlatformCountsAsync(weekStartUtc);
                var popular = await quizRepository.GetPopularQuizzesAsync(weekStartUtc, PopularCount);
                var newUsers = await userRepository.GetNewUsersAsync(NewUserCount);
                var lockedUsers = await userRepository.GetLockedUsersAsync();
                var teachers = await userRepository.GetTeachersAsync();

                if (platform == null || popular == null || newUsers == null || lockedUsers == null || teachers == null)
                {
                    logger.LogError("The platform overview could not be loaded for administrator {UserId}.", userId);
                    return null;
                }

                home.Platform = new PlatformCountsDto
                {
                    Students = platform.Students,
                    Teachers = platform.Teachers,
                    OfficialQuizzes = platform.OfficialQuizzes,
                    PracticeQuizzes = platform.PracticeQuizzes,
                    AttemptsThisWeek = platform.AttemptsThisWeek
                };
                home.PopularThisWeek = popular.Select(ToStatsDto).ToList();
                home.NewUsers = newUsers
                    .Select(user => new NewUserDto
                    {
                        Id = user.Id,
                        Email = user.Email,
                        IsTeacher = user.IsTeacher,
                        TimeAgo = TimeAgo.Format(user.CreatedAt, nowUtc)
                    })
                    .ToList();
                home.LockedUsers = lockedUsers
                    .Select(user => new LockedUserDto
                    {
                        Id = user.Id,
                        Email = user.Email,
                        /* An administrator locks "forever" (DateTimeOffset.MaxValue), Identity only for a few minutes */
                        LockedByAdmin = user.LockedUntil.Year > 9000,
                        LockedUntil = user.LockedUntil
                    })
                    .ToList();
                home.TeacherOptions = teachers
                    .Select(teacher => new TeacherOptionDto { Id = teacher.Id, Email = teacher.Email })
                    .ToList();

                return home;
            }

            var stats = await quizRepository.GetQuizStatsAsync(userId);
            var reached = await quizRepository.CountStudentsReachedAsync(userId);

            if (stats == null || reached == null)
            {
                logger.LogError("The quiz stats could not be loaded for teacher {UserId}.", userId);
                return null;
            }

            home.YourQuizzes = stats.Select(ToStatsDto).ToList();
            home.StudentsReached = reached.Value;
            home.TotalAttempts = stats.Sum(quiz => quiz.Attempts);

            /* Weighted by the number of attempts, the same as Subapp 1 */
            home.AverageScore = home.TotalAttempts == 0
                ? null
                : (int)Math.Round(stats
                    .Where(quiz => quiz.AverageScore.HasValue)
                    .Sum(quiz => quiz.AverageScore!.Value * quiz.Attempts) / home.TotalAttempts);

            return home;
        }

        private static HomeQuizDto ToHomeQuiz(QuizSuggestion quiz)
        {
            return new HomeQuizDto
            {
                QuizId = quiz.QuizId,
                Title = quiz.Title,
                ModuleName = quiz.ModuleName,
                Difficulty = quiz.Difficulty,
                QuestionCount = quiz.QuestionCount,
                BestScore = quiz.BestScore ?? 0
            };
        }

        private static QuizStatsDto ToStatsDto(QuizStats quiz)
        {
            return new QuizStatsDto
            {
                QuizId = quiz.QuizId,
                Title = quiz.Title,
                ModuleName = quiz.ModuleName,
                QuestionCount = quiz.QuestionCount,
                Attempts = quiz.Attempts,
                AverageScore = quiz.AverageScore is double average ? (int)Math.Round(average) : null
            };
        }
    }
}