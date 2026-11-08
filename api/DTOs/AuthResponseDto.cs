namespace api.DTOs
{
    /* The answer after a successful login: the token to send on every request, and who is signed in */
    public class AuthResponseDto
    {
        public string Token { get; set; } = "";

        public DateTime ExpiresAt { get; set; }

        public UserDto User { get; set; } = new();
    }
}