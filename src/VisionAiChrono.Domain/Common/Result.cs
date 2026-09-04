using System;

namespace CleanArchitectureTemplate_Domain.Common
{
    public record Error(string Code, string Message)
    {
        public static readonly Error None = new(string.Empty, string.Empty);
    }

    public class Result
    {
        public bool IsSuccess { get; }
        public bool IsFailure => !IsSuccess;
        public Error Error { get; } 

        protected Result(bool isSuccess, Error error)
        {
            if (isSuccess && error != Error.None)
                throw new InvalidOperationException("Success result cannot have an error.");

            if (!isSuccess && error == Error.None)
                throw new InvalidOperationException("Failure result must have an error.");

            IsSuccess = isSuccess;
            Error = error;
        }

        public static Result Success() => new(true, Error.None);
        public static Result Failure(Error error) => new(false, error);
    }

    public class Result<T> : Result
    {
        private readonly T? _value;

      
        public T Value => IsSuccess
            ? _value!
            : throw new InvalidOperationException("The value of a failure result can not be accessed.");

        private Result(T value) : base(true, Error.None)
        {
            _value = value;
        }

        private Result(Error error) : base(false, error)
        {
            _value = default;
        }

        public static Result<T> Success(T value) => new(value);
        public new static Result<T> Failure(Error error) => new(error);

      
        public static implicit operator Result<T>(T value) => Success(value);
    }
}