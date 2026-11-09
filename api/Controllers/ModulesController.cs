using api.DAL;
using api.DTOs;
using Microsoft.AspNetCore.Mvc;

namespace api.Controllers
{
    /*
     * Read-only endpoints for the ITPE3200 modules, used by the module cards in the React app.
     * Same order (by name) and the same quiz count as Module/Index in Subapp 1.
     * The controller only uses IModuleRepository, never the database directly.
     */
    [ApiController]
    [Route("api/[controller]")]
    public class ModulesController : ControllerBase
    {
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
        [HttpGet]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<ActionResult<List<ModuleDto>>> GetAll()
        {
            /*
             * No login yet (JWT is made on another branch), so only public quizzes are counted.
             * After login: pass the signed-in user's ID and whether they are an administrator.
             */
            var overview = await moduleRepository.GetOverviewAsync(userId: null, isAdmin: false);

            if (overview == null)
            {
                logger.LogWarning("GET /api/modules failed because the modules could not be loaded.");
                return Problem("The modules could not be loaded.", statusCode: StatusCodes.Status500InternalServerError);
            }

            /* Per-user progress (Your progress, mastery stars) is added here after login */

            return overview
                .Select(ModuleDto.FromOverview)
                .ToList();
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
            var overview = await moduleRepository.GetOverviewAsync(userId: null, isAdmin: false);

            if (overview == null)
            {
                logger.LogWarning("GET /api/modules/{ModuleId} failed because the modules could not be loaded.", id);
                return Problem("The module could not be loaded.", statusCode: StatusCodes.Status500InternalServerError);
            }

            var module = overview.FirstOrDefault(item => item.Module.Id == id);

            if (module == null)
            {
                logger.LogWarning("Module ID {ModuleId} was requested, but it was not found.", id);
                return NotFound();
            }

            /* Per-user progress for this module is added here after login */

            return ModuleDto.FromOverview(module);
        }
    }
}