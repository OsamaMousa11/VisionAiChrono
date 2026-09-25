using System;

namespace VisionAiChrono.Application.ServiceContract
{
    public interface IPipelineExecutionQueue
    {
        string QueueExecution(Guid pipelineRunId);
    }
}
