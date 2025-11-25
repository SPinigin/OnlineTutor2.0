using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using NLog;

namespace OnlineTutor2.Infrastructure
{
    /// <summary>
    /// Атрибут для автоматической обработки исключений в контроллерах
    /// </summary>
    public class ExceptionHandlingAttribute : ExceptionFilterAttribute
    {
        private static readonly Logger Logger = LogManager.GetCurrentClassLogger();

        public override void OnException(ExceptionContext context)
        {
            var exception = context.Exception;
            var controllerName = context.RouteData.Values["controller"]?.ToString();
            var actionName = context.RouteData.Values["action"]?.ToString();

            // Логируем исключение
            Logger.Error(exception, 
                "Исключение в контроллере {Controller}, действие {Action}. Path: {Path}",
                controllerName, actionName, context.HttpContext.Request.Path);

            // Определяем тип ответа
            if (context.HttpContext.Request.Headers["Accept"].ToString().Contains("application/json"))
            {
                // Для API запросов возвращаем JSON
                context.Result = new JsonResult(new
                {
                    error = new
                    {
                        message = "Произошла ошибка при обработке запроса",
                        statusCode = 500
                    }
                })
                {
                    StatusCode = 500
                };
            }
            else
            {
                // Для обычных запросов перенаправляем на страницу ошибки
                context.Result = new RedirectToActionResult("Error", "Home", new { statusCode = 500 });
            }

            context.ExceptionHandled = true;
        }
    }
}





