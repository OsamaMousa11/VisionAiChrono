using System;
using System.Collections.Generic;

namespace VisionAiChrono.Application.Dtos.PipelineRun
{
    /// <summary>
    /// One detection outcome: which media file, which task, and what was detected.
    /// </summary>
    public class DetectionOutcomeDTO
    {
        public string MediaFile { get; set; } = string.Empty;
        public int TaskIndex { get; set; }
        public string Task { get; set; } = string.Empty;

        /// <summary>"image" or "video".
        /// </summary>
        public string? ResultType { get; set; }

        /// <summary>Number of objects found (0 means nothing detected).
        /// </summary>
        public int Detections { get; set; }

        public double? Confidence { get; set; }
        public string Status { get; set; } = string.Empty;
        public string? Error { get; set; }
        public DateTime ProcessedAt { get; set; }
        public string ResultJson { get; set; } = string.Empty;
    }

    public class PipelineExecutionSummaryDTO
    {
        public Guid RunId { get; set; }
        public string Status { get; set; } = string.Empty;
        public int TotalExecutions { get; set; }
        public int Succeeded { get; set; }
        public int Failed { get; set; }
        public int TotalDetections { get; set; }
        public double? AverageConfidence { get; set; }
        public bool HasExport { get; set; }
        public string? ExportFileName { get; set; }
        public DateTime? CompletedAt { get; set; }
        public ICollection<DetectionOutcomeDTO> Results { get; set; } = new List<DetectionOutcomeDTO>();
    }
}
