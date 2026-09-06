using System;
using System.Collections.Generic;
using CleanArchitectureTemplate_Domain.Common;

namespace VisionAiChrono.Domain.Model.Entity
{
    public class Video : BaseEntity
    {
        public Video()
        {
            PipelineRunVideos = new List<PipelineRunVideo>();
            Favorites = new List<Favorite>();
            CreatedAt = DateTime.UtcNow;
        }

        public string FileName { get; set; } = null!;
        public string FilePath { get; set; } = null!; // path or URI to storage location
        public long? SizeBytes { get; set; }
        public TimeSpan? Duration { get; set; }
        public string? ContentType { get; set; }

        public ICollection<PipelineRunVideo> PipelineRunVideos { get; set; }

        // Users who favorited this video
        public ICollection<Favorite> Favorites { get; set; }
    }
}