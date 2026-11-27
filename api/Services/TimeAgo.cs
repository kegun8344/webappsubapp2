namespace api.Services
{
    /* "5 min ago", "2 h ago", "3 days ago". Pure logic, so it is easy to unit test. Same as Subapp 1. */
    public static class TimeAgo
    {
        public static string Format(DateTime thenUtc, DateTime nowUtc)
        {
            var span = nowUtc - thenUtc;

            /* Also covers a time slightly in the future, e.g. clocks that differ a little */
            if (span.TotalMinutes < 1) return "just now";
            if (span.TotalHours < 1) return $"{(int)span.TotalMinutes} min ago";
            if (span.TotalDays < 1) return $"{(int)span.TotalHours} h ago";
            if (span.TotalDays < 2) return "yesterday";

            return $"{(int)span.TotalDays} days ago";
        }
    }
}