using api.DAL;
using api.Data;
using api.Models;
using api.Services;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Scalar.AspNetCore;

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
.AddEntityFrameworkStores<ApplicationDbContext>();

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
              .AllowAnyMethod());
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
}

if (app.Environment.IsDevelopment())
{
    /* API description and a test page at /scalar/v1 */
    app.MapOpenApi();
    app.MapScalarApiReference();
}

app.UseCors("ReactClient");
app.UseAuthorization();
app.MapControllers();

app.Run();