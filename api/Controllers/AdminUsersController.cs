using System.Security.Claims;
using api.DAL;
using api.DTOs;
using api.Models;
using api.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace api.Controllers
{
    /*
     * The administrator's Users page, the same rules as AdminUsersController and
     * AdminDashboardController in Subapp 1: the list with search and role filter,
     * make teacher / make student, lock / unlock, and delete.
     * Administrators are never listed, changed, locked or deleted here.
     * After a role change or a lock the user gets a new security stamp, so their token stops working (see Program.cs).
     */
    [ApiController]
    [Route("api/admin/users")]
    [Authorize(Roles = "Admin")]
    public class AdminUsersController : ControllerBase
    {
        private const string AdminRole = "Admin";
        private const string TeacherRole = "Teacher";
        private const string StudentRole = "Student";

        /* The roles that can be used as a filter */
        private static readonly string[] FilterRoles = { StudentRole, TeacherRole };

        private readonly IUserRepository userRepository;
        private readonly UserManager<ApplicationUser> userManager;
        private readonly ILogger<AdminUsersController> logger;

        public AdminUsersController(
            IUserRepository userRepo,
            UserManager<ApplicationUser> um,
            ILogger<AdminUsersController> usersLogger)
        {
            userRepository = userRepo;
            userManager = um;
            logger = usersLogger;
        }

        // GET: /api/admin/users?role=Teacher&search=sofie
        [HttpGet]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<ActionResult<List<AdminUserDto>>> GetAll(string? role = null, string? search = null)
        {
            /* Only known roles are accepted as a filter. Any other value means "all users". */
            var selectedRole = FilterRoles.FirstOrDefault(
                filterRole => string.Equals(filterRole, role, StringComparison.OrdinalIgnoreCase));

            var searchText = search?.Trim();

            var users = await userRepository.GetOverviewAsync();

            if (users == null)
            {
                /* The repository has already logged the database error */
                return Problem("The users could not be loaded. Please try again.", statusCode: StatusCodes.Status500InternalServerError);
            }

            var rows = users
                /* Administrators cannot be changed or deleted here, so they are not listed */
                .Where(user => !user.Roles.Contains(AdminRole))
                .Where(user => selectedRole == null || user.Roles.Contains(selectedRole))
                .Where(user => string.IsNullOrEmpty(searchText)
                    || user.Email.Contains(searchText, StringComparison.OrdinalIgnoreCase))
                .Select(user => new AdminUserDto
                {
                    Id = user.Id,
                    Email = user.Email,
                    Roles = user.Roles,
                    QuizzesCreated = user.QuizzesCreated,
                    QuizzesPublished = user.QuizzesPublished,
                    QuizzesCompleted = user.QuizzesCompleted,
                    TotalPoints = user.TotalPoints,
                    Level = LevelCalculator.GetLevel(Math.Max(0, user.TotalPoints)).Level,
                    IsLocked = user.IsLocked,
                    CanDelete = UserDeletionRules.GetBlockReason(user.Id, CurrentUserId, user.Roles) == null
                })
                .OrderBy(row => string.Join(", ", row.Roles))
                .ThenBy(row => row.Email)
                .ToList();

            logger.LogInformation(
                "Administrator opened the user overview with role filter '{Role}' and {Count} users.",
                selectedRole ?? "all",
                rows.Count);

            return rows;
        }

        // GET: /api/admin/users/abc/deletion - what is lost if the user is deleted
        [HttpGet("{id}/deletion")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<ActionResult<UserDeletionPreviewDto>> GetDeletionPreview(string id)
        {
            var user = await userRepository.GetByIdAsync(id);

            if (user == null)
            {
                return NotFound();
            }

            var roles = await userRepository.GetRolesAsync(user.Id);
            var counts = await userRepository.GetDeletionCountsAsync(user.Id);

            if (roles == null || counts == null)
            {
                return Problem("The user could not be loaded. Please try again.", statusCode: StatusCodes.Status500InternalServerError);
            }

            /* Show the reason instead of the confirmation when deletion is not allowed */
            var blockReason = UserDeletionRules.GetBlockReason(user.Id, CurrentUserId, roles);

            if (blockReason != null)
            {
                return Problem(blockReason, statusCode: StatusCodes.Status400BadRequest);
            }

            return new UserDeletionPreviewDto
            {
                Id = user.Id,
                Email = user.Email ?? string.Empty,
                Roles = roles,
                PrivateQuizzes = counts.PrivateQuizzes,
                PublicQuizzes = counts.PublicQuizzes,
                Attempts = counts.Attempts,
                Points = counts.Points
            };
        }

        /*
         * DELETE: /api/admin/users/abc
         * The user's private quizzes are deleted. Public quizzes stay, but lose their creator.
         * Attempts, points and personal tags are deleted together with the user by the database (cascade).
         */
        [HttpDelete("{id}")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> Delete(string id)
        {
            var user = await userRepository.GetByIdAsync(id);

            if (user == null)
            {
                logger.LogWarning("User ID {UserId} could not be deleted because it was not found.", id);
                return NotFound();
            }

            var roles = await userRepository.GetRolesAsync(user.Id);

            if (roles == null)
            {
                return Problem("The user could not be deleted. Please try again.", statusCode: StatusCodes.Status500InternalServerError);
            }

            /* The rules are checked again here, since the request can be sent by hand */
            var blockReason = UserDeletionRules.GetBlockReason(user.Id, CurrentUserId, roles);

            if (blockReason != null)
            {
                logger.LogWarning(
                    "Administrator {AdminId} was not allowed to delete user ID {UserId}: {Reason}",
                    CurrentUserId,
                    user.Id,
                    blockReason);

                return Problem(blockReason, statusCode: StatusCodes.Status400BadRequest);
            }

            var deletedPrivateQuizzes = await userRepository.DeleteWithQuizzesAsync(user.Id);

            if (deletedPrivateQuizzes == null)
            {
                /* The repository has already logged the database error */
                return Problem("The user could not be deleted. Please try again.", statusCode: StatusCodes.Status500InternalServerError);
            }

            logger.LogInformation(
                "Administrator {AdminId} deleted user ID {UserId} ({Email}), including {PrivateCount} private quizzes.",
                CurrentUserId,
                user.Id,
                user.Email,
                deletedPrivateQuizzes);

            return NoContent();
        }

        // POST: /api/admin/users/abc/make-teacher
        [HttpPost("{id}/make-teacher")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> MakeTeacher(string id)
        {
            var user = await userManager.FindByIdAsync(id);

            if (user == null)
            {
                logger.LogWarning("Make teacher failed because user ID {UserId} was not found.", id);
                return NotFound();
            }

            /* Administrators already have every right, and should not be changed here */
            if (await userManager.IsInRoleAsync(user, AdminRole))
            {
                return Problem("Administrators cannot be changed here.", statusCode: StatusCodes.Status400BadRequest);
            }

            if (await userManager.IsInRoleAsync(user, TeacherRole))
            {
                return Problem($"{user.Email} is already a teacher.", statusCode: StatusCodes.Status400BadRequest);
            }

            var addResult = await userManager.AddToRoleAsync(user, TeacherRole);

            if (!addResult.Succeeded)
            {
                logger.LogError(
                    "Could not give user {Email} the Teacher role: {Errors}",
                    user.Email,
                    string.Join(", ", addResult.Errors.Select(e => e.Description)));

                return Problem("The role could not be changed. Please try again.", statusCode: StatusCodes.Status500InternalServerError);
            }

            /* A user is either a student or a teacher, so the Student role is removed */
            if (await userManager.IsInRoleAsync(user, StudentRole))
            {
                var removeResult = await userManager.RemoveFromRoleAsync(user, StudentRole);

                if (!removeResult.Succeeded)
                {
                    logger.LogWarning("User {Email} became a teacher but still has the Student role.", user.Email);
                }
            }

            /* New security stamp: the user's token stops working on the next request (see Program.cs) */
            await userManager.UpdateSecurityStampAsync(user);

            logger.LogInformation("Administrator {AdminId} gave user {Email} the Teacher role.", CurrentUserId, user.Email);

            return NoContent();
        }

        // POST: /api/admin/users/abc/remove-teacher - the user becomes a student again
        [HttpPost("{id}/remove-teacher")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> RemoveTeacher(string id)
        {
            var user = await userManager.FindByIdAsync(id);

            if (user == null)
            {
                logger.LogWarning("Remove teacher failed because user ID {UserId} was not found.", id);
                return NotFound();
            }

            if (await userManager.IsInRoleAsync(user, AdminRole))
            {
                return Problem("Administrators cannot be changed here.", statusCode: StatusCodes.Status400BadRequest);
            }

            if (!await userManager.IsInRoleAsync(user, TeacherRole))
            {
                return Problem($"{user.Email} is not a teacher.", statusCode: StatusCodes.Status400BadRequest);
            }

            /* Give the Student role first, so the user is never left without a role */
            if (!await userManager.IsInRoleAsync(user, StudentRole))
            {
                var addResult = await userManager.AddToRoleAsync(user, StudentRole);

                if (!addResult.Succeeded)
                {
                    logger.LogError(
                        "Could not give user {Email} the Student role: {Errors}",
                        user.Email,
                        string.Join(", ", addResult.Errors.Select(e => e.Description)));

                    return Problem("The role could not be changed. Please try again.", statusCode: StatusCodes.Status500InternalServerError);
                }
            }

            var removeResult = await userManager.RemoveFromRoleAsync(user, TeacherRole);

            if (!removeResult.Succeeded)
            {
                logger.LogError(
                    "Could not remove the Teacher role from user {Email}: {Errors}",
                    user.Email,
                    string.Join(", ", removeResult.Errors.Select(e => e.Description)));

                return Problem("The role could not be changed. Please try again.", statusCode: StatusCodes.Status500InternalServerError);
            }

            await userManager.UpdateSecurityStampAsync(user);

            logger.LogInformation("Administrator {AdminId} removed the Teacher role from user {Email}.", CurrentUserId, user.Email);

            return NoContent();
        }

        // POST: /api/admin/users/abc/lock - the user cannot log in until an administrator unlocks the account
        [HttpPost("{id}/lock")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> Lock(string id)
        {
            var user = await userManager.FindByIdAsync(id);

            if (user == null)
            {
                logger.LogWarning("Lock failed because user ID {UserId} was not found.", id);
                return NotFound();
            }

            if (await userManager.IsInRoleAsync(user, AdminRole))
            {
                return Problem("Administrators cannot be locked.", statusCode: StatusCodes.Status400BadRequest);
            }

            /* A lockout end far in the future means "until an administrator unlocks it" */
            await userManager.SetLockoutEnabledAsync(user, true);
            var result = await userManager.SetLockoutEndDateAsync(user, DateTimeOffset.MaxValue);

            if (!result.Succeeded)
            {
                logger.LogError(
                    "Could not lock user {Email}: {Errors}",
                    user.Email,
                    string.Join(", ", result.Errors.Select(e => e.Description)));

                return Problem("The account could not be locked. Please try again.", statusCode: StatusCodes.Status500InternalServerError);
            }

            /* New security stamp: a signed-in user is logged out on their next request */
            await userManager.UpdateSecurityStampAsync(user);

            logger.LogInformation("Administrator {AdminId} locked user {Email}.", CurrentUserId, user.Email);

            return NoContent();
        }

        // POST: /api/admin/users/abc/unlock - also after too many wrong passwords
        [HttpPost("{id}/unlock")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> Unlock(string id)
        {
            var user = await userManager.FindByIdAsync(id);

            if (user == null)
            {
                logger.LogWarning("Unlock failed because user ID {UserId} was not found.", id);
                return NotFound();
            }

            var result = await userManager.SetLockoutEndDateAsync(user, null);

            if (!result.Succeeded)
            {
                logger.LogError(
                    "Could not unlock user {Email}: {Errors}",
                    user.Email,
                    string.Join(", ", result.Errors.Select(e => e.Description)));

                return Problem("The account could not be unlocked. Please try again.", statusCode: StatusCodes.Status500InternalServerError);
            }

            /* Start the count of wrong passwords from zero again */
            await userManager.ResetAccessFailedCountAsync(user);

            logger.LogInformation("Administrator {AdminId} unlocked user {Email}.", CurrentUserId, user.Email);

            return NoContent();
        }

        /* The ID of the signed-in administrator */
        private string? CurrentUserId => User.FindFirstValue(ClaimTypes.NameIdentifier);
    }
}