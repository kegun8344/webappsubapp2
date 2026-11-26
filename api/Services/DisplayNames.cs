namespace api.Services
{
    /* How users are named on shared pages, without showing their email address to everyone */
    public static class DisplayNames
    {
        /* The number of avatar colours in the stylesheet (lb-avatar-0 to lb-avatar-7) */
        public const int AvatarColors = 8;

        /* "kevin@oslomet.no" becomes "kevin". Empty names become "Unknown". */
        public static string FromEmail(string? email)
        {
            if (string.IsNullOrWhiteSpace(email))
            {
                return "Unknown";
            }

            var name = email.Split('@')[0];

            return name.Length == 0 ? "Unknown" : name;
        }

        /* The same name always gets the same colour, 0-7 */
        public static int AvatarColor(string displayName)
        {
            return displayName.Sum(c => c) % AvatarColors;
        }
    }
}