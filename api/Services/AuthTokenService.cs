using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using api.DTOs;
using api.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.IdentityModel.Tokens;

namespace api.Services
{
    /*
     * Makes a signed token with the user's id, email, roles and security stamp, and sends it in the HttpOnly cookie.
     * The token itself is never in the answer, so JavaScript never sees it.
     */
    public class AuthTokenService : IAuthTokenService
    {
        /*
         * The user's security stamp at login. Identity changes the stamp when the password or role changes
         * or the account is locked, and Program.cs then refuses the old token.
         */
        public const string SecurityStampClaim = "security_stamp";

        private readonly UserManager<ApplicationUser> userManager;
        private readonly IConfiguration configuration;

        public AuthTokenService(UserManager<ApplicationUser> um, IConfiguration config)
        {
            userManager = um;
            configuration = config;
        }

        public async Task<AuthResponseDto> SignInAsync(ApplicationUser user, HttpContext httpContext)
        {
            var roles = await userManager.GetRolesAsync(user);

            var claims = new List<Claim>
            {
                new(ClaimTypes.NameIdentifier, user.Id),
                new(ClaimTypes.Email, user.Email ?? ""),
                new(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
                new(SecurityStampClaim, user.SecurityStamp ?? "")
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

            httpContext.Response.Cookies.Append(
                AuthCookie.Name,
                new JwtSecurityTokenHandler().WriteToken(token),
                AuthCookie.Options(httpContext.Request, expiresAt));

            return new AuthResponseDto
            {
                ExpiresAt = expiresAt,
                User = ToUserDto(user, roles)
            };
        }

        public static UserDto ToUserDto(ApplicationUser user, IList<string> roles)
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