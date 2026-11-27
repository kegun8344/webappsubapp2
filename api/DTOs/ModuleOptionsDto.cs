using api.Services;

namespace api.DTOs
{
    /*
     * The icons and colours a teacher can choose for a module.
     * Sent by the API so the React app always shows the same lists the server accepts.
     */
    public class ModuleOptionsDto
    {
        public List<IconOptionDto> Icons { get; set; } = new();

        public List<ColorOptionDto> Colors { get; set; } = new();

        public static ModuleOptionsDto Create()
        {
            return new ModuleOptionsDto
            {
                Icons = ModuleIcons.All
                    .Select(key => new IconOptionDto { Key = key, Label = ModuleIcons.Label(key) })
                    .ToList(),
                Colors = ModuleColors.Names
                    .Select((name, index) => new ColorOptionDto { Index = index, Name = name })
                    .ToList()
            };
        }
    }

    public class IconOptionDto
    {
        /* A Bootstrap Icons class, e.g. "bi-database" */
        public string Key { get; set; } = string.Empty;

        /* The name read by screen readers, e.g. "database" */
        public string Label { get; set; } = string.Empty;
    }

    public class ColorOptionDto
    {
        /* Used as module-color-{index} */
        public int Index { get; set; }

        public string Name { get; set; } = string.Empty;
    }
}