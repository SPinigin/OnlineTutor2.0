using System.Net;
using System.Text.Json;
using Microsoft.AspNetCore.Diagnostics;
using NLog;

namespace OnlineTutor2.Infrastructure
{
    /// <summary>
    /// Глобальный обработчик исключений для приложения
    /// </summary>
    public class GlobalExceptionHandlerMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly ILogger<GlobalExceptionHandlerMiddleware> _logger;
        private static readonly Logger NLogLogger = LogManager.GetCurrentClassLogger();

        public GlobalExceptionHandlerMiddleware(RequestDelegate next, ILogger<GlobalExceptionHandlerMiddleware> logger)
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

        private static Task HandleExceptionAsync(HttpContext context, Exception exception)
        {
            var code = HttpStatusCode.InternalServerError;
            var result = string.Empty;

            // Логируем исключение
            NLogLogger.Error(exception, "Необработанное исключение: {Message}. Path: {Path}", 
                exception.Message, context.Request.Path);

            // Определяем код ответа в зависимости от типа исключения
            switch (exception)
            {
                case UnauthorizedAccessException:
                    code = HttpStatusCode.Unauthorized;
                    break;
                case ArgumentNullException:
                case ArgumentException:
                    code = HttpStatusCode.BadRequest;
                    break;
                case KeyNotFoundException:
                case FileNotFoundException:
                    code = HttpStatusCode.NotFound;
                    break;
            }

            // Если это API запрос, возвращаем JSON
            if (context.Request.Path.StartsWithSegments("/api"))
            {
                result = JsonSerializer.Serialize(new
                {
                    error = new
                    {
                        message = exception.Message,
                        statusCode = (int)code
                    }
                });

                context.Response.ContentType = "application/json";
                context.Response.StatusCode = (int)code;
                return context.Response.WriteAsync(result);
            }

            // Для обычных запросов перенаправляем на страницу ошибки
            // Проверяем, что ответ еще не был отправлен
            if (!context.Response.HasStarted)
            {
                context.Response.StatusCode = (int)code;
                context.Response.Redirect($"/Home/Error?statusCode={(int)code}");
            }
            return Task.CompletedTask;
        }
    }

    /// <summary>
    /// Extension методы для регистрации middleware
    /// </summary>
    public static class GlobalExceptionHandlerMiddlewareExtensions
    {
        public static IApplicationBuilder UseGlobalExceptionHandler(this IApplicationBuilder builder)
        {
            return builder.UseMiddleware<GlobalExceptionHandlerMiddleware>();
        }
    }
}

