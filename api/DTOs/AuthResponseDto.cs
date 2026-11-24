namespace api.DTOs
{
    /* The answer after a successful login. The token itself is in the HttpOnly cookie, not here. */
    public class AuthResponseDto
    {
        public DateTime ExpiresAt { get; set; }

        public UserDto User { get; set; } = new();
    }
}