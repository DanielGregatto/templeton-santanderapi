using Domain.Contracts.API;
using Domain.Interfaces;
using Microsoft.AspNetCore.Mvc;
using Services.Contracts.Results;
using Services.Features.Stories.Queries.GetBestStories;
using UI.API.Controllers.Base;

namespace UI.API.Controllers
{
    public class StoriesController : CoreController
    {
        private readonly IMediatorHandler _mediator;
        private readonly ILogger<StoriesController> _logger;

        public StoriesController(IMediatorHandler mediator, ILogger<StoriesController> logger)
        {
            _mediator = mediator;
            _logger = logger;
        }

        /// <summary>
        /// Retrieves the best <paramref name="n"/> Hacker News stories, ordered by score descending.
        /// </summary>
        /// <param name="n">Number of stories to return. Must be greater than 0.</param>
        [HttpGet("v1/stories/best")]
        [ProducesResponseType(typeof(IEnumerable<StoryResult>), 200)]
        [ProducesResponseType(typeof(ErrorResponseDto), 400)]
        public async Task<IActionResult> GetBest([FromQuery] int n = 10)
        {
            _logger.LogInformation("Getting best {N} stories", n);

            var query = new GetBestStoriesQuery { N = n };
            var result = await _mediator.SendCommand(query);

            if (!result.IsSuccess)
                _logger.LogWarning("Failed to retrieve best stories. Errors: {Errors}",
                    string.Join(", ", result.Errors.Select(e => e.Message)));

            return ToActionResult(result);
        }
    }
}
