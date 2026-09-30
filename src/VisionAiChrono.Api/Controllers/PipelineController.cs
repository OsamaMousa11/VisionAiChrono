using CleanArchitectureTemplate_Application.Dtos;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using VisionAiChrono.Application.Dtos.Pipeline;
using VisionAiChrono.Application.Dtos.PipelineRun;
using VisionAiChrono.Application.ServiceContract;
using VisionAiChrono.Application.VisionDetection;
using VisionAiChrono.Application.VisionDetection.Models;
using VisionAiChrono.Domain.Enumration;

namespace VisionAiChrono.Api.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class PipelineController : ControllerBase
    {
        private readonly IPipelineService _pipelineService;
        private readonly IPipelineRunService _pipelineRunService;

        public PipelineController(
            IPipelineService pipelineService,
            IPipelineRunService pipelineRunService)
        {
            _pipelineService = pipelineService;
            _pipelineRunService = pipelineRunService;
        }

        [HttpPost]
        [ProducesResponseType(typeof(ApiResponse<PipelineResponseDTO>), StatusCodes.Status201Created)]
        public async Task<IActionResult> Create([FromBody] CreatePipelineDTO dto)
        {
            var result = await _pipelineService.CreateAsync(dto);
            return Created(string.Empty, new ApiResponse<PipelineResponseDTO>(result, "Pipeline created successfully."));
        }

        [HttpPost("{id:guid}/execute")]
        [Consumes("multipart/form-data")]
        [RequestSizeLimit(500 * 1024 * 1024)]
        [ProducesResponseType(typeof(ApiResponse<RunExecutionResponseDTO>), StatusCodes.Status200OK)]
        public async Task<IActionResult> Execute(
            Guid id,
            [FromForm] string tasks,
            [FromForm] IFormFileCollection files,
            CancellationToken cancellationToken)
        {
            if (files is null || files.Count == 0)
                return BadRequest(new ApiResponse("At least one video or image file is required."));

            if (!TaskIndexParser.TryParse(tasks, out var parsedTasks, out var error))
                return BadRequest(new ApiResponse(error!));

            var inputs = BuildInputs(files);

            if (inputs is null)
                return BadRequest(new ApiResponse("At least one video or image file is required."));

            var userId = User.FindFirst("uid")?.Value ?? string.Empty;

            var result = await _pipelineRunService.ExecuteAsync(
                new CreateRunWithMediaDTO
                {
                    PipelineId = id,
                    Tasks = parsedTasks.Select(t => t.ToIndex()).ToList()
                },
                inputs,
                userId,
                cancellationToken);

            foreach (var input in inputs)
                await input.Content.DisposeAsync();

            return Ok(new ApiResponse<RunExecutionResponseDTO>(result, result.Message));
        }

        private static List<UploadedMediaInput> BuildInputs(IFormFileCollection? files)
        {
            var inputs = new List<UploadedMediaInput>();

            if (files is null)
                return inputs;

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

            return inputs;
        }

        [HttpPost("{id:guid}/execute-async")]
        [Consumes("multipart/form-data")]
        [RequestSizeLimit(500 * 1024 * 1024)]
        [ProducesResponseType(typeof(ApiResponse<RunExecutionResponseDTO>), StatusCodes.Status202Accepted)]
        public async Task<IActionResult> ExecuteInBackground(
            Guid id,
            [FromForm] string tasks,
            [FromForm] IFormFileCollection files,
            CancellationToken cancellationToken)
        {
            var inputs = BuildInputs(files);

            if (inputs is null)
                return BadRequest(new ApiResponse("At least one video or image file is required."));

            if (!TaskIndexParser.TryParse(tasks, out var parsedTasks, out var error))
                return BadRequest(new ApiResponse(error!));

            var userId = User.FindFirst("uid")?.Value ?? string.Empty;

            var result = await _pipelineRunService.QueueExecutionAsync(
                new CreateRunWithMediaDTO
                {
                    PipelineId = id,
                    Tasks = parsedTasks.Select(t => t.ToIndex()).ToList()
                },
                inputs,
                userId,
                cancellationToken);

            foreach (var input in inputs)
                await input.Content.DisposeAsync();

            return Accepted(new ApiResponse<RunExecutionResponseDTO>(result, result.Message));
        }

        [HttpGet]
        [ProducesResponseType(typeof(ApiResponse<IEnumerable<PipelineResponseDTO>>), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetAll()
        {
            var result = await _pipelineService.GetAllAsync();
            return Ok(new ApiResponse<IEnumerable<PipelineResponseDTO>>(result, "Pipelines retrieved successfully."));
        }

        [HttpGet("{id:guid}")]
        [ProducesResponseType(typeof(ApiResponse<PipelineResponseDTO>), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetById(Guid id)
        {
            var result = await _pipelineService.GetByIdAsync(id);
            return Ok(new ApiResponse<PipelineResponseDTO>(result, "Pipeline retrieved successfully."));
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
    }
}
