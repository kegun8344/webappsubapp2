using api.Models;
using api.Services;

namespace api.Data
{
    /*
     * Adds demonstration content to a new StudyQuest database.
     *
     * The demo quizzes represent official learning material created
     * by the seeded teacher account. This gives users meaningful
     * content to test immediately after starting the application.
     */
    public static class DbSeeder
    {
        /*
         * Adds a small starter catalog of shared tags.
         * Safe to run on every start: a tag is only added if no shared tag
         * with the same name exists.
         */
        public static void SeedTags(ApplicationDbContext context)
        {
            var starterTags = new (string Name, string Color)[]
            {
                ("Short quiz", "info"),
                ("Exam relevant", "danger"),
                ("Beginner", "success"),
                ("Advanced", "dark"),
                ("Practice", "primary")
            };

            foreach (var (name, color) in starterTags)
            {
                var exists = context.Tags.Any(
                    tag => tag.OwnerUserId == null &&
                           tag.Name.ToLower() == name.ToLower());

                if (!exists)
                {
                    context.Tags.Add(new Tag { Name = name, Color = color });
                }
            }

            context.SaveChanges();
        }

        /*
         * Gives the modules created by the migration a fitting icon.
         * Only modules that still have the default icon are changed, so an icon
         * a teacher has chosen is never overwritten. Safe to run on every start.
         */
        public static void SeedModuleIcons(ApplicationDbContext context)
        {
            var modules = context.Modules
                .Where(module => module.IconKey == "" || module.IconKey == ModuleIcons.Default)
                .ToList();

            foreach (var module in modules)
            {
                var icon = ModuleIcons.ForSeededModule(module.Name);

                if (icon != null)
                {
                    module.IconKey = icon;
                }
            }

            context.SaveChanges();
        }

        public static void Seed(
            ApplicationDbContext context,
            string teacherId)
        {
            // The tag catalog is seeded first, even if the demo quizzes already exist.
            SeedTags(context);
            SeedModuleIcons(context);

            /*
             * Demo content should only be inserted once.
             * If quizzes already exist, the database has already
             * been initialized and no additional demo quizzes are added.
             */
            if (context.Quizzes.Any())
            {
                return;
            }

            /*
            * The ITPE3200 modules are created by the database migration.
            * Look them up by name instead of relying on hardcoded database IDs.
            */
            var mvcModuleId =
                GetRequiredModuleId(
                    context,
                    "ASP.NET MVC");

            var entityFrameworkModuleId =
                GetRequiredModuleId(
                    context,
                    "Entity Framework Core");

            var validationModuleId =
                GetRequiredModuleId(
                    context,
                    "Validation & Error Handling");

            var quizzes = new List<Quiz>
            {
                new Quiz
                {
                    Title = "ASP.NET MVC Basics",
                    Description =
                        "Test your knowledge of the Model-View-Controller pattern in ASP.NET Core.",
                    Difficulty = "Easy",
                    ModuleId = mvcModuleId,

                    // Demo quizzes represent teacher-created course material.
                    CreatedByUserId = teacherId,
                    IsPublic = true,
                    AwardsPoints = true,

                    Questions = new List<Question>
                    {
                        CreateQuestion(
                            "Which component in MVC handles incoming requests?",
                            2,
                            "Model",
                            "View",
                            "Controller",
                            "Database"),

                        CreateQuestion(
                            "What does MVC stand for?",
                            0,
                            "Model View Controller",
                            "Main View Component",
                            "Model Version Control",
                            "Multiple View Controller"),

                        CreateQuestion(
                            "Which file extension is used for Razor views?",
                            1,
                            ".html",
                            ".cshtml",
                            ".cs",
                            ".razorview"),

                        CreateQuestion(
                            "Where are views that are shared by several controllers usually placed?",
                            3,
                            "Views/Home",
                            "wwwroot",
                            "Controllers",
                            "Views/Shared"),

                        CreateQuestion(
                            "Which action is used by default when the URL does not specify one?",
                            0,
                            "Index",
                            "Home",
                            "Default",
                            "Main")
                    }
                },

                new Quiz
                {
                    Title = "Entity Framework Core",
                    Description =
                        "Questions about DbContext, migrations and LINQ queries.",
                    Difficulty = "Medium",
                    ModuleId = entityFrameworkModuleId,

                    // Demo quizzes represent teacher-created course material.
                    CreatedByUserId = teacherId,
                    IsPublic = true,
                    AwardsPoints = true,

                    Questions = new List<Question>
                    {
                        CreateQuestion(
                            "Which class represents a session with the database in EF Core?",
                            1,
                            "DbSet",
                            "DbContext",
                            "SqlConnection",
                            "Migration"),

                        CreateQuestion(
                            "What does a DbSet<T> usually represent?",
                            0,
                            "A table in the database",
                            "A single row",
                            "A connection string",
                            "A Razor view"),

                        CreateQuestion(
                            "Which command applies migrations to the database?",
                            2,
                            "dotnet ef migrations add",
                            "dotnet build",
                            "dotnet ef database update",
                            "dotnet ef database drop"),

                        CreateQuestion(
                            "What does Include() do in a LINQ query?",
                            3,
                            "Filters the rows",
                            "Sorts the result",
                            "Deletes related data",
                            "Loads related data (eager loading)"),

                        CreateQuestion(
                            "Which method writes tracked changes to the database?",
                            1,
                            "Update()",
                            "SaveChangesAsync()",
                            "Commit()",
                            "Flush()")
                    }
                },

                new Quiz
                {
                    Title = "Validation, Errors and Logging",
                    Description =
                        "Server-side validation, error handling and logging in ASP.NET Core.",
                    Difficulty = "Hard",
                    ModuleId = validationModuleId,

                    // Demo quizzes represent teacher-created course material.
                    CreatedByUserId = teacherId,
                    IsPublic = true,
                    AwardsPoints = true,

                    Questions = new List<Question>
                    {
                        CreateQuestion(
                            "Which property tells you if the submitted form passed validation?",
                            0,
                            "ModelState.IsValid",
                            "Model.IsValid",
                            "Request.IsValid",
                            "ViewData.IsValid"),

                        CreateQuestion(
                            "Which attribute makes a property mandatory?",
                            2,
                            "[Key]",
                            "[MaxLength]",
                            "[Required]",
                            "[Display]"),

                        CreateQuestion(
                            "What does [ValidateAntiForgeryToken] protect against?",
                            1,
                            "SQL injection",
                            "Cross-site request forgery (CSRF)",
                            "Cross-site scripting (XSS)",
                            "Brute-force login"),

                        CreateQuestion(
                            "Which interface is used for logging in ASP.NET Core?",
                            3,
                            "ILog",
                            "IConsole",
                            "ITrace",
                            "ILogger<T>"),

                        CreateQuestion(
                            "Which HTTP status code means \"Not Found\"?",
                            0,
                            "404",
                            "400",
                            "500",
                            "302")
                    }
                }
            };

            /*
             * Adding the quizzes also adds their related questions
             * and answer options through the configured relationships.
             */
            context.Quizzes.AddRange(quizzes);

            /*
             * Save all demonstration content in a single database operation.
             */
            context.SaveChanges();
        }

        /*
        * Returns the ID of a required seeded module.
        * A clear exception is thrown if the database was not initialized correctly.
        */
        private static int GetRequiredModuleId(
            ApplicationDbContext context,
            string moduleName)
        {
            var module =
                context.Modules
                    .SingleOrDefault(
                        m => m.Name == moduleName);

            if (module == null)
            {
                throw new InvalidOperationException(
                    $"Required module '{moduleName}' was not found.");
            }

            return module.Id;
        }


        /*
         * Creates a question together with its answer options.
         * correctIndex is zero-based and identifies which option is correct.
         */
        private static Question CreateQuestion(
            string text,
            int correctIndex,
            params string[] options)
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