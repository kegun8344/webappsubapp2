using api.DTOs;
using api.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

namespace api.Controllers
{
    /*
     * Sign up, log in and "who am I" for the React app.
     * Same rules as AccountController in Subapp 1, but the answer is a JWT instead of a cookie.
     * There is no logout endpoint: the React app logs out by forgetting the token.
     */
    [ApiController]
    [Route("api/[controller]")]
    public class AuthController : ControllerBase
    {
        /* Everyone who signs up becomes a student. Only an administrator can give the Teacher role. */
        private const string DefaultRole = "Student";

        private readonly UserManager<ApplicationUser> userManager;
        private readonly SignInManager<ApplicationUser> signInManager;
        private readonly IConfiguration configuration;
        private readonly ILogger<AuthController> logger;

        public AuthController(
            UserManager<ApplicationUser> um,
            SignInManager<ApplicationUser> sm,
            IConfiguration config,
            ILogger<AuthController> log)
        {
            userManager = um;
            signInManager = sm;
            configuration = config;
            logger = log;
        }

        // POST: /api/auth/register
        [HttpPost("register")]
        [AllowAnonymous]
        public async Task<IActionResult> Register(RegisterDto dto)
        {
            var user = new ApplicationUser
            {
                UserName = dto.Email,
                Email = dto.Email
            };

            var result = await userManager.CreateAsync(user, dto.Password);

            if (!result.Succeeded)
            {
                /* E.g. the email is taken. Sent back the same way as other validation errors. */
                foreach (var error in result.Errors)
                {
                    ModelState.AddModelError(string.Empty, error.Description);
                }

                return ValidationProblem(ModelState);
            }

            // New accounts always get the Student role. The role is never read from the request.
            var roleResult = await userManager.AddToRoleAsync(user, DefaultRole);

            if (!roleResult.Succeeded)
            {
                // Avoid leaving an account without a valid application role.
                await userManager.DeleteAsync(user);
                logger.LogError("Could not give {Email} the role {Role}.", dto.Email, DefaultRole);

                return Problem("The account could not be created. Please try again.");
            }

            logger.LogInformation("User {Email} registered with role {Role}.", dto.Email, DefaultRole);

            /* Signed in straight away, same as in Subapp 1 */
            return Ok(await CreateResponseAsync(user));
        }

        // POST: /api/auth/login
        [HttpPost("login")]
        [AllowAnonymous]
        public async Task<IActionResult> Login(LoginDto dto)
        {
            var user = await userManager.FindByEmailAsync(dto.Email);

            if (user == null)
            {
                /* Same message as a wrong password, so nobody can find out which emails exist */
                logger.LogWarning("Login failed for {Email}.", dto.Email);
                return Problem(statusCode: StatusCodes.Status401Unauthorized, detail: "Invalid email or password.");
            }

            /* Checks the password and locks the account after 5 wrong tries, without making a cookie */
            var result = await signInManager.CheckPasswordSignInAsync(user, dto.Password, lockoutOnFailure: true);

            if (result.IsLockedOut)
            {
                logger.LogWarning("Login for {Email} refused: the account is locked.", dto.Email);
                return Problem(statusCode: StatusCodes.Status401Unauthorized, detail: "Account temporarily locked. Try again later.");
            }

            if (!result.Succeeded)
            {
                logger.LogWarning("Login failed for {Email}.", dto.Email);
                return Problem(statusCode: StatusCodes.Status401Unauthorized, detail: "Invalid email or password.");
            }

            logger.LogInformation("User {Email} logged in.", dto.Email);

            return Ok(await CreateResponseAsync(user));
        }

        // GET: /api/auth/me
        [HttpGet("me")]
        [Authorize]
        public async Task<IActionResult> Me()
        {
            /* The user id comes from the token, so a user can only ever ask about themselves */
            var user = await userManager.GetUserAsync(User);

            if (user == null)
            {
                /* The token is valid, but the account has been deleted since */
                return Unauthorized();
            }

            var roles = await userManager.GetRolesAsync(user);

            return Ok(ToUserDto(user, roles));
        }

        /* Makes a signed token with the user's id, email and roles */
        private async Task<AuthResponseDto> CreateResponseAsync(ApplicationUser user)
        {
            var roles = await userManager.GetRolesAsync(user);

            var claims = new List<Claim>
            {
                new(ClaimTypes.NameIdentifier, user.Id),
                new(ClaimTypes.Email, user.Email ?? ""),
                new(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
            };

            /* [Authorize(Roles = "Teacher")] reads these */
            claims.AddRange(roles.Select(role => new Claim(ClaimTypes.Role, role)));

            var expiresAt = DateTime.UtcNow.AddMinutes(configuration.GetValue("Jwt:ExpiresInMinutes", 120));
            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(configuration["Jwt:Key"]!));

            var token = new JwtSecurityToken(
                issuer: configuration["Jwt:Issuer"],
                audience: configuration["Jwt:Audience"],
                claims: claims,
                expires: expiresAt,
                signingCredentials: new SigningCredentials(key, SecurityAlgorithms.HmacSha256));

            return new AuthResponseDto
            {
                Token = new JwtSecurityTokenHandler().WriteToken(token),
                ExpiresAt = expiresAt,
                User = ToUserDto(user, roles)
            };
        }

        private static UserDto ToUserDto(ApplicationUser user, IList<string> roles)
        {
            return new UserDto
            {
                Id = user.Id,
                Email = user.Email ?? "",
                Roles = roles.ToList()
            };
        }
    }
}