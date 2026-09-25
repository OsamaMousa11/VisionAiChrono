using CleanArchitectureTemplate_Application.Dtos;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using VisionAiChrono.Application.Dtos.Video;
using VisionAiChrono.Application.ServiceContract;

namespace VisionAiChrono.Api.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class VideoController : ControllerBase
    {
        private readonly IVideoService _videoService;

        public VideoController(IVideoService videoService)
        {
            _videoService = videoService;
        }

        [HttpPost]
        [ProducesResponseType(typeof(ApiResponse<VideoResponseDTO>), StatusCodes.Status201Created)]
        public async Task<IActionResult> Create([FromBody] CreateVideoDTO dto)
        {
            var result = await _videoService.CreateAsync(dto);
            return Created(string.Empty, new ApiResponse<VideoResponseDTO>(result, "Video created successfully."));
        }

        [HttpGet("{id:guid}")]
        [ProducesResponseType(typeof(ApiResponse<VideoResponseDTO>), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetById(Guid id)
        {
            var result = await _videoService.GetByIdAsync(id);
            return Ok(new ApiResponse<VideoResponseDTO>(result, "Video retrieved successfully."));
        }

        [HttpGet]
        [ProducesResponseType(typeof(ApiResponse<IEnumerable<VideoResponseDTO>>), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetAll()
        {
            var result = await _videoService.GetAllAsync();
            return Ok(new ApiResponse<IEnumerable<VideoResponseDTO>>(result, "Videos retrieved successfully."));
        }

        [HttpDelete("{id:guid}")]
        [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status200OK)]
        public async Task<IActionResult> Delete(Guid id)
        {
            await _videoService.DeleteAsync(id);
            return Ok(new ApiResponse("Video deleted successfully."));
        }
    }
}
