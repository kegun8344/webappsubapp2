namespace api.Services
{
    /*
     * Rules for when an administrator may delete a user account.
     * Pure logic without database access, so the rules are easy to unit test
     * and are used by both the confirmation page and the delete action.
     */
    public static class UserDeletionRules
    {
        /*
         * Returns the reason why the account cannot be deleted,
         * or null when deletion is allowed.
         */
        public static string? GetBlockReason(
            string targetUserId,
            string? currentUserId,
            IEnumerable<string> targetRoles)
        {
            // An administrator cannot delete their own account.
            if (targetUserId == currentUserId)
            {
                return "You cannot delete your own account.";
            }

            // Administrators are never deleted, so the system always has one.
            if (targetRoles.Contains("Admin"))
            {
                return "Administrator accounts cannot be deleted.";
            }

            return null;
        }
    }
}