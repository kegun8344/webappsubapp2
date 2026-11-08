namespace api.DTOs
{
    /* The signed-in user, as the React app sees it. Never contains the password hash. */
    public class UserDto
    {
        public string Id { get; set; } = "";

        public string Email { get; set; } = "";

        public List<string> Roles { get; set; } = new();
    }
}