using System;
using System.Collections.Generic;

namespace VisionAiChrono.Application.Dtos.PipelineRun
{
    public class CreateRunWithMediaDTO
    {
        public Guid PipelineId { get; set; }

        /// <summary>
        /// 0 = person, 1 = weapon, 2 = fire.
        /// </summary>
        public List<int> Tasks { get; set; } = new List<int>();
    }
}
