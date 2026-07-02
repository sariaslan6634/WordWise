using System.Net;
using System.Text.Json;
using WordWise.Application.Common.Exceptions;
using WordWise.Application.Common.Models;

namespace WordWise.WebAPI.Middleware
{
    public class ExceptionMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly ILogger<ExceptionMiddleware> _logger;

        private static readonly JsonSerializerOptions _jsonOptions = new()
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        };
        public ExceptionMiddleware(RequestDelegate next, ILogger<ExceptionMiddleware> logger)
        {
            _next = next;
            _logger = logger;
        }

        public async Task InvokeAsync(HttpContext context)
        {
            try
            {
                await _next(context);
            }
            catch (Exception ex)
            {
                await HandleExceptionAsync(context, ex);
            }
        }

        private async Task HandleExceptionAsync(HttpContext context, Exception exception)
        {
            var (statusCode, response) = exception switch
            {
                NotFoundException ex => (
                    HttpStatusCode.NotFound,
                    ApiResponse<object>.Fail(ex.Message, 404)
                ),

                Application.Common.Exceptions.ValidationException ex => (
                    HttpStatusCode.BadRequest,
                    ApiResponse<object>.Fail(
                        ex.Message,
                        400,
                        ex.Errors.SelectMany(e => e.Value).ToList()
                    )
                ),

                UnauthorizedException ex => (
                    HttpStatusCode.Unauthorized,
                    ApiResponse<object>.Fail(ex.Message, 401)
                ),

                ForbiddenException ex => (
                    HttpStatusCode.Forbidden,
                    ApiResponse<object>.Fail(ex.Message, 403)
                ),

                BusinessException ex => (
                    HttpStatusCode.UnprocessableEntity,
                    ApiResponse<object>.Fail(ex.Message, 422)
                ),

                ConflictException ex => (
                    HttpStatusCode.Conflict,
                    ApiResponse<object>.Fail(ex.Message, 409)
                ),

                _ => (
                    HttpStatusCode.InternalServerError,
                    ApiResponse<object>.Fail(
                        "Beklenmeyen bir hata oluştu.",
                        500
                    )
                )
            };

            if (statusCode == HttpStatusCode.InternalServerError)
                _logger.LogError(exception, "Yakalanamayan istisna (Hata). Yol: {Path}", context.Request.Path);
            else
                _logger.LogWarning(exception, "Yakalanan istisna (Uyarı). Yol: {Path}", context.Request.Path);

            context.Response.ContentType = "application/json";
            context.Response.StatusCode = (int)statusCode;

            var json = JsonSerializer.Serialize(response, _jsonOptions);
            await context.Response.WriteAsync(json);
        }
    }
    public static class ExceptionMiddlewareExtensions
    {
        public static IApplicationBuilder UseExceptionMiddleware(this IApplicationBuilder app)
            => app.UseMiddleware<ExceptionMiddleware>();
    }
}
