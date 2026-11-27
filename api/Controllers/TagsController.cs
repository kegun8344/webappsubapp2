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
     * Tags and the many-to-many link between tags and quizzes.
     * There are two kinds of tags:
     * - Shared tags form a small catalog. Only teachers and administrators can create them,
     *   and every user can put them on their own quizzes.
     * - Personal tags are made by any user, and only the owner can see and use them.
     */
    [ApiController]
    [Route("api")]
    [Authorize]
    public class TagsController : ControllerBase
    {
        private readonly IQuizRepository quizRepository;
        private readonly ITagRepository tagRepository;
        private readonly ILogger<TagsController> logger;

        public TagsController(
            IQuizRepository quizRepo,
            ITagRepository tagRepo,
            ILogger<TagsController> tagLogger)
        {
            quizRepository = quizRepo;
            tagRepository = tagRepo;
            logger = tagLogger;
        }

        // GET: /api/tags
        // The tags the user can filter on or add to a quiz: the shared catalog and the user's own tags.
        [HttpGet("tags")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<ActionResult<List<TagDto>>> GetAvailable()
        {
            var tags = await tagRepository.GetAvailableForUserAsync(CurrentUserId, Array.Empty<int>());

            if (tags == null)
            {
                logger.LogWarning("GET /api/tags failed because the tags could not be loaded.");
                return Problem("The tags could not be loaded.", statusCode: StatusCodes.Status500InternalServerError);
            }

            return tags
                .OrderBy(tag => !tag.IsShared)
                .ThenBy(tag => tag.Name)
                .Select(TagDto.FromTag)
                .ToList();
        }

        // POST: /api/quizzes/5/tags
        // Creates a tag (or reuses one with the same name) and puts it on the quiz.
        [HttpPost("quizzes/{quizId:int}/tags")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<ActionResult<TagDto>> Create(int quizId, CreateTagDto dto)
        {
            var userId = CurrentUserId;

            if (userId == null)
            {
                return Unauthorized();
            }

            var quiz = await quizRepository.GetByIdWithTagsAsync(quizId);

            if (quiz == null)
            {
                logger.LogWarning("A tag could not be added because quiz ID {QuizId} was not found.", quizId);
                return NotFound();
            }

            /* Only the quiz creator or an administrator may change the quiz */
            if (!QuizRules.CanManage(quiz, userId, User.IsInRole("Admin")))
            {
                logger.LogWarning("User {UserId} tried to add a tag to quiz ID {QuizId} without permission.", userId, quizId);
                return StatusCode(StatusCodes.Status403Forbidden);
            }

            var name = dto.Name.Trim();

            if (name.Length == 0)
            {
                ModelState.AddModelError(nameof(dto.Name), "A tag name must be between 1 and 30 characters.");
                return ValidationProblem(ModelState);
            }

            /*
             * Only teachers and administrators may create shared tags. For everyone else
             * the "shared" value is ignored, so a hand-made request cannot make a shared tag.
             */
            var makeShared = dto.Shared && (User.IsInRole("Teacher") || User.IsInRole("Admin"));

            /*
             * Reuse a tag that already exists ("Exam" and "exam" are the same).
             * A shared tag is only matched against shared tags. A personal tag is matched
             * against shared tags and the user's own, so nobody gets a personal copy of a shared tag.
             */
            var lowerName = name.ToLower();

            var tag = makeShared
                ? await tagRepository.FindSharedByNameAsync(lowerName)
                : await tagRepository.FindAvailableByNameAsync(userId, lowerName);

            tag ??= new Tag
            {
                Name = name,
                Color = dto.Color,
                OwnerUserId = makeShared ? null : userId
            };

            if (quiz.Tags.Any(existing => existing.Id != 0 && existing.Id == tag.Id))
            {
                ModelState.AddModelError(nameof(dto.Name), $"The quiz already has the tag \"{tag.Name}\".");
                return ValidationProblem(ModelState);
            }

            /* A new tag is saved together with the quiz, because the quiz is tracked */
            quiz.Tags.Add(tag);

            if (!await quizRepository.UpdateAsync(quiz))
            {
                return Problem("The tag could not be added. Please try again.", statusCode: StatusCodes.Status500InternalServerError);
            }

            logger.LogInformation("Tag '{TagName}' was added to quiz ID {QuizId}.", tag.Name, quizId);

            return TagDto.FromTag(tag);
        }

        // POST: /api/quizzes/5/tags/3
        // Puts an existing tag on the quiz. No new tag is created.
        [HttpPost("quizzes/{quizId:int}/tags/{tagId:int}")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<ActionResult<TagDto>> Attach(int quizId, int tagId)
        {
            var userId = CurrentUserId;

            var quiz = await quizRepository.GetByIdWithTagsAsync(quizId);

            if (quiz == null)
            {
                logger.LogWarning("A tag could not be attached because quiz ID {QuizId} was not found.", quizId);
                return NotFound();
            }

            if (!QuizRules.CanManage(quiz, userId, User.IsInRole("Admin")))
            {
                logger.LogWarning("User {UserId} tried to attach a tag to quiz ID {QuizId} without permission.", userId, quizId);
                return StatusCode(StatusCodes.Status403Forbidden);
            }

            var tag = await tagRepository.GetByIdAsync(tagId);

            /* A user can only use shared tags and their own personal tags. Another user's tag looks like a missing one. */
            if (tag == null || !tag.IsVisibleTo(userId))
            {
                if (tag != null)
                {
                    logger.LogWarning("User {UserId} tried to attach tag ID {TagId} that belongs to another user.", userId, tagId);
                }

                return NotFound();
            }

            if (quiz.Tags.Any(existing => existing.Id == tag.Id))
            {
                ModelState.AddModelError(string.Empty, $"The quiz already has the tag \"{tag.Name}\".");
                return ValidationProblem(ModelState);
            }

            quiz.Tags.Add(tag);

            if (!await quizRepository.UpdateAsync(quiz))
            {
                return Problem("The tag could not be added. Please try again.", statusCode: StatusCodes.Status500InternalServerError);
            }

            logger.LogInformation("Existing tag '{TagName}' was attached to quiz ID {QuizId}.", tag.Name, quizId);

            return TagDto.FromTag(tag);
        }

        // DELETE: /api/quizzes/5/tags/3
        // Takes the tag off the quiz. The tag itself stays for other quizzes.
        [HttpDelete("quizzes/{quizId:int}/tags/{tagId:int}")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> Remove(int quizId, int tagId)
        {
            var userId = CurrentUserId;

            var quiz = await quizRepository.GetByIdWithTagsAsync(quizId);

            if (quiz == null)
            {
                logger.LogWarning("A tag could not be removed because quiz ID {QuizId} was not found.", quizId);
                return NotFound();
            }

            if (!QuizRules.CanManage(quiz, userId, User.IsInRole("Admin")))
            {
                logger.LogWarning("User {UserId} tried to remove a tag from quiz ID {QuizId} without permission.", userId, quizId);
                return StatusCode(StatusCodes.Status403Forbidden);
            }

            var tag = quiz.Tags.FirstOrDefault(existing => existing.Id == tagId);

            if (tag == null)
            {
                return NotFound();
            }

            quiz.Tags.Remove(tag);

            if (!await quizRepository.UpdateAsync(quiz))
            {
                return Problem("The tag could not be removed. Please try again.", statusCode: StatusCodes.Status500InternalServerError);
            }

            logger.LogInformation("Tag '{TagName}' was removed from quiz ID {QuizId}.", tag.Name, quizId);

            return NoContent();
        }

        // GET: /api/tags/mine
        // The user's own tags with how many quizzes each is on, for the My tags page.
        [HttpGet("tags/mine")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<ActionResult<List<TagUsage>>> GetMine()
        {
            var userId = CurrentUserId;

            if (userId == null)
            {
                return Unauthorized();
            }

            var tags = await tagRepository.GetOwnWithUsageAsync(userId);

            if (tags == null)
            {
                return Problem("Your tags could not be loaded. Please try again.", statusCode: StatusCodes.Status500InternalServerError);
            }

            return tags;
        }

        // PUT: /api/tags/3
        // Renames one of the user's own tags. Course tags cannot be renamed.
        [HttpPut("tags/{id:int}")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<ActionResult<TagDto>> Rename(int id, RenameTagDto dto)
        {
            var tag = await tagRepository.GetByIdAsync(id);

            if (tag == null)
            {
                logger.LogWarning("Rename requested for tag ID {TagId}, but the tag was not found.", id);
                return NotFound();
            }

            /* Only the owner can rename a personal tag */
            if (tag.OwnerUserId == null || tag.OwnerUserId != CurrentUserId)
            {
                logger.LogWarning("User {UserId} tried to rename tag ID {TagId} without permission.", CurrentUserId, id);
                return StatusCode(StatusCodes.Status403Forbidden);
            }

            var name = dto.Name.Trim();

            if (name.Length == 0)
            {
                ModelState.AddModelError(nameof(dto.Name), "A tag name must be between 1 and 30 characters.");
                return ValidationProblem(ModelState);
            }

            /* The new name must not match a course tag or another of the user's tags ("Exam" and "exam" are the same) */
            var existing = await tagRepository.FindAvailableByNameAsync(CurrentUserId, name.ToLower());

            if (existing != null && existing.Id != tag.Id)
            {
                ModelState.AddModelError(nameof(dto.Name), $"A tag called \"{existing.Name}\" already exists.");
                return ValidationProblem(ModelState);
            }

            var oldName = tag.Name;
            tag.Name = name;

            if (!await tagRepository.UpdateAsync(tag))
            {
                return Problem("The tag could not be renamed. Please try again.", statusCode: StatusCodes.Status500InternalServerError);
            }

            logger.LogInformation("User {UserId} renamed tag '{OldName}' to '{NewName}'.", CurrentUserId, oldName, name);

            return TagDto.FromTag(tag);
        }

        // DELETE: /api/tags/3
        // Deletes one of the user's own tags and removes it from every quiz. Course tags cannot be deleted.
        [HttpDelete("tags/{id:int}")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> Delete(int id)
        {
            var tag = await tagRepository.GetByIdAsync(id);

            if (tag == null)
            {
                logger.LogWarning("Delete requested for tag ID {TagId}, but the tag was not found.", id);
                return NotFound();
            }

            /* Only the owner can delete a personal tag */
            if (tag.OwnerUserId == null || tag.OwnerUserId != CurrentUserId)
            {
                logger.LogWarning("User {UserId} tried to delete tag ID {TagId} without permission.", CurrentUserId, id);
                return StatusCode(StatusCodes.Status403Forbidden);
            }

            if (!await tagRepository.DeleteAsync(tag))
            {
                return Problem("The tag could not be deleted. Please try again.", statusCode: StatusCodes.Status500InternalServerError);
            }

            logger.LogInformation("User {UserId} deleted their tag '{TagName}'.", CurrentUserId, tag.Name);

            return NoContent();
        }
        
        /* The signed-in user's Identity ID, read from the token */
        private string? CurrentUserId => User.FindFirstValue(ClaimTypes.NameIdentifier);
    }
}