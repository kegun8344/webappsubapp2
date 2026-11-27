using System.Security.Claims;
using api.DAL;
using api.DTOs;
using api.Models;
using api.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace api.Controllers
{
    /*
     * The ITPE3200 modules: the module cards with the signed-in user's progress, and
     * create, edit and delete for teachers and administrators. Same rules as ModuleController in Subapp 1.
     * The controller only uses IModuleRepository, never the database directly.
     */
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class ModulesController : ControllerBase
    {
        private const string ManagerRoles = "Teacher,Admin";

        private readonly IModuleRepository moduleRepository;
        private readonly ILogger<ModulesController> logger;

        public ModulesController(
            IModuleRepository modules,
            ILogger<ModulesController> modulesLogger)
        {
            moduleRepository = modules;
            logger = modulesLogger;
        }

        // GET: /api/modules
        // All modules, sorted by name, with the quiz numbers and progress for the signed-in user.
        [HttpGet]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<ActionResult<List<ModuleDto>>> GetAll()
        {
            var overview = await LoadOverviewAsync();

            if (overview == null)
            {
                return Problem("The modules could not be loaded.", statusCode: StatusCodes.Status500InternalServerError);
            }

            return overview.Select(ModuleDto.FromOverview).ToList();
        }

        // GET: /api/modules/summary
        // The student's progress over all modules, for the "learning journey" at the top of the page.
        [HttpGet("summary")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<ActionResult<ModuleSummaryDto>> GetSummary()
        {
            var overview = await LoadOverviewAsync();

            if (overview == null)
            {
                return Problem("The module progress could not be loaded.", statusCode: StatusCodes.Status500InternalServerError);
            }

            var cards = overview.Select(ModuleDto.FromOverview).ToList();

            var totalOfficial = cards.Sum(card => card.OfficialQuizCount);
            var totalCompleted = cards.Sum(card => card.CompletedOfficialQuizCount);
            var progressPercent = PointRules.ModuleProgressPercent(totalCompleted, totalOfficial);

            /* Weighted by the number of quizzes taken in each module, the same as Subapp 1 */
            var taken = cards.Where(card => card.AverageBestScore.HasValue && card.CompletedOfficialQuizCount > 0).ToList();
            var takenCount = taken.Sum(card => card.CompletedOfficialQuizCount);
            int? averageScore = takenCount == 0
                ? null
                : (int)Math.Round(taken.Sum(card => card.AverageBestScore!.Value * card.CompletedOfficialQuizCount) / (double)takenCount);

            /* A module already started first, then the one furthest along */
            var continueWith = cards
                .Where(card => card.NextQuizId.HasValue)
                .OrderByDescending(card => card.CompletedOfficialQuizCount > 0)
                .ThenByDescending(card => card.ProgressPercent)
                .FirstOrDefault();

            return new ModuleSummaryDto
            {
                DisplayName = DisplayNames.FromEmail(User.FindFirstValue(ClaimTypes.Email)),
                TotalOfficialQuizzes = totalOfficial,
                TotalCompletedQuizzes = totalCompleted,
                ProgressPercent = progressPercent,
                ModulesWithQuizzes = cards.Count(card => card.OfficialQuizCount > 0),
                ModulesCompleted = cards.Count(card => card.IsCompleted),
                AverageScore = averageScore,
                Message = ModuleRules.JourneyMessage(progressPercent),
                ContinueQuizId = continueWith?.NextQuizId,
                ContinueQuizTitle = continueWith?.NextQuizTitle,
                ContinueModuleName = continueWith?.Name
            };
        }

        // GET: /api/modules/options
        // The icons and colours a module can have, for the module form.
        [HttpGet("options")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        public ActionResult<ModuleOptionsDto> GetOptions()
        {
            return ModuleOptionsDto.Create();
        }

        // GET: /api/modules/5
        [HttpGet("{id:int}")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<ActionResult<ModuleDto>> GetById(int id)
        {
            /*
             * Uses the same overview as the list, so the quiz count is always the same in both places.
             * It also tells a database error (500) apart from a module that does not exist (404).
             */
            var overview = await LoadOverviewAsync();

            if (overview == null)
            {
                return Problem("The module could not be loaded.", statusCode: StatusCodes.Status500InternalServerError);
            }

            var module = overview.FirstOrDefault(item => item.Module.Id == id);

            if (module == null)
            {
                logger.LogWarning("Module ID {ModuleId} was requested, but it was not found.", id);
                return NotFound();
            }

            return ModuleDto.FromOverview(module);
        }

        // POST: /api/modules
        [HttpPost]
        [Authorize(Roles = ManagerRoles)]
        [ProducesResponseType(StatusCodes.Status201Created)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<ActionResult<ModuleDto>> Create(ModuleFormDto dto)
        {
            var name = dto.Name.Trim();

            await ValidateFormAsync(dto, name, excludeId: null);

            if (!ModelState.IsValid)
            {
                logger.LogWarning("Module creation failed because the submitted form was invalid.");
                return ValidationProblem(ModelState);
            }

            var module = new Module
            {
                Name = name,
                Description = string.IsNullOrWhiteSpace(dto.Description) ? null : dto.Description.Trim(),
                IconKey = dto.IconKey,
                ColorIndex = dto.ColorIndex
            };

            if (!await moduleRepository.AddAsync(module))
            {
                /* The repository has already logged the database error */
                return Problem("The module could not be created. Please try again.", statusCode: StatusCodes.Status500InternalServerError);
            }

            logger.LogInformation("Module ID {ModuleId} with name '{ModuleName}' was created.", module.Id, module.Name);

            /* A new module has no quizzes and no progress yet */
            return CreatedAtAction(nameof(GetById), new { id = module.Id }, ModuleDto.FromOverview(new ModuleOverview(module, 0, 0, 0)));
        }

        // PUT: /api/modules/5
        [HttpPut("{id:int}")]
        [Authorize(Roles = ManagerRoles)]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> Update(int id, ModuleFormDto dto)
        {
            var module = await moduleRepository.GetForUpdateAsync(id);

            if (module == null)
            {
                logger.LogWarning("Module ID {ModuleId} was not found during update.", id);
                return NotFound();
            }

            var name = dto.Name.Trim();

            /* Another module may not already use the name */
            await ValidateFormAsync(dto, name, excludeId: id);

            if (!ModelState.IsValid)
            {
                logger.LogWarning("Module ID {ModuleId} could not be updated because the submitted form was invalid.", id);
                return ValidationProblem(ModelState);
            }

            module.Name = name;
            module.Description = string.IsNullOrWhiteSpace(dto.Description) ? null : dto.Description.Trim();
            module.IconKey = dto.IconKey;
            module.ColorIndex = dto.ColorIndex;

            if (!await moduleRepository.UpdateAsync(module))
            {
                return Problem("The module could not be updated. Please try again.", statusCode: StatusCodes.Status500InternalServerError);
            }

            logger.LogInformation("Module ID {ModuleId} was updated to '{ModuleName}'.", module.Id, module.Name);

            return NoContent();
        }

        // DELETE: /api/modules/5
        [HttpDelete("{id:int}")]
        [Authorize(Roles = ManagerRoles)]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status409Conflict)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> Delete(int id)
        {
            var module = await moduleRepository.GetForUpdateAsync(id);

            if (module == null)
            {
                logger.LogWarning("Delete requested for module ID {ModuleId}, but the module was not found.", id);
                return NotFound();
            }

            /*
             * Domain rule: a module that contains quizzes cannot be deleted.
             * The quizzes must first be moved to another module or deleted.
             */
            if (await moduleRepository.HasQuizzesAsync(id))
            {
                logger.LogWarning("Module ID {ModuleId} could not be deleted because it still contains quizzes.", id);
                return Problem(
                    "This module cannot be deleted while it contains quizzes. Move or delete the quizzes first.",
                    statusCode: StatusCodes.Status409Conflict);
            }

            if (!await moduleRepository.DeleteAsync(module))
            {
                return Problem("The module could not be deleted. Please try again.", statusCode: StatusCodes.Status500InternalServerError);
            }

            logger.LogInformation("Module ID {ModuleId} with name '{ModuleName}' was deleted.", module.Id, module.Name);

            return NoContent();
        }

        /*
         * The overview for the signed-in user. The repository only counts quizzes the user may see
         * (public ones and the user's own), and administrators see everything.
         */
        private async Task<List<ModuleOverview>?> LoadOverviewAsync()
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var overview = await moduleRepository.GetOverviewAsync(userId, User.IsInRole("Admin"));

            if (overview == null)
            {
                logger.LogError("The module overview could not be loaded for user {UserId}.", userId);
            }

            return overview;
        }

        /* The rules that need the database or the fixed lists: unique name, known icon, known colour */
        private async Task ValidateFormAsync(ModuleFormDto dto, string name, int? excludeId)
        {
            if (!string.IsNullOrWhiteSpace(name) && await moduleRepository.NameExistsAsync(name, excludeId))
            {
                ModelState.AddModelError(nameof(dto.Name), "A module with this name already exists.");
            }

            if (!ModuleIcons.IsValid(dto.IconKey))
            {
                ModelState.AddModelError(nameof(dto.IconKey), "Choose one of the icons in the list.");
            }

            if (dto.ColorIndex.HasValue && !ModuleColors.IsValid(dto.ColorIndex))
            {
                ModelState.AddModelError(nameof(dto.ColorIndex), "Choose one of the colours in the list.");
            }
        }
    }
}