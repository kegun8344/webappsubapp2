using api.Models;
using Microsoft.AspNetCore.Identity;

namespace api.Data
{
    public static class IdentitySeeder
    {
        public static async Task SeedAsync(
            IServiceProvider services,
            IConfiguration configuration)
        {
            var roleManager =
                services.GetRequiredService<RoleManager<IdentityRole>>();

            var userManager =
                services.GetRequiredService<UserManager<ApplicationUser>>();

            // Create the application roles if they do not already exist.
            var roles = new[]
            {
                "Student",
                "Teacher",
                "Admin"
            };

            foreach (var role in roles)
            {
                if (!await roleManager.RoleExistsAsync(role))
                {
                    await roleManager.CreateAsync(
                        new IdentityRole(role));
                }
            }

            /* Demo accounts have known passwords, so they are only created in Development */
            var environment = services.GetRequiredService<IHostEnvironment>();
            if (!environment.IsDevelopment())
            {
                return;
            }

            // Create demo accounts used for development and examination.
            await EnsureUserAsync(
                userManager,
                "dilaj@student.no",
                "Student123!",
                "Student");

            await EnsureUserAsync(
                userManager,
                "arne@teacher.no",
                "Teacher123!",
                "Teacher");

            await EnsureUserAsync(
                userManager,
                "admin@studyquest.no",
                "Admin123!",
                "Admin");

            // More teachers and students, so the demo has a lively leaderboard.
            foreach (var email in DemoDataSeeder.ExtraTeacherEmails)
            {
                await EnsureUserAsync(userManager, email, "Teacher123!", "Teacher");
            }

            foreach (var email in DemoDataSeeder.ExtraStudentEmails)
            {
                await EnsureUserAsync(userManager, email, "Student123!", "Student");
            }
        }


        /*
         * Creates a demo user if it does not already exist
         * and ensures that the user has the expected role.
         */
        private static async Task EnsureUserAsync(
            UserManager<ApplicationUser> userManager,
            string email,
            string password,
            string role)
        {
            var user =
                await userManager.FindByEmailAsync(email);

            if (user == null)
            {
                user = new ApplicationUser
                {
                    UserName = email,
                    Email = email,
                    EmailConfirmed = true
                };

                var createResult =
                    await userManager.CreateAsync(
                        user,
                        password);

                if (!createResult.Succeeded)
                {
                    throw new InvalidOperationException(
                        $"Could not create demo user '{email}'.");
                }
            }

            // Ensure the account has the correct application role.
            if (!await userManager.IsInRoleAsync(user, role))
            {
                var roleResult =
                    await userManager.AddToRoleAsync(
                        user,
                        role);

                if (!roleResult.Succeeded)
                {
                    throw new InvalidOperationException(
                        $"Could not assign role '{role}' to demo user '{email}'.");
                }
            }
        }
    }
}