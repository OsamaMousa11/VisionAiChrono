using System;
using System.Collections.Generic;

namespace VisionAiChrono.Application.Dtos.PipelineRun
{
    public class RunExecutionResponseDTO
    {
        public Guid RunId { get; set; }
        public string? JobId { get; set; }
        public string Status { get; set; } = string.Empty;

        public int UploadedCount { get; set; }
        public int TaskCount { get; set; }
        public int TotalExecutions { get; set; }
        public int Succeeded { get; set; }
        public int Failed { get; set; }

        /// <summary>Total objects detected across all media and tasks.</summary>
        public int TotalDetections { get; set; }

        public double? AverageConfidence { get; set; }

        public bool HasExport { get; set; }
        public string? ExportFileName { get; set; }
        public DateTime? CompletedAt { get; set; }

        public string Message { get; set; } = string.Empty;

        public ICollection<UploadedMediaResultDTO> UploadedMedia { get; set; } = new List<UploadedMediaResultDTO>();

        /// <summary>
        /// What the model actually detected, per media file and per task.
        /// </summary>
        public ICollection<DetectionOutcomeDTO> Results { get; set; } = new List<DetectionOutcomeDTO>();
    }
}
