using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CleanArchitectureTemplate_Application.Exceptions
{
    public sealed class ExternalServiceException : AppException
    {
        public string ServiceName { get; }

        public ExternalServiceException(string serviceName, string message)
            : base($"[{serviceName}] {message}")
        {
            ServiceName = serviceName;
        }

        public ExternalServiceException(string serviceName, string message, Exception inner)
            : base($"[{serviceName}] {message}", inner)
        {
            ServiceName = serviceName;
        }
    }
}
