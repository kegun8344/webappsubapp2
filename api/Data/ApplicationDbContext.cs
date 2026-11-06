using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using api.Models;

namespace api.Data
{
    // EF Core database context, the bridge between the C# models and the database.
    public class ApplicationDbContext : IdentityDbContext<ApplicationUser>
    {
        public ApplicationDbContext(
            DbContextOptions<ApplicationDbContext> options)
            : base(options) { }

        // Each DbSet maps to a table in the database.
        public DbSet<Quiz> Quizzes { get; set; }
        public DbSet<Question> Questions { get; set; }
        public DbSet<AnswerOption> AnswerOptions { get; set; }
        public DbSet<QuizAttempt> QuizAttempts { get; set; }
        public DbSet<PointAward> PointAwards { get; set; }
        public DbSet<UserAnswer> UserAnswers { get; set; }
        public DbSet<Tag> Tags { get; set; }
        public DbSet<Module> Modules { get; set; }
        public DbSet<ReviewDismissal> ReviewDismissals { get; set; }

        protected override void OnModelCreating(ModelBuilder builder)
        {
            // Required: configures the ASP.NET Core Identity tables.
            base.OnModelCreating(builder);

            /*
             * A module can contain many quizzes.
             * Every quiz must belong to exactly one module.
             * Restrict prevents a module from being deleted while
             * quizzes still belong to it.
             */
            builder.Entity<Quiz>()
                .HasOne(q => q.Module)
                .WithMany(m => m.Quizzes)
                .HasForeignKey(q => q.ModuleId)
                .OnDelete(DeleteBehavior.Restrict);

            /*
             * Module names are unique because they represent the shared ITPE3200 learning structure.
             */
            builder.Entity<Module>()
                .HasIndex(m => m.Name)
                .IsUnique();

            // If a user is deleted, their quizzes remain but lose their creator.
            builder.Entity<Quiz>()
                .HasOne(q => q.CreatedBy)
                .WithMany()
                .HasForeignKey(q => q.CreatedByUserId)
                .OnDelete(DeleteBehavior.SetNull);

            // Points remain even if the quiz they came from is deleted.
            builder.Entity<PointAward>()
                .HasOne(p => p.Quiz)
                .WithMany()
                .HasForeignKey(p => p.QuizId)
                .OnDelete(DeleteBehavior.SetNull);

            // Store the reason as readable text instead of an integer.
            builder.Entity<PointAward>()
                .Property(p => p.Reason)
                .HasConversion<string>()
                .HasMaxLength(30);

            // Supports point and daily-award queries efficiently.
            builder.Entity<PointAward>()
                .HasIndex(p => new { p.UserId, p.AwardedOn });

            /*
             * A personal tag is deleted together with its owner.
             * Shared tags have no owner and are therefore never deleted this way.
             */
            builder.Entity<Tag>()
                .HasOne(t => t.Owner)
                .WithMany()
                .HasForeignKey(t => t.OwnerUserId)
                .OnDelete(DeleteBehavior.Cascade);

            /*
             * A user cannot have two personal tags with the same name.
             * SQL treats NULL as different values, so uniqueness among shared tags
             * (no owner) is checked by TagController instead.
             */
            builder.Entity<Tag>()
                .HasIndex(t => new { t.OwnerUserId, t.Name })
                .IsUnique();

            /*
             * Stored answers are removed together with their user or their question.
             * The selected answer option is only an ID (no foreign key), so editing
             * a quiz never fails because of old answers.
             */
            builder.Entity<UserAnswer>()
                .HasOne(a => a.User)
                .WithMany()
                .HasForeignKey(a => a.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.Entity<UserAnswer>()
                .HasOne(a => a.Question)
                .WithMany()
                .HasForeignKey(a => a.QuestionId)
                .OnDelete(DeleteBehavior.Cascade);

            // Supports the review query: all answers of one user, per question.
            builder.Entity<UserAnswer>()
                .HasIndex(a => new { a.UserId, a.QuestionId });

            /* Many-to-many between quizzes and tags. EF Core creates the join table QuizTags. */
            builder.Entity<Quiz>()
                .HasMany(q => q.Tags)
                .WithMany(t => t.Quizzes)
                .UsingEntity(j => j.ToTable("QuizTags"));

            /* A cleared review is removed together with its user or its quiz */
            builder.Entity<ReviewDismissal>()
                .HasOne(d => d.User)
                .WithMany()
                .HasForeignKey(d => d.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.Entity<ReviewDismissal>()
                .HasOne(d => d.Quiz)
                .WithMany()
                .HasForeignKey(d => d.QuizId)
                .OnDelete(DeleteBehavior.Cascade);

            // Supports the review query: the clear times of one user, per quiz.
            builder.Entity<ReviewDismissal>()
                .HasIndex(d => new { d.UserId, d.QuizId });
        }
    }
}