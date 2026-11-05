namespace api.Services
{
    /*
     * The icons a teacher can choose for a module (Bootstrap Icons class names).
     * A fixed list is used instead of free text, so a submitted value can never
     * inject anything into a CSS class, and every icon is known to exist.
     */
    public static class ModuleIcons
    {
        public const string Default = "bi-journal-bookmark";

        public static readonly IReadOnlyList<string> All = new[]
        {
            "bi-journal-bookmark", "bi-window", "bi-shield-check", "bi-database",
            "bi-shield-lock", "bi-diagram-3", "bi-braces", "bi-code-slash",
            "bi-layers", "bi-puzzle", "bi-bug", "bi-cpu",
            "bi-globe", "bi-server", "bi-terminal", "bi-git",
            "bi-lightning-charge", "bi-rocket-takeoff", "bi-calculator", "bi-book",
            "bi-journal-code", "bi-palette", "bi-bar-chart", "bi-translate",
            "bi-clipboard-data", "bi-gear", "bi-cloud", "bi-lock",
            "bi-people", "bi-lightbulb", "bi-trophy", "bi-pencil",
            "bi-mortarboard"
        };

        /* Icons for the modules that are created by the migration, matched on module name */
        private static readonly Dictionary<string, string> SeededModuleIcons = new()
        {
            ["ASP.NET MVC"] = "bi-window",
            ["Entity Framework Core"] = "bi-database",
            ["Validation & Error Handling"] = "bi-shield-check",
            ["Authentication & Authorization"] = "bi-shield-lock",
            ["Web API"] = "bi-globe",
            ["React / SPA"] = "bi-braces",
            ["Exam Practice"] = "bi-mortarboard"
        };

        public static bool IsValid(string? key)
        {
            return key != null && All.Contains(key);
        }

        /* A key that is not in the list falls back to the default icon */
        public static string Normalize(string? key)
        {
            return IsValid(key) ? key! : Default;
        }

        /* The icon a seeded module should have, null for modules the teachers created */
        public static string? ForSeededModule(string moduleName)
        {
            return SeededModuleIcons.TryGetValue(moduleName, out var key) ? key : null;
        }

        /* "bi-journal-bookmark" becomes "journal bookmark", used as the accessible name of the icon button */
        public static string Label(string key)
        {
            return key.StartsWith("bi-") ? key[3..].Replace('-', ' ') : key;
        }
    }
}