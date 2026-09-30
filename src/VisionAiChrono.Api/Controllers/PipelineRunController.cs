using CleanArchitectureTemplate_Application.Dtos;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using VisionAiChrono.Application.Dtos.PipelineResult;
using VisionAiChrono.Application.Dtos.PipelineRun;
using VisionAiChrono.Application.ServiceContract;

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
    }
}
