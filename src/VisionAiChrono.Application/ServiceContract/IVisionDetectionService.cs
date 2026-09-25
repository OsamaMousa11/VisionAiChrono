using VisionAiChrono.Application.VisionDetection.Models;

namespace VisionAiChrono.Application.VisionDetection;

public interface IVisionDetectionService
{
    /// <summary>
    /// Sends an image OR a video to the Python detection service for the given task.
    /// The Python service auto-detects which one it is from the file extension/content-type.
    /// Check response.Type ("image" or "video") to know how to handle the result.
    /// </summary>
    Task<DetectionResponse> DetectAsync(
        DetectionTask task, Stream fileStream, string fileName, CancellationToken ct = default);

    /// <summary>
    /// Streams the annotated video back (after a video was analyzed) so your
    /// frontend can download it through your own backend instead of hitting
    /// the Python service directly.
    /// </summary>
    Task<Stream> DownloadVideoAsync(string videoId, CancellationToken ct = default);
}