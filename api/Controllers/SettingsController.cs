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
    /* The signed-in user's own settings. The user always comes from the token, never from the request. */
    [ApiController]
    [Route("api/settings")]
    [Authorize]
    public class SettingsController : ControllerBase
    {
        private readonly ISettingsRepository settingsRepository;
        private readonly IUserRepository userRepository;
        private readonly UserManager<ApplicationUser> userManager;
        private readonly IAuthTokenService authTokenService;
        private readonly ILogger<SettingsController> logger;

        /* Administrator accounts are protected: their password cannot be changed and they cannot be deleted here */
        private const string AdminRole = "Admin";

        public SettingsController(ISettingsRepository settingsRepo, IUserRepository userRepo,
            UserManager<ApplicationUser> users, IAuthTokenService tokens, ILogger<SettingsController> settingsLogger)
        {
            settingsRepository = settingsRepo;
            userRepository = userRepo;
            userManager = users;
            authTokenService = tokens;
            logger = settingsLogger;
        }

        // GET: /api/settings
        [HttpGet]
        public async Task<ActionResult<SettingsDto>> Get()
        {
            var settings = await settingsRepository.GetUserSettingsAsync(CurrentUserId!);
            if (settings == null)
            {
                return Problem("Your settings could not be loaded.", statusCode: StatusCodes.Status500InternalServerError);
            }
            return SettingsDto.FromSettings(settings);
        }

        // PUT: /api/settings
        [HttpPut]
        public async Task<ActionResult<SettingsDto>> Update(SettingsDto dto)
        {
            var settings = new UserSettings
            {
                UserId = CurrentUserId!,
                DarkModeEnabled = dto.DarkModeEnabled,
                ReduceMotion = dto.ReduceMotion,
                ColorVisionMode = dto.ColorVisionMode
            };

            if (!await settingsRepository.UpdateUserSettingsAsync(settings))
            {
                return Problem("Your settings could not be saved. Please try again.", statusCode: StatusCodes.Status500InternalServerError);
            }

            logger.LogInformation("User {UserId} updated settings: dark mode {DarkMode}, reduced motion {ReduceMotion}, colour vision {ColorVision}.",
                settings.UserId, settings.DarkModeEnabled, settings.ReduceMotion, settings.ColorVisionMode);

            return SettingsDto.FromSettings(settings);
        }

        // POST: /api/settings/change-password
        [HttpPost("change-password")]
        public async Task<IActionResult> ChangePassword(ChangePasswordDto dto)
        {
            var user = await userManager.GetUserAsync(User);
            if (user == null) return Unauthorized();

            /* Checked on the server too, so the protection does not depend on React hiding the form */
            if (await userManager.IsInRoleAsync(user, AdminRole))
            {
                logger.LogWarning("Administrator {UserId} tried to change the password, which is not allowed.", user.Id);
                return Problem("Administrator passwords cannot be changed here.", statusCode: StatusCodes.Status403Forbidden);
            }

            var result = await userManager.ChangePasswordAsync(user, dto.CurrentPassword, dto.NewPassword);
            if (!result.Succeeded)
            {
                foreach (var error in result.Errors)
                {
                    ModelState.AddModelError(string.Empty, error.Description);
                }
                /* Only the error codes are logged, never the passwords */
                logger.LogWarning("Password change for user {UserId} failed: {Errors}",
                    user.Id, string.Join(", ", result.Errors.Select(e => e.Code)));
                return ValidationProblem(ModelState);
            }

            /*
             * Changing the password gives the user a new security stamp, and Program.cs refuses tokens with the old one.
             * A new token keeps the user signed in here, while other devices are logged out (same as RefreshSignInAsync in Subapp 1).
             */
            await authTokenService.SignInAsync(user, HttpContext);

            logger.LogInformation("User {UserId} changed their password.", user.Id);
            return NoContent();
        }

        // GET: /api/settings/delete-info
        [HttpGet("delete-info")]
        public async Task<ActionResult<DeleteAccountInfoDto>> GetDeleteInfo()
        {
            var info = await BuildDeleteInfoAsync(CurrentUserId!);
            if (info == null)
            {
                return Problem("Your account information could not be loaded.", statusCode: StatusCodes.Status500InternalServerError);
            }
            return info;
        }

        // POST: /api/settings/delete-account (POST, since a DELETE with a password in the body is unusual)
        [HttpPost("delete-account")]
        public async Task<IActionResult> DeleteAccount(DeleteAccountDto dto)
        {
            var user = await userManager.GetUserAsync(User);
            if (user == null) return Unauthorized();

            var info = await BuildDeleteInfoAsync(user.Id);
            if (info == null)
            {
                return Problem("Your account could not be deleted. Please try again.", statusCode: StatusCodes.Status500InternalServerError);
            }

            if (info.BlockReason != null)
            {
                logger.LogWarning("User {UserId} was not allowed to delete their account: {Reason}", user.Id, info.BlockReason);
                return Problem(info.BlockReason, statusCode: StatusCodes.Status403Forbidden);
            }

            if (!await userManager.CheckPasswordAsync(user, dto.Password))
            {
                logger.LogWarning("Account deletion for user {UserId} rejected because the password was wrong.", user.Id);
                ModelState.AddModelError(nameof(DeleteAccountDto.Password), "The password is not correct.");
                return ValidationProblem(ModelState);
            }

            var deletedQuizzes = await userRepository.DeleteWithQuizzesAsync(user.Id);
            if (deletedQuizzes == null)
            {
                return Problem("Your account could not be deleted. Please try again.", statusCode: StatusCodes.Status500InternalServerError);
            }

            /* Same as logout: JavaScript cannot remove the HttpOnly cookie */
            Response.Cookies.Delete(AuthCookie.Name, AuthCookie.Options(Request));

            logger.LogInformation("User {UserId} deleted their own account. {QuizCount} private quizzes were deleted.",
                user.Id, deletedQuizzes);
            return NoContent();
        }

        /* Administrators may not delete themselves, so the system always keeps one (same rule as Subapp 1) */
        private async Task<DeleteAccountInfoDto?> BuildDeleteInfoAsync(string userId)
        {
            var roles = await userRepository.GetRolesAsync(userId);
            var counts = await userRepository.GetDeletionCountsAsync(userId);
            if (roles == null || counts == null) return null;

            return new DeleteAccountInfoDto
            {
                BlockReason = roles.Contains(AdminRole) ? "Administrator accounts cannot be deleted." : null,
                PrivateQuizzes = counts.PrivateQuizzes,
                PublicQuizzes = counts.PublicQuizzes,
                Attempts = counts.Attempts,
                Points = counts.Points
            };
        }

        private string? CurrentUserId => User.FindFirstValue(ClaimTypes.NameIdentifier);
    }
}