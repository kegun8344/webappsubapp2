using api.Data;
using api.Models;
using Microsoft.EntityFrameworkCore;

namespace api.DAL
{
    public class UserRepository : IUserRepository
    {
        private readonly ApplicationDbContext _db;
        private readonly ILogger<UserRepository> _logger;

        public UserRepository(ApplicationDbContext db, ILogger<UserRepository> logger)
        {
            _db = db;
            _logger = logger;
        }

        public async Task<List<UserOverview>?> GetOverviewAsync()
        {
            try
            {
                var users = await _db.Users
                    .AsNoTracking()
                    .OrderBy(user => user.Email)
                    .Select(user => new { user.Id, user.Email })
                    .ToListAsync();

                // Roles per user, from the Identity role tables.
                var roleRows = await (
                    from userRole in _db.UserRoles.AsNoTracking()
                    join identityRole in _db.Roles.AsNoTracking()
                        on userRole.RoleId equals identityRole.Id
                    select new { userRole.UserId, RoleName = identityRole.Name })
                    .ToListAsync();

                var rolesByUser = roleRows
                    .GroupBy(row => row.UserId)
                    .ToDictionary(
                        group => group.Key,
                        group => group
                            .Select(row => row.RoleName ?? string.Empty)
                            .OrderBy(name => name)
                            .ToList());

                // Quizzes created and published (public) per user.
                var quizCounts = await _db.Quizzes
                    .AsNoTracking()
                    .Where(quiz => quiz.CreatedByUserId != null)
                    .GroupBy(quiz => quiz.CreatedByUserId!)
                    .Select(group => new
                    {
                        UserId = group.Key,
                        Created = group.Count(),
                        Published = group.Count(quiz => quiz.IsPublic)
                    })
                    .ToDictionaryAsync(row => row.UserId);

                // Different quizzes each user has completed at least once.
                var completedCounts = await _db.QuizAttempts
                    .AsNoTracking()
                    .GroupBy(attempt => attempt.UserId)
                    .Select(group => new
                    {
                        UserId = group.Key,
                        Completed = group.Select(attempt => attempt.QuizId).Distinct().Count()
                    })
                    .ToDictionaryAsync(row => row.UserId, row => row.Completed);

                // Total points per user.
                var pointTotals = await _db.PointAwards
                    .AsNoTracking()
                    .GroupBy(award => award.UserId)
                    .Select(group => new { UserId = group.Key, Points = group.Sum(award => award.Points) })
                    .ToDictionaryAsync(row => row.UserId, row => row.Points);

                return users
                    .Select(user =>
                    {
                        quizCounts.TryGetValue(user.Id, out var created);
                        completedCounts.TryGetValue(user.Id, out var completed);
                        pointTotals.TryGetValue(user.Id, out var points);

                        return new UserOverview(
                            user.Id,
                            user.Email ?? string.Empty,
                            rolesByUser.TryGetValue(user.Id, out var roles) ? roles : new List<string>(),
                            created?.Created ?? 0,
                            created?.Published ?? 0,
                            completed,
                            points);
                    })
                    .ToList();
            }
            catch (Exception e)
            {
                _logger.LogError(e, "[UserRepository] Could not load the user overview.");
                return null;
            }
        }

        public async Task<ApplicationUser?> GetByIdAsync(string id)
        {
            try
            {
                return await _db.Users
                    .AsNoTracking()
                    .FirstOrDefaultAsync(u => u.Id == id);
            }
            catch (Exception e)
            {
                _logger.LogError(e, "[UserRepository] Could not load user ID {UserId}.", id);
                return null;
            }
        }

        public async Task<List<string>?> GetRolesAsync(string userId)
        {
            try
            {
                return await (
                    from userRole in _db.UserRoles.AsNoTracking()
                    join identityRole in _db.Roles.AsNoTracking()
                        on userRole.RoleId equals identityRole.Id
                    where userRole.UserId == userId
                    select identityRole.Name ?? string.Empty)
                    .ToListAsync();
            }
            catch (Exception e)
            {
                _logger.LogError(e, "[UserRepository] Could not load the roles of user {UserId}.", userId);
                return null;
            }
        }

        public async Task<UserDeletionCounts?> GetDeletionCountsAsync(string userId)
        {
            try
            {
                return new UserDeletionCounts(
                    await _db.Quizzes.CountAsync(q => q.CreatedByUserId == userId && !q.IsPublic),
                    await _db.Quizzes.CountAsync(q => q.CreatedByUserId == userId && q.IsPublic),
                    await _db.QuizAttempts.CountAsync(a => a.UserId == userId),
                    await _db.PointAwards.Where(a => a.UserId == userId).SumAsync(a => a.Points));
            }
            catch (Exception e)
            {
                _logger.LogError(e, "[UserRepository] Could not count the data of user {UserId}.", userId);
                return null;
            }
        }

        public async Task<int?> DeleteWithQuizzesAsync(string userId)
        {
            try
            {
                var user = await _db.Users.FirstOrDefaultAsync(u => u.Id == userId);
                if (user == null)
                {
                    _logger.LogWarning("[UserRepository] User ID {UserId} was not found when deleting.", userId);
                    return null;
                }

                // Private quizzes belong only to this user, so they are deleted too.
                var privateQuizzes = await _db.Quizzes
                    .Where(q => q.CreatedByUserId == userId && !q.IsPublic)
                    .ToListAsync();

                _db.Quizzes.RemoveRange(privateQuizzes);

                // Public quizzes stay available for other users, without a creator.
                var publicQuizzes = await _db.Quizzes
                    .Where(q => q.CreatedByUserId == userId && q.IsPublic)
                    .ToListAsync();

                foreach (var quiz in publicQuizzes)
                {
                    quiz.CreatedByUserId = null;
                }

                _db.Users.Remove(user);

                /* One SaveChanges, so either everything is deleted or nothing */
                await _db.SaveChangesAsync();

                return privateQuizzes.Count;
            }
            catch (Exception e)
            {
                _logger.LogError(e, "[UserRepository] Could not delete user ID {UserId}.", userId);
                return null;
            }
        }
    }
}