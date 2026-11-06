using api.Models;
using api.Services;

namespace api.Data
{
    /*
     * The seven ITPE3200 modules. In Subapp 1 they are added by a migration (AddModules),
     * here by this seeder, with the same names and descriptions.
     * Safe to run on every start: nothing is added if any module exists.
     */
    public static class ModuleSeeder
    {
        public static void Seed(ApplicationDbContext context)
        {
            if (context.Modules.Any())
            {
                return;
            }

            var modules = new (string Name, string Description)[]
            {
                ("ASP.NET MVC", "Model-View-Controller, Razor views, controllers and server-rendered web applications."),
                ("Entity Framework Core", "Database access, DbContext, LINQ, relationships and migrations."),
                ("Validation & Error Handling", "Validation, error handling, logging and resilient request processing."),
                ("Authentication & Authorization", "Identity, authentication, roles, authorization and access control."),
                ("Web API", "RESTful APIs, HTTP endpoints, DTOs and server-side API design."),
                ("React / SPA", "Single-page application development with React and API integration."),
                ("Exam Practice", "Mixed revision quizzes covering topics across ITPE3200.")
            };

            foreach (var (name, description) in modules)
            {
                context.Modules.Add(new Module
                {
                    Name = name,
                    Description = description,
                    IconKey = ModuleIcons.ForSeededModule(name) ?? ModuleIcons.Default
                });
            }

            context.SaveChanges();
        }
    }
}