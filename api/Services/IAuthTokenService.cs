using api.DTOs;
using api.Models;

namespace api.Services
{
    /* Makes the login token (JWT) and puts it in the HttpOnly cookie. Used at login, sign up and after a password change. */
    public interface IAuthTokenService
    {
        Task<AuthResponseDto> SignInAsync(ApplicationUser user, HttpContext httpContext);
    }
}