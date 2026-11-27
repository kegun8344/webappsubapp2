using api.DAL;
using api.Data;
using api.Models;
using api.Services;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Scalar.AspNetCore;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using System.Security.Claims;
using System.Text;
using api.Controllers;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddOpenApi();

/* The same database setup as Subapp 1, but in its own file (see appsettings.json) */
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlite(
        builder.Configuration.GetConnectionString("DefaultConnection")));

/*
 * Users and roles, same rules as Subapp 1.
 * AddIdentityCore instead of AddIdentity: an API logs in with tokens (JWT, added later), not cookies.
 */
builder.Services.AddIdentityCore<ApplicationUser>(options =>
{
    options.Password.RequiredLength = 8;
    options.User.RequireUniqueEmail = true;
    // Lock the account after 5 wrong passwords, to stop brute-force attacks.
    options.Lockout.MaxFailedAccessAttempts = 5;
    options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(5);
})
.AddRoles<IdentityRole>()
.AddEntityFrameworkStores<ApplicationDbContext>()
.AddSignInManager();

/*
 * Login with a JWT: the API gives the React app a signed token at login,
 * and the app sends it in the Authorization header on every request.
 * The key is only in appsettings.Development.json, so it must be set elsewhere in production.
 */
var jwtKey = builder.Configuration["Jwt:Key"]
    ?? throw new InvalidOperationException("Jwt:Key is missing from the configuration.");

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = builder.Configuration["Jwt:Issuer"],
            ValidAudience = builder.Configuration["Jwt:Audience"],
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey))
        };

        /* The React app sends the token in the HttpOnly cookie. An Authorization header still works too. */
        options.Events = new JwtBearerEvents
        {
            OnMessageReceived = context =>
            {
                if (string.IsNullOrEmpty(context.Request.Headers.Authorization))
                {
                    context.Token = context.Request.Cookies[AuthCookie.Name];
                }

                return Task.CompletedTask;
            },

            /*
             * A token lives for up to 2 hours, so it is checked against the database on every request,
             * the same as the security stamp check in Subapp 1. A deleted user, or a user whose role
             * was changed or who was locked by an administrator (new security stamp), is logged out.
             */
            OnTokenValidated = async context =>
            {
                var userManager = context.HttpContext.RequestServices.GetRequiredService<UserManager<ApplicationUser>>();
                var userId = context.Principal?.FindFirstValue(ClaimTypes.NameIdentifier);
                var stamp = context.Principal?.FindFirstValue(AuthController.SecurityStampClaim);

                var user = userId == null ? null : await userManager.FindByIdAsync(userId);

                if (user == null || string.IsNullOrEmpty(stamp) || stamp != user.SecurityStamp)
                {
                    context.Fail("The login is no longer valid.");
                }
            }
        };
    });

builder.Services.AddAuthorization();

/* Points and attempts. TimeProvider gives the current time, and can be faked in unit tests. */
builder.Services.AddSingleton(TimeProvider.System);
builder.Services.AddScoped<IPointService, PointService>();
builder.Services.AddScoped<IBadgeService, BadgeService>();

/* Data access layer: the same repositories as Subapp 1, so the controllers never use the database directly */
builder.Services.AddScoped<IQuizRepository, QuizRepository>();
builder.Services.AddScoped<IModuleRepository, ModuleRepository>();
builder.Services.AddScoped<ITagRepository, TagRepository>();
builder.Services.AddScoped<IReviewRepository, ReviewRepository>();
builder.Services.AddScoped<IQuestionRepository, QuestionRepository>();
builder.Services.AddScoped<IUserRepository, UserRepository>();
builder.Services.AddScoped<IPointRepository, PointRepository>();

/*
 * The React app runs on its own port (4000), so the browser needs permission
 * to call this API from there (CORS).
 */
builder.Services.AddCors(options =>
{
    options.AddPolicy("ReactClient", policy =>
        policy.WithOrigins("http://localhost:4000")
              .AllowAnyHeader()
              .AllowAnyMethod()
              .AllowCredentials());
});

var app = builder.Build();

/*
 * Create or update the database from the migrations when the API starts.
 * The .db file is not in git, so this runs for everyone (including the examiner).
 */
using (var scope = app.Services.CreateScope())
{
    var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
    context.Database.Migrate();

    /* The modules first, because the demo quizzes are placed in them */
    ModuleSeeder.Seed(context);

    /* Roles always, demo users only in Development (see IdentitySeeder) */
    await IdentitySeeder.SeedAsync(scope.ServiceProvider, builder.Configuration);

    /*
     * Demo quizzes and simulated activity are only added in Development,
     * because the demo accounts have known passwords. Everywhere else only the
     * shared tags and module icons are added.
     */
    if (!app.Environment.IsDevelopment())
    {
        DbSeeder.SeedTags(context);
        DbSeeder.SeedModuleIcons(context);
    }
    else
    {
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();

        var demoTeacher = await userManager.FindByEmailAsync("arne@teacher.no")
            ?? throw new InvalidOperationException("The demo teacher could not be found after Identity seeding.");

        DbSeeder.Seed(context, demoTeacher.Id);

        /* Extra quizzes and simulated student activity. A problem here must never stop the API from starting. */
        try
        {
            await DemoDataSeeder.SeedAsync(context, TimeProvider.System);
        }
        catch (Exception exception)
        {
            app.Logger.LogError(exception, "The demo data could not be added.");
        }
    }
}

if (app.Environment.IsDevelopment())
{
    /* API description and a test page at /scalar/v1 */
    app.MapOpenApi();
    app.MapScalarApiReference();
}

app.UseCors("ReactClient");
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();

app.Run();