using System;

namespace CleanArchitectureTemplate_Application.Exceptions
{
    public class BadRequestException : AppException
    {
        public BadRequestException(string message) : base(message) { }
    }
}
