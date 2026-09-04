using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CleanArchitectureTemplate_Application.Dtos
{
    public class ApiResponse<T>
    {
        public bool Success { get; set; } = true;
        public string Message { get; set; } = string.Empty;
        public T Data { get; set; } = default!;

        public ApiResponse() { }

        public ApiResponse(T data, string message = "", bool success = true)
        {
            Success = success;
            Message = message;
            Data = data;
        }
    }

    public class ApiResponse
    {
        public bool Success { get; set; } = true;
        public string Message { get; set; } = string.Empty;

        public ApiResponse() { }

        public ApiResponse(string message = "", bool success = true)
        {
            Success = success;
            Message = message;
        }
    }
}
