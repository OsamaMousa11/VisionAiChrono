using System;

namespace CleanArchitectureTemplate_Application.Exceptions
{
    public class ValidationException : AppException
    {
        public ValidationException(string message) : base(message) { }
    }
}
