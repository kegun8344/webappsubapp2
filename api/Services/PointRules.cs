namespace api.Services
{
    /*
     * The point rules as plain calculations, without any database.
     * Kept separate so they are easy to unit test.
     */
    public static class PointRules
    {
        public const int FirstCompletionBonus = 10;

        /* Daily goal: collect this many stars in one day to get the bonus. Each quiz gives up to 2 stars a day. */
        public const int DailyGoalStarsNeeded = 5;
        public const int DailyGoalMaxStarsPerQuiz = 2;
        public const int DailyGoalBonus = 50;
        public const int StreakPointsPerDay = 10;
        public const int StreakMaxDays = 5;

        /* Percentage correct, 0-100. This is also what the attempt is worth in points. */
        public static int CalculateScore(int correctAnswers, int totalQuestions)
        {
            if (totalQuestions <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(totalQuestions), "A quiz must have at least one question.");
            }

            if (correctAnswers < 0 || correctAnswers > totalQuestions)
            {
                throw new ArgumentOutOfRangeException(nameof(correctAnswers),
                    "Correct answers must be between 0 and the number of questions.");
            }

            return (int)Math.Round((double)correctAnswers / totalQuestions * 100, MidpointRounding.AwayFromZero);
        }

        /* Only the best attempt counts: 80 first, then 100 later, gives 20 more. A worse attempt gives 0. */
        public static int ImprovementPoints(int newScore, int? previousBest)
        {
            return Math.Max(0, newScore - (previousBest ?? 0));
        }

        /* Day 1: 10, day 2: 20 ... day 5 and after: 50 */
        public static int StreakPoints(int streakDay)
        {
            if (streakDay < 1)
            {
                throw new ArgumentOutOfRangeException(nameof(streakDay), "Streak day must be 1 or higher.");
            }

            return Math.Min(streakDay, StreakMaxDays) * StreakPointsPerDay;
        }

        /* Which day of the streak today is, counting back from yesterday through the days the user was active */
        public static int CountStreakDay(IEnumerable<DateOnly> earlierActiveDays, DateOnly today)
        {
            var activeDays = new HashSet<DateOnly>(earlierActiveDays);
            var streakDay = 1;
            var day = today.AddDays(-1);

            while (activeDays.Contains(day))
            {
                streakDay++;
                day = day.AddDays(-1);
            }

            return streakDay;
        }

        /* "A day" means a Norwegian day, not a UTC day */
        private static readonly TimeZoneInfo NorwayTime = TimeZoneInfo.FindSystemTimeZoneById("Europe/Oslo");

        /* The Norwegian date for a UTC time, e.g. 23:30 UTC in summer is already the next day in Norway */
        public static DateOnly NorwegianDate(DateTime utcTime)
        {
            return DateOnly.FromDateTime(TimeZoneInfo.ConvertTimeFromUtc(utcTime, NorwayTime));
        }

        /* Start and end of a Norwegian day, in UTC (attempts are stored in UTC) */
        public static (DateTime StartUtc, DateTime EndUtc) DayInUtc(DateOnly day)
        {
            var start = TimeZoneInfo.ConvertTimeToUtc(day.ToDateTime(TimeOnly.MinValue), NorwayTime);
            var end = TimeZoneInfo.ConvertTimeToUtc(day.AddDays(1).ToDateTime(TimeOnly.MinValue), NorwayTime);
            return (start, end);
        }

        /*
         * Daily goal stars from today's attempts on point quizzes.
         * Each quiz gives up to 2 stars a day: 1 if the best attempt today is below 100 %, 2 if it is 100 %.
         * So taking the same quiz again and again never gives more than 2.
         */
        public static int DailyGoalStars(IEnumerable<(int QuizId, int Score)> attemptsToday)
        {
            return attemptsToday
                .GroupBy(a => a.QuizId)
                .Sum(quiz => quiz.Any(a => a.Score >= 100) ? DailyGoalMaxStarsPerQuiz : 1);
        }

        /*
         * The streak right now: days in a row up to today.
         * If the user has not been active today yet, the streak from yesterday still counts (it is not broken yet).
         */
        public static int CurrentStreak(IEnumerable<DateOnly> activeDays, DateOnly today)
        {
            var days = new HashSet<DateOnly>(activeDays);
            var day = days.Contains(today) ? today : today.AddDays(-1);
            var streak = 0;

            while (days.Contains(day))
            {
                streak++;
                day = day.AddDays(-1);
            }

            return streak;
        }

        /* The longest number of days in a row the user has ever been active */
        public static int LongestStreak(IEnumerable<DateOnly> activeDays)
        {
            var days = activeDays.Distinct().OrderBy(d => d).ToList();
            var longest = 0;
            var current = 0;

            for (var i = 0; i < days.Count; i++)
            {
                /* Continue the run if this day comes right after the previous one, otherwise start over */
                current = i > 0 && days[i] == days[i - 1].AddDays(1) ? current + 1 : 1;
                longest = Math.Max(longest, current);
            }

            return longest;
        }

        /*
         * Harder quizzes are worth more points. The multiplier is stored in tenths
         * (10 = x1.0, 15 = x1.5, 20 = x2.0) so no decimal numbers are needed.
         * An unknown difficulty counts as Easy.
         */
        public static int DifficultyTenths(string? difficulty)
        {
            return difficulty switch
            {
                "Medium" => 15,
                "Hard" => 20,
                _ => 10
            };
        }

        /* Easy: x1, Medium: x1.5, Hard: x2. Half points are rounded up (35 on Medium gives 53). */
        public static int ApplyDifficulty(int points, string? difficulty)
        {
            if (points < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(points), "Points cannot be negative.");
            }

            return (int)Math.Round(points * DifficultyTenths(difficulty) / 10.0, MidpointRounding.AwayFromZero);
        }

        /* The Monday of the week a day is in. Weeks start on Monday, as in Norway. */
        public static DateOnly StartOfWeek(DateOnly day)
        {
            /* DayOfWeek counts Sunday as 0, so shift it to make Monday 0 and Sunday 6 */
            var daysSinceMonday = ((int)day.DayOfWeek + 6) % 7;
            return day.AddDays(-daysSinceMonday);
        }

        /* How much of a module the user has completed, 0-100. A module without quizzes counts as 0. */
        public static int ModuleProgressPercent(int completedQuizzes, int totalQuizzes)
        {
            if (completedQuizzes < 0 || completedQuizzes > totalQuizzes)
            {
                throw new ArgumentOutOfRangeException(nameof(completedQuizzes),
                    "Completed quizzes must be between 0 and the total number of quizzes.");
            }

            return totalQuizzes == 0 ? 0 : completedQuizzes * 100 / totalQuizzes;
        }
    }
}