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
    Person,
    Weapon,
    Fire
}

public static class DetectionTaskExtensions
{
    public static string ToRoute(this DetectionTask task) => task switch
    {
        DetectionTask.Person => "person",
        DetectionTask.Weapon => "weapon",
        DetectionTask.Fire => "fire",
        _ => throw new ArgumentOutOfRangeException(nameof(task))
    };
}