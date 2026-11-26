namespace api.Services
{
    /*
     * The cookie that holds the login token (JWT).
     * HttpOnly: JavaScript cannot read it, so an XSS attack cannot steal the token.
     * SameSite Strict: the browser never sends it with requests from other sites, which stops CSRF.
     */
    public static class AuthCookie
    {
        public const string Name = "studyquest_auth";

        /*
         * Secure (HTTPS only) when the request is HTTPS. In development the API runs on
         * plain http://localhost, where a Secure cookie would never be sent back.
         */
        public static CookieOptions Options(HttpRequest request, DateTimeOffset? expires = null)
        {
            return new CookieOptions
            {
                HttpOnly = true,
                SameSite = SameSiteMode.Strict,
                Secure = request.IsHttps,
                Path = "/",
                Expires = expires
            };
        }
    }
}