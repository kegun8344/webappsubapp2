using api.DAL;
using api.Models;
using api.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;

namespace api.Data
{
    /*
     * Fills a new database with a realistic demonstration:
     * more teachers and students, quizzes for every module, and three weeks of
     * quiz activity, so the leaderboard, badges, streaks and review quiz are
     * alive the first time someone opens the site.
     *
     * The activity is NOT written directly. It is simulated through the real
     * PointService with a pretend clock, so every point, streak and daily goal
     * follows exactly the same rules as for a real user.
     *
     * Safe to run on every start: nothing is added if the demo quizzes exist.
     */
    public static class DemoDataSeeder
    {
        /* Used to see if the demo data has already been added */
        private const string MarkerQuizTitle = "Razor Views and Tag Helpers";

        /* The extra demo teachers (arne@teacher.no already exists) */
        public static readonly string[] ExtraTeacherEmails =
        {
            "ingrid@teacher.no",
            "magnus@teacher.no"
        };

        /* The extra demo students (dilaj@student.no already exists) */
        public static readonly string[] ExtraStudentEmails =
        {
            "sofie@student.no",
            "jonas@student.no",
            "ida@student.no",
            "mathias@student.no",
            "nora@student.no",
            "henrik@student.no",
            "thea@student.no",
            "oskar@student.no"
        };

        private static readonly TimeZoneInfo NorwayTime = TimeZoneInfo.FindSystemTimeZoneById("Europe/Oslo");

        /* A clock the seeder can set, so old attempts get old timestamps */
        private class SimulatedClock : TimeProvider
        {
            public DateTimeOffset Now { get; set; }

            public override DateTimeOffset GetUtcNow() => Now;
        }

        /*
         * How a demo student behaves:
         * - Skill: chance (0-1) of answering a question correctly
         * - ActiveChance: chance of being active on a day without a guaranteed streak
         * - StreakDays: the student is active on each of the last N days (today included)
         * - Min/MaxPerDay: how many quizzes the student takes on an active day
         * - StartDaysAgo: how long ago the student started
         * - PracticeQuiz: the student's own private practice quiz, if any
         */
        private record StudentProfile(
            string Email, double Skill, double ActiveChance, int StreakDays,
            int MinPerDay, int MaxPerDay, int StartDaysAgo, string? PracticeQuiz);

        private static readonly StudentProfile[] Profiles =
        {
            new("oskar@student.no",   0.45, 0.25, 0, 1, 2, 14, null),
            new("henrik@student.no",  0.55, 0.35, 0, 1, 2, 17, null),
            new("dilaj@student.no",   0.62, 0.45, 2, 1, 2, 10, null),
            new("mathias@student.no", 0.66, 0.50, 0, 1, 3, 18, null),
            new("thea@student.no",    0.72, 0.75, 0, 1, 3, 5,  null),
            new("ida@student.no",     0.75, 0.60, 0, 1, 3, 20, "React Hooks Cheat Sheet"),
            new("nora@student.no",    0.82, 0.60, 4, 1, 3, 20, "Identity Revision"),
            new("jonas@student.no",   0.86, 0.80, 0, 1, 4, 21, "EF Core Exam Prep"),
            new("sofie@student.no",   0.93, 0.90, 31, 3, 5, 35, "My MVC Cheat Sheet")
        };

        public static async Task SeedAsync(ApplicationDbContext context, TimeProvider clock)
        {
            if (await context.Quizzes.AnyAsync(q => q.Title == MarkerQuizTitle))
            {
                return;
            }

            /* All or nothing on a real database, so a failure never leaves half a demo */
            var transaction = context.Database.IsRelational()
                ? await context.Database.BeginTransactionAsync()
                : null;

            try
            {
                var emails = ExtraTeacherEmails
                    .Concat(ExtraStudentEmails)
                    .Append("arne@teacher.no")
                    .Append("dilaj@student.no")
                    .ToList();

                var users = await context.Users
                    .Where(u => u.Email != null && emails.Contains(u.Email))
                    .ToDictionaryAsync(u => u.Email!);

                var modules = await context.Modules.ToDictionaryAsync(m => m.Name, m => m.Id);

                await AddQuizzesAsync(context, users, modules);
                await SimulateActivityAsync(context, clock, users);

                if (transaction != null)
                {
                    await transaction.CommitAsync();
                }
            }
            finally
            {
                if (transaction != null)
                {
                    await transaction.DisposeAsync();
                }
            }
        }

        // ---------------------------------------------------------------- quizzes

        private static async Task AddQuizzesAsync(
            ApplicationDbContext context,
            Dictionary<string, ApplicationUser> users,
            Dictionary<string, int> modules)
        {
            string UserId(string email) => users.TryGetValue(email, out var user)
                ? user.Id
                : throw new InvalidOperationException($"Demo user '{email}' was not found.");

            int ModuleId(string name) => modules.TryGetValue(name, out var id)
                ? id
                : throw new InvalidOperationException($"Required module '{name}' was not found.");

            var arne = UserId("arne@teacher.no");
            var ingrid = UserId("ingrid@teacher.no");
            var magnus = UserId("magnus@teacher.no");

            const string mvc = "ASP.NET MVC";
            const string ef = "Entity Framework Core";
            const string validation = "Validation & Error Handling";
            const string auth = "Authentication & Authorization";
            const string api = "Web API";
            const string react = "React / SPA";
            const string exam = "Exam Practice";

            var quizzes = new List<Quiz>
            {
                // ---- Official quizzes from teachers (public, give points) ----
                Official("Razor Views and Tag Helpers", "Razor syntax, layouts, partial views and tag helpers.",
                    "Easy", ModuleId(mvc), ingrid,
                    Q("Which character starts a C# expression in a Razor view?", 1, "#", "@", "$", "%"),
                    Q("Which tag helper attribute builds a link to a controller action?", 2, "href-action", "asp-url", "asp-action", "asp-link"),
                    Q("What does @model specify at the top of a view?", 0, "The type of the data the view receives", "The database table", "The CSS file", "The controller name"),
                    Q("What is a layout (_Layout.cshtml) used for?", 3, "Validating forms", "Storing data", "Routing requests", "Sharing the common page structure between views"),
                    Q("Which tag helper renders a partial view?", 1, "<include>", "<partial>", "<render>", "<insert>")),

                Official("Routing and Model Binding", "How a URL reaches an action and how form data becomes C# objects.",
                    "Medium", ModuleId(mvc), arne,
                    Q("What is the default route pattern in a new MVC project?", 0, "{controller=Home}/{action=Index}/{id?}", "{action}/{controller}", "{controller}/{id}", "{area}/{view}"),
                    Q("What does the ? in {id?} mean?", 2, "The parameter is encrypted", "The parameter must be a number", "The parameter is optional", "The parameter is a string only"),
                    Q("Which attribute restricts an action to POST requests?", 1, "[PostOnly]", "[HttpPost]", "[Submit]", "[FormPost]"),
                    Q("What is model binding?", 3, "Joining two database tables", "Compiling views", "Encrypting form data", "Mapping request data to action parameters or model properties"),
                    Q("Why use a ViewModel instead of the entity directly?", 0, "To send only the fields the view needs and avoid over-posting", "To make the database faster", "To skip validation", "Because entities cannot be used in views")),

                Official("EF Core Relationships and Migrations", "One-to-many, many-to-many, delete behaviour and migrations.",
                    "Hard", ModuleId(ef), ingrid,
                    Q("Which relationship needs a join table in EF Core (created automatically)?", 2, "One-to-one", "One-to-many", "Many-to-many", "None of them"),
                    Q("What does DeleteBehavior.Restrict do?", 1, "Deletes related rows automatically", "Prevents deleting a row that still has related rows", "Sets the foreign key to null", "Ignores the foreign key"),
                    Q("What does DeleteBehavior.SetNull do when the principal is deleted?", 3, "Throws an exception", "Deletes the dependent rows", "Does nothing", "Sets the foreign key of the dependents to null"),
                    Q("Which method avoids change tracking for read-only queries?", 0, "AsNoTracking()", "AsReadOnly()", "NoTrack()", "Detach()"),
                    Q("Which command creates a new migration from model changes?", 2, "dotnet ef database update", "dotnet ef migrations remove", "dotnet ef migrations add <Name>", "dotnet build")),

                Official("Repository Pattern and Dependency Injection", "Hiding data access behind interfaces and registering services.",
                    "Medium", ModuleId(ef), magnus,
                    Q("What is the main purpose of the repository pattern?", 1, "Make pages load faster", "Hide data access behind an interface", "Replace the database", "Generate HTML"),
                    Q("Which service lifetime creates one instance per HTTP request?", 0, "Scoped", "Singleton", "Transient", "Static"),
                    Q("Where are services registered for dependency injection?", 3, "In the view", "In appsettings.json", "In the controller", "On builder.Services in Program.cs"),
                    Q("Why do controllers depend on an interface such as IQuizRepository and not a class?", 2, "Interfaces run faster", "Classes cannot be injected", "The implementation can be replaced, for example by a fake in unit tests", "It is required by C#"),
                    Q("Which method loads a list asynchronously with EF Core?", 1, "ToList()", "ToListAsync()", "GetList()", "LoadAll()")),

                Official("Unit Testing with xUnit", "Writing positive and negative tests for controllers and rules.",
                    "Medium", ModuleId(validation), magnus,
                    Q("Which attribute marks a test method in xUnit?", 0, "[Fact]", "[Test]", "[TestMethod]", "[Spec]"),
                    Q("What do Arrange, Act and Assert describe?", 2, "Three database operations", "Three HTTP verbs", "The structure of a test", "Three layers of MVC"),
                    Q("Why use UseInMemoryDatabase in tests?", 1, "To test the real SQLite file", "To get a fresh, fast database without touching the real one", "To skip EF", "To log queries"),
                    Q("Which assertion checks that a collection has exactly one item?", 3, "Assert.Equal", "Assert.True", "Assert.NotNull", "Assert.Single"),
                    Q("Why replace a dependency with a fake in a test?", 0, "So only the class under test is tested", "To make tests longer", "To avoid assertions", "To test the framework")),

                Official("Identity, Roles and Authorization", "Protecting pages with sign-in and roles.",
                    "Medium", ModuleId(auth), magnus,
                    Q("Which attribute requires a signed-in user?", 0, "[Authorize]", "[Authenticate]", "[LoggedIn]", "[Secure]"),
                    Q("How do you restrict an action to teachers and admins?", 2, "[Authorize(Teacher)]", "[Roles(\"Teacher\")]", "[Authorize(Roles = \"Teacher,Admin\")]", "[Allow(\"Teacher\")]"),
                    Q("What is the difference between authentication and authorization?", 1, "They are the same thing", "Authentication proves who you are, authorization decides what you may do", "Authorization proves who you are, authentication decides what you may do", "Authentication only applies to APIs"),
                    Q("Which middleware order is correct?", 3, "UseAuthorization before UseAuthentication", "UseRouting after MapControllerRoute", "Neither is needed", "UseAuthentication before UseAuthorization"),
                    Q("Why must the server check permissions even if a button is hidden?", 0, "Users can send requests manually without using the button", "Hidden buttons slow the page", "Razor cannot hide elements", "Browsers ignore hidden elements")),

                Official("Passwords, Cookies and Security Basics", "Hashing, lockout, CSRF, XSS and SQL injection.",
                    "Easy", ModuleId(auth), arne,
                    Q("How does ASP.NET Core Identity store passwords?", 2, "As plain text", "Encrypted with a key anyone can read", "As a salted hash", "In the cookie"),
                    Q("What does account lockout protect against?", 1, "SQL injection", "Brute-force password guessing", "Cross-site scripting", "Slow queries"),
                    Q("Which attribute helps prevent CSRF on POST actions?", 3, "[Authorize]", "[HttpGet]", "[AllowAnonymous]", "[ValidateAntiForgeryToken]"),
                    Q("What does Razor do with @Model.Name by default?", 0, "HTML-encodes it, which helps prevent XSS", "Writes it unchanged", "Removes all tags", "Sends it to the database"),
                    Q("What helps avoid SQL injection?", 2, "String concatenation in SQL", "Disabling validation", "Parameterised queries or LINQ", "Longer passwords")),

                Official("REST and HTTP Fundamentals", "HTTP methods, status codes and JSON.",
                    "Easy", ModuleId(api), ingrid,
                    Q("Which HTTP method is normally used to read a resource?", 0, "GET", "POST", "PUT", "DELETE"),
                    Q("Which status code means a resource was created?", 2, "200", "204", "201", "404"),
                    Q("Which HTTP method normally creates a new resource?", 1, "GET", "POST", "HEAD", "OPTIONS"),
                    Q("What does status code 401 mean?", 3, "Not found", "Server error", "Bad request", "Not authenticated"),
                    Q("Which format is most often used for the body in REST APIs?", 0, "JSON", "BMP", "EXE", "CSS")),

                Official("Building a Web API with ASP.NET Core", "Controllers, DTOs, status codes and binding in an API.",
                    "Medium", ModuleId(api), magnus,
                    Q("Which attribute marks a controller as an API controller?", 2, "[ApiPage]", "[RestController]", "[ApiController]", "[JsonController]"),
                    Q("What does an action returning ActionResult<T> allow?", 1, "Only returning views", "Returning either a value of T or an HTTP result such as NotFound()", "Only returning strings", "Skipping routing"),
                    Q("Why use DTOs in an API?", 3, "They make the database smaller", "They replace validation", "They are required by JSON", "To expose only the fields the client needs, not the entities"),
                    Q("Which status should a missing resource return?", 0, "404 Not Found", "200 OK", "302 Found", "500 Internal Server Error"),
                    Q("What does [FromBody] tell ASP.NET Core?", 2, "Read the value from the URL", "Read the value from a cookie", "Bind the parameter from the request body", "Skip binding")),

                Official("React Basics", "Components, JSX, state and props.",
                    "Easy", ModuleId(react), arne,
                    Q("What is JSX?", 1, "A database language", "A syntax that looks like HTML inside JavaScript", "A CSS framework", "A server"),
                    Q("Which hook stores state in a function component?", 0, "useState", "useServer", "useStore", "useVariable"),
                    Q("How do you pass data from a parent to a child component?", 3, "Global variables only", "Cookies", "The database", "Props"),
                    Q("What should you add to each item rendered with map()?", 2, "A class", "A database id", "A unique key prop", "A style"),
                    Q("Which command typically starts a Vite dev server?", 1, "dotnet run", "npm run dev", "npm build all", "vite stop")),

                Official("React and API Integration", "Fetching data from the ASP.NET API in a React app.",
                    "Medium", ModuleId(react), ingrid,
                    Q("Which hook is normally used to fetch data when a component mounts?", 2, "useState", "useRef", "useEffect", "useMemo"),
                    Q("Which browser feature blocks calls to another origin unless the server allows it?", 0, "CORS", "Cookies", "Caching", "Compression"),
                    Q("Where should the API base URL ideally be kept?", 3, "Hardcoded in every component", "In the database", "In the HTML title", "In configuration, for example an environment variable"),
                    Q("What does fetch() return?", 1, "The JSON data directly", "A Promise", "A string", "A React component"),
                    Q("Why must a SPA never contain secrets such as database passwords?", 2, "JavaScript cannot store strings", "React forbids it", "Everything sent to the browser can be read by the user", "Secrets slow down rendering")),

                Official("Exam Practice: Mixed Revision 1", "Mixed questions covering the whole course.",
                    "Medium", ModuleId(exam), arne,
                    Q("Where should business rules such as 'a module with quizzes cannot be deleted' be enforced?", 2, "In the Razor view", "In the CSS", "On the server (controller or service)", "Only in the browser"),
                    Q("What is the purpose of a unit test?", 1, "Test the whole system through a browser", "Check one small piece of code in isolation", "Measure server load", "Check the design"),
                    Q("What is a negative test?", 0, "A test that checks invalid input or forbidden actions are handled correctly", "A test that fails on purpose", "A test without assertions", "A slow test"),
                    Q("What does ILogger<T> help you do?", 3, "Draw charts", "Validate forms", "Open the database", "Record information, warnings and errors"),
                    Q("Which status fits a signed-in user who lacks permission?", 1, "401 Unauthorized", "403 Forbidden", "200 OK", "201 Created"),
                    Q("Which is an example of conditional rendering in Razor?", 2, "@section", "@using", "@if (condition) { ... }", "@inject")),

                Official("Exam Practice: Mixed Revision 2", "Harder mixed questions about design and security.",
                    "Hard", ModuleId(exam), magnus,
                    Q("Why is the repository pattern useful for both an MVC app and a Web API?", 0, "The same data access code can be reused behind an interface", "It removes the need for a database", "It makes views faster", "It replaces Identity"),
                    Q("A user changes the quiz ID in the URL to open someone else's private quiz. What protects against this?", 3, "Hiding the link", "A longer URL", "Client-side JavaScript", "Server-side ownership checks on every request"),
                    Q("What is over-posting?", 1, "Posting too many requests", "A user setting properties through a form that were not meant to be editable", "Sending large files", "Duplicate migrations"),
                    Q("How is a many-to-many relation between Quiz and Tag stored?", 2, "A comma-separated string", "Two unrelated tables", "A join table (EF Core creates QuizTags)", "Duplicated tags on each quiz"),
                    Q("Why are attempts stored as separate rows instead of overwritten?", 0, "To keep a history and calculate best results and streaks", "To waste space", "Because SQLite requires it", "To avoid migrations"),
                    Q("What does async/await achieve in a controller action that queries the database?", 1, "Makes the query faster in the database", "Frees the thread while waiting for the database", "Removes the need for await in EF", "Adds caching")),

                // ---- A teacher's unpublished draft: awards points, but only the teacher can see it ----
                new Quiz
                {
                    Title = "Draft: Logging in Practice",
                    Description = "Work in progress. Not published yet.",
                    Difficulty = "Medium",
                    ModuleId = ModuleId(validation),
                    CreatedByUserId = ingrid,
                    IsPublic = false,
                    AwardsPoints = true,
                    Questions = new List<Question>
                    {
                        Q("Which log level is meant for unexpected failures?", 2, "Trace", "Information", "Error", "Debug"),
                        Q("Why should log messages never contain passwords?", 1, "Logs are too short", "Logs are stored and can be read by others", "Passwords are numbers", "Loggers cannot print text"),
                        Q("What is a benefit of message templates like User {UserId}?", 0, "Values stay structured and can be searched", "Messages are encrypted", "Logs get shorter", "Exceptions are hidden")
                    }
                },

                // ---- Students' own practice quizzes (private, no points) ----
                Practice("My MVC Cheat Sheet", "Quick facts I keep forgetting.", "Easy", ModuleId(mvc), UserId("sofie@student.no"),
                    Q("Which folder holds shared layouts?", 2, "wwwroot", "Models", "Views/Shared", "Data"),
                    Q("What does a controller action return to show a page?", 0, "IActionResult (for example View())", "A database", "A CSS file", "void only"),
                    Q("What does [Required] do?", 1, "Hides a field", "Makes a value mandatory in validation", "Adds an index", "Sets a default"),
                    Q("What does Program.cs do?", 3, "Stores data", "Contains the views", "Defines the CSS", "Configures services and the request pipeline")),

                Practice("EF Core Exam Prep", "My own questions before the exam.", "Medium", ModuleId(ef), UserId("jonas@student.no"),
                    Q("Which method adds a new entity to the context?", 1, "Remove()", "Add()", "Detach()", "Find()"),
                    Q("What does FirstOrDefaultAsync return if nothing matches?", 2, "An exception", "An empty list", "null", "Zero"),
                    Q("What does a foreign key do?", 0, "Links a row to a row in another table", "Encrypts a column", "Sorts rows", "Deletes a table"),
                    Q("What is a migration?", 3, "A user account", "A CSS file", "A test", "A versioned change to the database schema")),

                Practice("Identity Revision", "Short revision of sign-in and roles.", "Easy", ModuleId(auth), UserId("nora@student.no"),
                    Q("Which class represents a user in Identity?", 0, "IdentityUser", "UserTable", "Account", "Login"),
                    Q("What is a role?", 2, "A password", "A table", "A group of permissions such as Teacher or Student", "A cookie"),
                    Q("Which service signs a user in?", 1, "RoleManager", "SignInManager", "DbContext", "ILogger"),
                    Q("Where is the login cookie stored?", 3, "On the server only", "In the database only", "In the HTML", "In the browser")),

                Practice("React Hooks Cheat Sheet", "Hooks I need for the SPA.", "Easy", ModuleId(react), UserId("ida@student.no"),
                    Q("Which hook runs code after a render?", 0, "useEffect", "useRender", "useAfter", "useLoad"),
                    Q("Can hooks be called inside loops?", 1, "Yes, always", "No, they must be called at the top level", "Only in classes", "Only with props"),
                    Q("What causes a component to render again?", 2, "Editing the CSS", "Restarting the server", "A change in state or props", "A change in the database"),
                    Q("What does setCount(count + 1) do?", 3, "Reads the state", "Deletes the state", "Starts the server", "Updates the state and renders again"))
            };

            context.Quizzes.AddRange(quizzes);
            await context.SaveChangesAsync();

            await AddTagsAsync(context, users);
        }

        /* Tags: shared catalog tags on the teacher quizzes, and one personal tag for a student */
        private static async Task AddTagsAsync(
            ApplicationDbContext context,
            Dictionary<string, ApplicationUser> users)
        {
            var sharedTags = await context.Tags
                .Where(t => t.OwnerUserId == null)
                .ToDictionaryAsync(t => t.Name);

            var tagsByQuiz = new Dictionary<string, string[]>
            {
                ["ASP.NET MVC Basics"] = new[] { "Beginner" },
                ["Entity Framework Core"] = new[] { "Exam relevant" },
                ["Validation, Errors and Logging"] = new[] { "Advanced", "Exam relevant" },
                ["Razor Views and Tag Helpers"] = new[] { "Beginner", "Short quiz" },
                ["Routing and Model Binding"] = new[] { "Exam relevant" },
                ["EF Core Relationships and Migrations"] = new[] { "Advanced", "Exam relevant" },
                ["Repository Pattern and Dependency Injection"] = new[] { "Exam relevant" },
                ["Unit Testing with xUnit"] = new[] { "Practice" },
                ["Identity, Roles and Authorization"] = new[] { "Exam relevant" },
                ["Passwords, Cookies and Security Basics"] = new[] { "Beginner", "Short quiz" },
                ["REST and HTTP Fundamentals"] = new[] { "Beginner" },
                ["Building a Web API with ASP.NET Core"] = new[] { "Exam relevant" },
                ["React Basics"] = new[] { "Beginner", "Short quiz" },
                ["React and API Integration"] = new[] { "Advanced" },
                ["Exam Practice: Mixed Revision 1"] = new[] { "Exam relevant", "Practice" },
                ["Exam Practice: Mixed Revision 2"] = new[] { "Advanced", "Exam relevant", "Practice" }
            };

            var titles = tagsByQuiz.Keys.ToList();

            var quizzes = await context.Quizzes
                .Include(q => q.Tags)
                .Where(q => titles.Contains(q.Title))
                .ToListAsync();

            foreach (var quiz in quizzes.Where(q => q.Tags.Count == 0))
            {
                foreach (var tagName in tagsByQuiz[quiz.Title])
                {
                    if (sharedTags.TryGetValue(tagName, out var tag))
                    {
                        quiz.Tags.Add(tag);
                    }
                }
            }

            /* A personal tag only its owner can see or use */
            if (users.TryGetValue("sofie@student.no", out var sofie))
            {
                var personalTag = new Tag { Name = "Cheat sheet", Color = "warning", OwnerUserId = sofie.Id };

                var cheatSheet = await context.Quizzes
                    .Include(q => q.Tags)
                    .FirstOrDefaultAsync(q => q.Title == "My MVC Cheat Sheet");

                cheatSheet?.Tags.Add(personalTag);
            }

            await context.SaveChangesAsync();
        }

        // ---------------------------------------------------------------- activity

        private static async Task SimulateActivityAsync(
            ApplicationDbContext context,
            TimeProvider clock,
            Dictionary<string, ApplicationUser> users)
        {
            var nowUtc = clock.GetUtcNow().UtcDateTime;
            var today = PointRules.NorwegianDate(nowUtc);

            /* The real point rules, running on a clock that the seeder moves */
            var simulatedClock = new SimulatedClock();
            var pointService = new PointService(
                new PointRepository(context, NullLogger<PointRepository>.Instance),
                NullLogger<PointService>.Instance,
                simulatedClock);

            var allQuizzes = await context.Quizzes
                .Include(q => q.Questions)
                    .ThenInclude(question => question.AnswerOptions)
                .OrderBy(q => q.Id)
                .ToListAsync();

            /* Quizzes without questions cannot be taken, so they are skipped */
            var officialQuizzes = allQuizzes.Where(q => q.AwardsPoints && q.IsPublic && q.Questions.Count > 0).ToList();

            foreach (var profile in Profiles)
            {
                if (!users.TryGetValue(profile.Email, out var student))
                {
                    continue;
                }

                /* A real user with history is left alone, so real data is never mixed with demo data */
                if (await context.QuizAttempts.AnyAsync(a => a.UserId == student.Id))
                {
                    continue;
                }

                /* The same student always gets the same history (a fixed seed from the e-mail) */
                var rng = new Random(profile.Email.Aggregate(17, (hash, c) => hash * 31 + c));
                var bestScores = new Dictionary<int, int>();
                var practiceQuiz = profile.PracticeQuiz == null
                    ? null
                    : allQuizzes.FirstOrDefault(q => q.Title == profile.PracticeQuiz);

                for (var daysAgo = profile.StartDaysAgo; daysAgo >= 0; daysAgo--)
                {
                    var isActive = daysAgo < profile.StreakDays || rng.NextDouble() < profile.ActiveChance;
                    if (!isActive)
                    {
                        continue;
                    }

                    var date = today.AddDays(-daysAgo);
                    var quizzesToday = rng.Next(profile.MinPerDay, profile.MaxPerDay + 1);

                    /* One more attempt at the student's own practice quiz on two of the days */
                    var plan = new List<Quiz?>();
                    for (var slot = 0; slot < quizzesToday; slot++)
                    {
                        plan.Add(null);
                    }
                    if (practiceQuiz != null && (daysAgo == 3 || daysAgo == 1))
                    {
                        plan.Add(practiceQuiz);
                    }

                    for (var slot = 0; slot < plan.Count; slot++)
                    {
                        var quiz = plan[slot] ?? PickOfficialQuiz(officialQuizzes, bestScores, rng);
                        if (quiz == null)
                        {
                            /* There are no official quizzes at all */
                            break;
                        }

                        var isRetake = bestScores.ContainsKey(quiz.Id);
                        var attemptTime = AttemptTime(date, daysAgo, slot, plan.Count, nowUtc, rng);
                        simulatedClock.Now = new DateTimeOffset(attemptTime, TimeSpan.Zero);

                        await TakeQuizAsync(context, pointService, student.Id, quiz, profile.Skill, isRetake, attemptTime, rng, bestScores);
                    }
                }
            }
        }

        /*
         * Mostly a quiz the student has not taken yet. When all are taken, or sometimes
         * by choice, the quiz with the lowest best score is retaken to improve it.
         */
        private static Quiz? PickOfficialQuiz(List<Quiz> officialQuizzes, Dictionary<int, int> bestScores, Random rng)
        {
            var untaken = officialQuizzes.Where(q => !bestScores.ContainsKey(q.Id)).ToList();
            var retakable = officialQuizzes
                .Where(q => bestScores.TryGetValue(q.Id, out var best) && best < 100)
                .OrderBy(q => bestScores[q.Id])
                .ThenBy(q => q.Id)
                .ToList();

            if (untaken.Count > 0 && (retakable.Count == 0 || rng.NextDouble() < 0.8))
            {
                return untaken[rng.Next(untaken.Count)];
            }

            if (retakable.Count > 0)
            {
                return retakable[0];
            }

            /* Full points on everything: an active student still practises, which keeps the streak going */
            return officialQuizzes.Count > 0 ? officialQuizzes[rng.Next(officialQuizzes.Count)] : null;
        }

        /* Today's attempts are placed shortly before "now" so none of them is in the future */
        private static DateTime AttemptTime(DateOnly date, int daysAgo, int slot, int slotsToday, DateTime nowUtc, Random rng)
        {
            if (daysAgo == 0)
            {
                return nowUtc.AddMinutes(-(15 + 12 * (slotsToday - 1 - slot)));
            }

            var local = date.ToDateTime(new TimeOnly(13, 0)).AddMinutes(slot * 55 + rng.Next(0, 30));
            return TimeZoneInfo.ConvertTimeToUtc(DateTime.SpecifyKind(local, DateTimeKind.Unspecified), NorwayTime);
        }

        /* Takes one quiz: decides which questions are right, saves through PointService, stores the answers */
        private static async Task TakeQuizAsync(
            ApplicationDbContext context,
            PointService pointService,
            string userId,
            Quiz quiz,
            double skill,
            bool isRetake,
            DateTime attemptTime,
            Random rng,
            Dictionary<int, int> bestScores)
        {
            var penalty = quiz.Difficulty switch
            {
                "Medium" => 0.07,
                "Hard" => 0.15,
                _ => 0.0
            };

            /* A retake is a bit easier, because the student has seen the questions before */
            var chanceCorrect = Math.Clamp(skill - penalty + (isRetake ? 0.12 : 0), 0.05, 0.98);

            var questions = quiz.Questions.OrderBy(q => q.Id).ToList();
            var isCorrect = questions.Select(_ => rng.NextDouble() < chanceCorrect).ToList();
            var correctCount = isCorrect.Count(c => c);

            var result = await pointService.SaveAttemptAsync(userId, quiz, correctCount, questions.Count)
                ?? throw new InvalidOperationException("The demo attempt could not be saved.");
            bestScores[quiz.Id] = result.BestScore;

            for (var i = 0; i < questions.Count; i++)
            {
                var options = questions[i].AnswerOptions;
                var chosen = isCorrect[i]
                    ? options.First(o => o.IsCorrect)
                    : options.Where(o => !o.IsCorrect).OrderBy(_ => rng.Next()).First();

                context.UserAnswers.Add(new UserAnswer
                {
                    UserId = userId,
                    QuestionId = questions[i].Id,
                    SelectedAnswerId = chosen.Id,
                    IsCorrect = isCorrect[i],
                    AnsweredAt = attemptTime
                });
            }

            await context.SaveChangesAsync();
        }

        // ---------------------------------------------------------------- helpers

        /* A quiz made by a teacher: public and gives points */
        private static Quiz Official(
            string title, string description, string difficulty, int moduleId, string teacherId,
            params Question[] questions)
        {
            return new Quiz
            {
                Title = title,
                Description = description,
                Difficulty = difficulty,
                ModuleId = moduleId,
                CreatedByUserId = teacherId,
                IsPublic = true,
                AwardsPoints = true,
                Questions = questions.ToList()
            };
        }

        /* A student's own quiz: private and gives no points */
        private static Quiz Practice(
            string title, string description, string difficulty, int moduleId, string studentId,
            params Question[] questions)
        {
            return new Quiz
            {
                Title = title,
                Description = description,
                Difficulty = difficulty,
                ModuleId = moduleId,
                CreatedByUserId = studentId,
                IsPublic = false,
                AwardsPoints = false,
                Questions = questions.ToList()
            };
        }

        /* A question with its answer options. correctIndex is zero-based. */
        private static Question Q(string text, int correctIndex, params string[] options)
        {
            return new Question
            {
                Text = text,
                AnswerOptions = options
                    .Select((option, index) => new AnswerOption
                    {
                        Text = option,
                        IsCorrect = index == correctIndex
                    })
                    .ToList()
            };
        }
    }
}