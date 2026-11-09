using api.DAL;
using api.Services;

namespace api.DTOs
{
    /*
     * A module as the React app receives it from GET /api/modules.
     * Only what a module card needs, never the full entity with its quizzes.
     */
    public class ModuleDto
    {
        public int Id { get; set; }

        public string Name { get; set; } = string.Empty;

        public string? Description { get; set; }

        /* Bootstrap Icons class, e.g. "bi-database". Always a key from ModuleIcons.All. */
        public string IconKey { get; set; } = ModuleIcons.Default;

        /* The resolved colour 0-7 (module-color-0 to module-color-7), so the React app never has to work it out */
        public int ColorIndex { get; set; }

        /* Quizzes the user may see in this module, the same number as on the module cards in Subapp 1 */
        public int QuizCount { get; set; }

        /* Builds the DTO from the repository's module overview */
        public static ModuleDto FromOverview(ModuleOverview overview)
        {
            var module = overview.Module;

            return new ModuleDto
            {
                Id = module.Id,
                Name = module.Name,
                Description = module.Description,
                IconKey = ModuleIcons.Normalize(module.IconKey),
                ColorIndex = ModuleColors.Resolve(module.ColorIndex, module.Name),
                QuizCount = overview.VisibleQuizCount
            };
        }
    }
}