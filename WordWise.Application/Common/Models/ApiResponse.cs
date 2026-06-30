using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WordWise.Application.Common.Models
{
    public class ApiResponse<T>
    {
        public bool Success { get; set; }
        public string Message { get; set; } = string.Empty;
        public T? Data { get; set; }
        public List<string> Errors { get; set; } = new();
        public int StatusCode { get; set; }

        public static ApiResponse<T> Ok(T data, string message = "Başarılı.")
            => new()
            {
                Success = true,
                Message = message,
                Data = data,
                StatusCode = 200
            };

        public static ApiResponse<T> Created(T data, string message = "Başarıyla Oluşturuldu.")
            => new()
            {
                Success = true,
                Message = message,
                Data = data,
                StatusCode = 201
            };

        public static ApiResponse<T> Fail(string message, int statusCode, List<string>? errors = null)
            => new()
            {
                Success = false,
                Message = message,
                Data = default,
                Errors = errors ?? new List<string>(),
                StatusCode = statusCode
            };

        public static ApiResponse<T> Fail(string message, int statusCode, string error)
            => Fail(message, statusCode, new List<string> { error });
    }
    public class ApiResponse : ApiResponse<object>
    {
        public static ApiResponse OkNoData(string message = "Başarılı.")
            => new()
            {
                Success = true,
                Message = message,
                Data = null,
                StatusCode = 200
            };
    }
}
