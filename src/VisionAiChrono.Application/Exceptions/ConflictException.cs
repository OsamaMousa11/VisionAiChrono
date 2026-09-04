using System;

namespace CleanArchitectureTemplate_Application.Exceptions
{
    public class ConflictException : AppException
    {
        public ConflictException(string message) : base(message) { }
    }
}
