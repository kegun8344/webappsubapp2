using Microsoft.AspNetCore.Mvc;

namespace api.Controllers
{
    /* Health check: lets the React app (and us) see that the API is running */
    [ApiController]
    [Route("api/[controller]")]
    public class PingController : ControllerBase
    {
        private readonly ILogger<PingController> logger;

        public PingController(ILogger<PingController> pingLogger)
        {
            logger = pingLogger;
        }

        // GET: /api/ping
        [HttpGet]
        public IActionResult Get()
        {
            logger.LogInformation("Ping received.");
            return Ok(new { message = "StudyQuest API is running", time = DateTime.UtcNow });
        }
    }
}