using CleanArchitectureTemplate_Application.Dtos;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using VisionAiChrono.Application.Dtos.Pipeline;
using VisionAiChrono.Application.Dtos.PipelineRun;
using VisionAiChrono.Application.ServiceContract;
using VisionAiChrono.Domain.Enumration;

namespace VisionAiChrono.Api.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class PipelineController : ControllerBase
    {
        private readonly IPipelineService _pipelineService;

        public PipelineController(IPipelineService pipelineService)
        {
            _pipelineService = pipelineService;
        }

        [HttpPost]
        [ProducesResponseType(typeof(ApiResponse<PipelineResponseDTO>), StatusCodes.Status201Created)]
        public async Task<IActionResult> Create([FromBody] CreatePipelineDTO dto)
        {
            var result = await _pipelineService.CreateAsync(dto);
            return Created(string.Empty, new ApiResponse<PipelineResponseDTO>(result, "Pipeline created successfully."));
        }

        [HttpGet("{id:guid}")]
        [ProducesResponseType(typeof(ApiResponse<PipelineResponseDTO>), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetById(Guid id)
        {
            var result = await _pipelineService.GetByIdAsync(id);
            return Ok(new ApiResponse<PipelineResponseDTO>(result, "Pipeline retrieved successfully."));
        }

        [HttpGet]
        [ProducesResponseType(typeof(ApiResponse<IEnumerable<PipelineResponseDTO>>), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetAll()
        {
            var result = await _pipelineService.GetAllAsync();
            return Ok(new ApiResponse<IEnumerable<PipelineResponseDTO>>(result, "Pipelines retrieved successfully."));
        }

        [HttpPut("{id:guid}")]
        [ProducesResponseType(typeof(ApiResponse<PipelineResponseDTO>), StatusCodes.Status200OK)]
        public async Task<IActionResult> Update(Guid id, [FromBody] UpdatePipelineDTO dto)
        {
            var result = await _pipelineService.UpdateAsync(id, dto);
            return Ok(new ApiResponse<PipelineResponseDTO>(result, "Pipeline updated successfully."));
        }

        [HttpDelete("{id:guid}")]
        [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status200OK)]
        public async Task<IActionResult> Delete(Guid id)
        {
            await _pipelineService.DeleteAsync(id);
            return Ok(new ApiResponse("Pipeline deleted successfully."));
        }

        [HttpPost("{pipelineId:guid}/models")]
        [ProducesResponseType(typeof(ApiResponse<PipelineModelResponseDTO>), StatusCodes.Status201Created)]
        public async Task<IActionResult> AddModel(Guid pipelineId, [FromBody] AddPipelineModelDTO dto)
        {
            var result = await _pipelineService.AddModelAsync(pipelineId, dto);
            return Created(string.Empty, new ApiResponse<PipelineModelResponseDTO>(result, "Model added to pipeline successfully."));
        }

        [HttpDelete("{pipelineId:guid}/models/{modelId:guid}")]
        [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status200OK)]
        public async Task<IActionResult> RemoveModel(Guid pipelineId, Guid modelId)
        {
            await _pipelineService.RemoveModelAsync(pipelineId, modelId);
            return Ok(new ApiResponse("Model removed from pipeline successfully."));
        }

        [HttpGet("{pipelineId:guid}/runs")]
        [ProducesResponseType(typeof(ApiResponse<PagedResultDTO<PipelineRunHistoryResponseDTO>>), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetRunHistory(
            Guid pipelineId,
            [FromQuery] int pageNumber = 1,
            [FromQuery] int pageSize = 20,
            [FromQuery] ExecutionStatus? status = null,
            CancellationToken cancellationToken = default)
        {
            var result = await _pipelineService.GetRunHistoryAsync(
                pipelineId, pageNumber, pageSize, status, cancellationToken);

            return Ok(new ApiResponse<PagedResultDTO<PipelineRunHistoryResponseDTO>>(
                result, "Pipeline run history retrieved successfully."));
        }

        [HttpPost("{pipelineId:guid}/clone")]
        [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status201Created)]
        public async Task<IActionResult> CloneAsDraft(Guid pipelineId, [FromQuery] string? name = null)
        {
            var userId = User.FindFirst("uid")?.Value ?? string.Empty;
            var result = await _pipelineService.CloneAsDraftAsync(pipelineId, userId, name);

            return Created(string.Empty, new ApiResponse<PipelineRunDetailResponseDTO>(
                result, "Pipeline cloned as a new draft."));
        }
    }
}
