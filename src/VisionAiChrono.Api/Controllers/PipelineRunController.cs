using CleanArchitectureTemplate_Application.Dtos;
using CleanArchitectureTemplate_Application.Exceptions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Text.Json;
using VisionAiChrono.Application.Dtos.PipelineResult;
using VisionAiChrono.Application.Dtos.PipelineRun;
using VisionAiChrono.Application.ServiceContract;
using VisionAiChrono.Domain.Enumration;

namespace VisionAiChrono.Api.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class PipelineRunController : ControllerBase
    {
        private readonly IPipelineRunService _pipelineRunService;

        public PipelineRunController(IPipelineRunService pipelineRunService)
        {
            _pipelineRunService = pipelineRunService;
        }

        private string GetUserId() => User.FindFirst("uid")?.Value
            ?? throw new UnauthorizedException("User not authenticated.");

        [HttpPost]
        [ProducesResponseType(typeof(ApiResponse<PipelineRunResponseDTO>), StatusCodes.Status201Created)]
        public async Task<IActionResult> Create([FromBody] CreatePipelineRunDTO dto)
        {
            var userId = GetUserId();
            var result = await _pipelineRunService.CreateAsync(dto, userId);
            return Created(string.Empty, new ApiResponse<PipelineRunResponseDTO>(result, "Pipeline run created successfully."));
        }

        [HttpPost("execute")]
        [Consumes("multipart/form-data")]
        [RequestSizeLimit(500 * 1024 * 1024)]
        [ProducesResponseType(typeof(ApiResponse<RunExecutionResponseDTO>), StatusCodes.Status202Accepted)]
        public async Task<IActionResult> Execute(
            [FromForm] Guid pipelineId,
            [FromForm] IFormFileCollection files,
            [FromForm] string modelsJson,
            CancellationToken cancellationToken)
        {
            if (files is null || files.Count == 0)
                return BadRequest(new ApiResponse("At least one video or image file is required."));

            List<RunModelSelectionDTO> models;

            try
            {
                models = JsonSerializer.Deserialize<List<RunModelSelectionDTO>>(modelsJson ?? "[]")
                         ?? new List<RunModelSelectionDTO>();
            }
            catch (JsonException)
            {
                return BadRequest(new ApiResponse("modelsJson is not a valid JSON array of models."));
            }

            if (models.Count == 0)
                return BadRequest(new ApiResponse("At least one model is required."));

            var inputs = new List<UploadedMediaInput>(files.Count);

            foreach (var file in files)
            {
                if (file.Length == 0)
                    continue;

                inputs.Add(new UploadedMediaInput
                {
                    FileName = file.FileName,
                    ContentType = file.ContentType,
                    Length = file.Length,
                    Content = file.OpenReadStream()
                });
            }

            if (inputs.Count == 0)
                return BadRequest(new ApiResponse("At least one video or image file is required."));

            var userId = GetUserId();

            var result = await _pipelineRunService.ExecuteAsync(
                new CreateRunWithMediaDTO { PipelineId = pipelineId, Models = models },
                inputs,
                userId,
                cancellationToken);

            foreach (var input in inputs)
                await input.Content.DisposeAsync();

            return Accepted(new ApiResponse<RunExecutionResponseDTO>(result, result.Message));
        }

        [HttpGet("history")]
        [ProducesResponseType(typeof(ApiResponse<PagedResultDTO<PipelineRunHistoryResponseDTO>>), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetHistory(
            [FromQuery] int pageNumber = 1,
            [FromQuery] int pageSize = 20,
            [FromQuery] Guid? pipelineId = null,
            [FromQuery] ExecutionStatus? status = null,
            CancellationToken cancellationToken = default)
        {
            var userId = GetUserId();
            var result = await _pipelineRunService.GetHistoryAsync(
                userId, pageNumber, pageSize, pipelineId, status, cancellationToken);

            return Ok(new ApiResponse<PagedResultDTO<PipelineRunHistoryResponseDTO>>(
                result, "Pipeline run history retrieved successfully."));
        }

        [HttpGet("{id:guid}/detail")]
        [ProducesResponseType(typeof(ApiResponse<PipelineRunDetailResponseDTO>), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetDetail(Guid id)
        {
            var result = await _pipelineRunService.GetDetailAsync(id);
            return Ok(new ApiResponse<PipelineRunDetailResponseDTO>(result, "Pipeline run detail retrieved successfully."));
        }

        [HttpGet("{id:guid}/results")]
        [ProducesResponseType(typeof(ApiResponse<IEnumerable<PipelineResultResponseDTO>>), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetResults(Guid id)
        {
            var result = await _pipelineRunService.GetResultsAsync(id);
            return Ok(new ApiResponse<IEnumerable<PipelineResultResponseDTO>>(result, "Results retrieved successfully."));
        }

        [HttpGet("{id:guid}/export")]
        [Produces("application/vnd.openxmlformats-officedocument.spreadsheetml.sheet")]
        public async Task<IActionResult> DownloadExport(Guid id)
        {
            var (content, fileName, contentType) = await _pipelineRunService.GetLatestExportAsync(id);
            return File(content, contentType, fileName);
        }

        [HttpPut("{id:guid}")]
        [Consumes("multipart/form-data")]
        [RequestSizeLimit(500 * 1024 * 1024)]
        [ProducesResponseType(typeof(ApiResponse<PipelineRunDetailResponseDTO>), StatusCodes.Status200OK)]
        public async Task<IActionResult> Update(
            Guid id,
            [FromForm] string? pipelineId,
            [FromForm] string? modelsJson,
            [FromForm] string? removeMediaIdsJson,
            [FromForm] IFormFileCollection? files,
            CancellationToken cancellationToken)
        {
            List<RunModelSelectionDTO>? models = null;
            List<Guid>? removeMediaIds = null;

            if (!string.IsNullOrWhiteSpace(modelsJson))
            {
                models = JsonSerializer.Deserialize<List<RunModelSelectionDTO>>(modelsJson);

                if (models is null)
                    return BadRequest(new ApiResponse("modelsJson is not a valid JSON array."));
            }

            if (!string.IsNullOrWhiteSpace(removeMediaIdsJson))
            {
                removeMediaIds = JsonSerializer.Deserialize<List<Guid>>(removeMediaIdsJson);

                if (removeMediaIds is null)
                    return BadRequest(new ApiResponse("removeMediaIdsJson is not a valid JSON array."));
            }

            var inputs = new List<UploadedMediaInput>();

            if (files is not null)
            {
                foreach (var file in files)
                {
                    if (file.Length == 0)
                        continue;

                    inputs.Add(new UploadedMediaInput
                    {
                        FileName = file.FileName,
                        ContentType = file.ContentType,
                        Length = file.Length,
                        Content = file.OpenReadStream()
                    });
                }
            }

            var userId = GetUserId();

            var result = await _pipelineRunService.UpdateAsync(
                id,
                new UpdatePipelineRunDTO
                {
                    PipelineId = Guid.TryParse(pipelineId, out var parsed) ? parsed : null,
                    Models = models,
                    RemoveMediaIds = removeMediaIds
                },
                inputs,
                userId,
                cancellationToken);

            foreach (var input in inputs)
                await input.Content.DisposeAsync();

            return Ok(new ApiResponse<PipelineRunDetailResponseDTO>(result, "Pipeline run updated successfully."));
        }

        [HttpPost("{id:guid}/rerun")]
        [ProducesResponseType(typeof(ApiResponse<RunExecutionResponseDTO>), StatusCodes.Status202Accepted)]
        public async Task<IActionResult> ReRun(Guid id)
        {
            var userId = GetUserId();
            var result = await _pipelineRunService.ReRunAsync(id, userId);
            return Accepted(new ApiResponse<RunExecutionResponseDTO>(result, result.Message));
        }

        [HttpGet("{id:guid}")]
        [ProducesResponseType(typeof(ApiResponse<PipelineRunResponseDTO>), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetById(Guid id)
        {
            var result = await _pipelineRunService.GetByIdAsync(id);
            return Ok(new ApiResponse<PipelineRunResponseDTO>(result, "Pipeline run retrieved successfully."));
        }

        [HttpGet("my")]
        [ProducesResponseType(typeof(ApiResponse<IEnumerable<PipelineRunResponseDTO>>), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetMyRuns()
        {
            var userId = GetUserId();
            var result = await _pipelineRunService.GetAllByUserAsync(userId);
            return Ok(new ApiResponse<IEnumerable<PipelineRunResponseDTO>>(result, "Pipeline runs retrieved successfully."));
        }

        [HttpPut("{id:guid}/status")]
        [ProducesResponseType(typeof(ApiResponse<PipelineRunResponseDTO>), StatusCodes.Status200OK)]
        public async Task<IActionResult> UpdateStatus(Guid id, [FromBody] UpdatePipelineRunStatusDTO dto)
        {
            var result = await _pipelineRunService.UpdateStatusAsync(id, dto);
            return Ok(new ApiResponse<PipelineRunResponseDTO>(result, "Pipeline run status updated successfully."));
        }

        [HttpPost("{runId:guid}/videos")]
        [ProducesResponseType(typeof(ApiResponse<PipelineRunVideoResponseDTO>), StatusCodes.Status201Created)]
        public async Task<IActionResult> AddVideo(Guid runId, [FromBody] AddPipelineRunVideoDTO dto)
        {
            var result = await _pipelineRunService.AddVideoAsync(runId, dto);
            return Created(string.Empty, new ApiResponse<PipelineRunVideoResponseDTO>(result, "Video added to run successfully."));
        }

        [HttpPost("{runId:guid}/models")]
        [ProducesResponseType(typeof(ApiResponse<PipelineRunModelResponseDTO>), StatusCodes.Status201Created)]
        public async Task<IActionResult> AddModel(Guid runId, [FromBody] AddPipelineRunModelDTO dto)
        {
            var result = await _pipelineRunService.AddModelAsync(runId, dto);
            return Created(string.Empty, new ApiResponse<PipelineRunModelResponseDTO>(result, "Model added to run successfully."));
        }

        [HttpDelete("{runId:guid}/videos/{videoId:guid}")]
        [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status200OK)]
        public async Task<IActionResult> RemoveVideo(Guid runId, Guid videoId)
        {
            await _pipelineRunService.RemoveVideoAsync(runId, videoId);
            return Ok(new ApiResponse("Video removed from run successfully."));
        }

        [HttpDelete("{runId:guid}/models/{modelId:guid}")]
        [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status200OK)]
        public async Task<IActionResult> RemoveModel(Guid runId, Guid modelId)
        {
            await _pipelineRunService.RemoveModelAsync(runId, modelId);
            return Ok(new ApiResponse("Model removed from run successfully."));
        }
    }
}
