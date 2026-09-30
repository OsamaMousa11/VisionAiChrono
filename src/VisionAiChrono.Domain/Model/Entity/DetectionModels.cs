using System.Text.Json.Serialization;

namespace VisionAiChrono.Application.VisionDetection.Models;

public sealed class DetectionBox
{
    public double X1 { get; set; }
    public double Y1 { get; set; }
    public double X2 { get; set; }
    public double Y2 { get; set; }
}

public sealed class Detection
{
    public string Label { get; set; } = default!;
    public double Confidence { get; set; }
    public DetectionBox Box { get; set; } = default!;
}

public sealed class DetectionResponse
{
    public string Type { get; set; } = default!;
    public string Task { get; set; } = default!;

    // ---- populated when Type == "image" ----
    public int? Count { get; set; }
    public List<Detection>? Detections { get; set; }

    // ---- populated when Type == "video" ----
    public string? VideoId { get; set; }
    public double? DurationSeconds { get; set; }
    public bool? Detected { get; set; }
    public int? TotalDetections { get; set; }
    public string? DownloadUrl { get; set; }

    public bool IsVideo => Type == "video";
    public bool IsImage => Type == "image";
}

public enum DetectionTask
{
    Person = 0,
    Weapon = 1,
    Fire = 2
}

public static class DetectionTaskExtensions
{
    /// <summary>
    /// The route the vision detection service exposes, e.g. Person -> "/detect/person".
    /// The API itself accepts the index (0/1/2) and maps it here.
    /// </summary>
    public static string ToRoute(this DetectionTask task) => task switch
    {
        DetectionTask.Person => "person",
        DetectionTask.Weapon => "weapon",
        DetectionTask.Fire => "fire",
        _ => throw new ArgumentOutOfRangeException(nameof(task))
    };

    public static int ToIndex(this DetectionTask task) => (int)task;

    /// <summary>
    /// Maps a raw 0/1/2 value coming from the API into a detection task.
    /// Returns null when the value is not a known task index.
    /// </summary>
    public static DetectionTask? FromIndex(int index) =>
        Enum.IsDefined(typeof(DetectionTask), index)
            ? (DetectionTask)index
            : null;
}
