using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System;
using System.Net;
using System.Text.Json;
using System.Threading.Tasks;
using LMS.Core.Exception;

namespace LMS.PL.Middleware
{
    public class ExceptionHandlingMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly ILogger<ExceptionHandlingMiddleware> _logger;
        private readonly IHostEnvironment _env;

        public ExceptionHandlingMiddleware(RequestDelegate next, ILogger<ExceptionHandlingMiddleware> logger, IHostEnvironment env)
        {
            _next = next;
            _logger = logger;
            _env = env;
        }

        public async Task InvokeAsync(HttpContext context)
        {
            try
            {
                await _next(context);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An unhandled exception occurred: {Message}", ex.Message);
                await HandleExceptionAsync(context, ex);
            }
        }

        private Task HandleExceptionAsync(HttpContext context, Exception exception)
        {
            context.Response.ContentType = "application/json";

            var statusCode = HttpStatusCode.InternalServerError;
            var message = "An unexpected error occurred. Please try again later.";

            switch (exception)
            {
                case NotFoundException notFoundEx:
                    statusCode = HttpStatusCode.NotFound;
                    message = notFoundEx.Message;
                    break;
                case UnauthorizedAccessException unauthorizedEx:
                    statusCode = HttpStatusCode.Forbidden;
                    message = unauthorizedEx.Message;
                    break;
                case ArgumentException argEx:
                    statusCode = HttpStatusCode.BadRequest;
                    message = argEx.Message;
                    break;
                case InvalidOperationException invalidOpEx:
                    statusCode = HttpStatusCode.BadRequest;
                    message = invalidOpEx.Message;
                    break;
            }

            context.Response.StatusCode = (int)statusCode;

            var response = new ErrorResponse
            {
                StatusCode = context.Response.StatusCode,
                Message = message,
                Detail = _env.IsDevelopment() ? exception.ToString() : null
            };

            var jsonOptions = new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase
            };

            var jsonString = JsonSerializer.Serialize(response, jsonOptions);
            return context.Response.WriteAsync(jsonString);
        }
    }

    public class ErrorResponse
    {
        public int StatusCode { get; set; }
        public string Message { get; set; } = null!;
        public string? Detail { get; set; }
    }
}
