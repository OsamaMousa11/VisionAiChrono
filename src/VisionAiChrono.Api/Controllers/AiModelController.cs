using CleanArchitectureTemplate_Application.Dtos;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using VisionAiChrono.Application.Dtos.AiModel;
using VisionAiChrono.Application.ServiceContract;

namespace VisionAiChrono.Api.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class AiModelController : ControllerBase
    {
        private readonly IAiModelService _aiModelService;

        public AiModelController(IAiModelService aiModelService)
        {
            _aiModelService = aiModelService;
        }

        [HttpPost]
        [ProducesResponseType(typeof(ApiResponse<AiModelResponseDTO>), StatusCodes.Status201Created)]
        public async Task<IActionResult> Create([FromBody] CreateAiModelDTO dto)
        {
            var result = await _aiModelService.CreateAsync(dto);
            return Created(string.Empty, new ApiResponse<AiModelResponseDTO>(result, "AI Model created successfully."));
        }

        [HttpGet("{id:guid}")]
        [ProducesResponseType(typeof(ApiResponse<AiModelResponseDTO>), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetById(Guid id)
        {
            var result = await _aiModelService.GetByIdAsync(id);
            return Ok(new ApiResponse<AiModelResponseDTO>(result, "AI Model retrieved successfully."));
        }

        [HttpGet]
        [ProducesResponseType(typeof(ApiResponse<IEnumerable<AiModelResponseDTO>>), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetAll()
        {
            var result = await _aiModelService.GetAllAsync();
            return Ok(new ApiResponse<IEnumerable<AiModelResponseDTO>>(result, "AI Models retrieved successfully."));
        }

        [HttpPut("{id:guid}")]
        [ProducesResponseType(typeof(ApiResponse<AiModelResponseDTO>), StatusCodes.Status200OK)]
        public async Task<IActionResult> Update(Guid id, [FromBody] UpdateAiModelDTO dto)
        {
            var result = await _aiModelService.UpdateAsync(id, dto);
            return Ok(new ApiResponse<AiModelResponseDTO>(result, "AI Model updated successfully."));
        }

        [HttpDelete("{id:guid}")]
        [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status200OK)]
        public async Task<IActionResult> Delete(Guid id)
        {
            await _aiModelService.DeleteAsync(id);
            return Ok(new ApiResponse("AI Model deleted successfully."));
        }
    }
}
