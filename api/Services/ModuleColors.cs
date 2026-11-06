namespace api.Services
{
    /*
     * The colours a teacher can choose for a module.
     * They match module-color-0 to module-color-7 in site.css, so only the number is stored.
     */
    public static class ModuleColors
    {
        public static readonly IReadOnlyList<string> Names = new[]
        {
            "Blue", "Green", "Pink", "Purple", "Orange", "Teal", "Grey", "Magenta"
        };

        public static int Count => Names.Count;

        public static bool IsValid(int? colorIndex)
        {
            return colorIndex.HasValue && colorIndex.Value >= 0 && colorIndex.Value < Count;
        }

        /* The colour a module had before colours could be chosen, worked out from its name */
        public static int FromName(string name)
        {
            return name.Sum(c => c) % Count;
        }

        /* The chosen colour, or the one from the name if none was chosen */
        public static int Resolve(int? colorIndex, string name)
        {
            return IsValid(colorIndex) ? colorIndex!.Value : FromName(name);
        }
    }
}