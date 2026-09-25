using System;
using System.Collections.Generic;

namespace CleanArchitectureTemplate_Application.Exceptions
{
    public class ValidationException : AppException
    {
        public IReadOnlyDictionary<string, string[]> Errors { get; }

        public ValidationException(string message)
            : base(message)
        {
            Errors = new Dictionary<string, string[]>();
        }

        public ValidationException(IReadOnlyDictionary<string, string[]> errors)
            : base("Validation failed.")
        {
            Errors = errors;
        }

        public ValidationException(string message, IReadOnlyDictionary<string, string[]> errors)
            : base(message)
        {
            Errors = errors;
        }
    }
}
