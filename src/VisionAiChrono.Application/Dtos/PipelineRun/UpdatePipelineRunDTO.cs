using System.Collections.Generic;

namespace VisionAiChrono.Application.Dtos.PipelineRun
{
    public class UpdatePipelineRunDTO
    {
        public Guid? PipelineId { get; set; }
        public List<Guid>? RemoveMediaIds { get; set; }
        public List<RunModelSelectionDTO>? Models { get; set; }
    }
}
