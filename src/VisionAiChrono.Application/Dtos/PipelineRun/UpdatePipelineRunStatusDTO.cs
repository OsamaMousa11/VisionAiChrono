using VisionAiChrono.Domain.Enumration;

namespace VisionAiChrono.Application.Dtos.PipelineRun
{
    public class UpdatePipelineRunStatusDTO
    {
        public ExecutionStatus Status { get; set; }
    }
}
