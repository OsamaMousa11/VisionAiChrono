using Microsoft.AspNetCore.Mvc;
using VisionAiChrono.Application.VisionDetection;
using VisionAiChrono.Application.VisionDetection.Models;

namespace VisionAiChrono.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public sealed class VisionController : ControllerBase
{
    private readonly IVisionDetectionService _visionDetectionService;

    public VisionController(IVisionDetectionService visionDetectionService)
    {
        _visionDetectionService = visionDetectionService;
    }

    /// <summary>
    /// Detects the given task (person/weapon/fire) on an uploaded image OR video.
    /// The Python service figures out which one it is automatically.
    /// </summary>
    [HttpPost("detect/{task}")]
    [RequestSizeLimit(50 * 1024 * 1024)] // 50 MB max upload (covers small videos too)
    public async Task<IActionResult> Detect(string task, IFormFile file, CancellationToken ct)
    {
        if (file is null || file.Length == 0)
            return BadRequest(new { message = "No file uploaded." });

        if (!Enum.TryParse<DetectionTask>(task, ignoreCase: true, out var detectionTask))
            return BadRequest(new { message = $"Unknown task '{task}'. Use person, weapon, or fire." });

        await using var stream = file.OpenReadStream();
        var result = await _visionDetectionService.DetectAsync(detectionTask, stream, file.FileName, ct);

        return Ok(result);
    }

    /// <summary>
    /// Proxies the annotated video back through your own backend.
    /// </summary>
    [HttpGet("download/{videoId}")]
    public async Task<IActionResult> DownloadVideo(string videoId, CancellationToken ct)
    {
        var stream = await _visionDetectionService.DownloadVideoAsync(videoId, ct);
        return File(stream, "video/mp4", $"{videoId}.mp4");
    }
}