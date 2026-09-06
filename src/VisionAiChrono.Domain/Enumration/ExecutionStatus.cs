using System;

namespace VisionAiChrono.Domain.Enumration
{
    public enum ExecutionStatus
    {
        Pending = 0,
        Running = 1,
        Succeeded = 2,
        Failed = 3,
        Cancelled = 4
    }
}