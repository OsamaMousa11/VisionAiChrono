using CleanArchitectureTemplate_Application.Dtos;
using CleanArchitectureTemplate_Application.Exceptions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using VisionAiChrono.Application.Dtos.Favorite;
using VisionAiChrono.Application.ServiceContract;

namespace VisionAiChrono.Api.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class FavoriteController : ControllerBase
    {
        private readonly IFavoriteService _favoriteService;

        public FavoriteController(IFavoriteService favoriteService)
        {
            _favoriteService = favoriteService;
        }

        private string GetUserId() => User.FindFirst("uid")?.Value
            ?? throw new UnauthorizedException("User not authenticated.");

        [HttpPost]
        [ProducesResponseType(typeof(ApiResponse<FavoriteResponseDTO>), StatusCodes.Status201Created)]
        public async Task<IActionResult> Add([FromBody] AddFavoriteDTO dto)
        {
            var userId = GetUserId();
            var result = await _favoriteService.AddAsync(dto, userId);
            return Created(string.Empty, new ApiResponse<FavoriteResponseDTO>(result, "Pipeline added to favorites."));
        }

        [HttpDelete("{pipelineId:guid}")]
        [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status200OK)]
        public async Task<IActionResult> Remove(Guid pipelineId)
        {
            var userId = GetUserId();
            await _favoriteService.RemoveAsync(pipelineId, userId);
            return Ok(new ApiResponse("Pipeline removed from favorites."));
        }

        [HttpGet]
        [ProducesResponseType(typeof(ApiResponse<IEnumerable<FavoriteResponseDTO>>), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetAll()
        {
            var userId = GetUserId();
            var result = await _favoriteService.GetAllByUserAsync(userId);
            return Ok(new ApiResponse<IEnumerable<FavoriteResponseDTO>>(result, "Favorites retrieved successfully."));
        }

        [HttpGet("check/{pipelineId:guid}")]
        [ProducesResponseType(typeof(ApiResponse<bool>), StatusCodes.Status200OK)]
        public async Task<IActionResult> Check(Guid pipelineId)
        {
            var userId = GetUserId();
            var result = await _favoriteService.IsFavoritedAsync(pipelineId, userId);
            return Ok(new ApiResponse<bool>(result, "Favorite status retrieved."));
        }
    }
}
