using System;

namespace CleanArchitectureTemplate_Application.Exceptions
{
    public class ForbiddenException : AppException
    {
        public ForbiddenException(string message) : base(message) { }
    }
}
