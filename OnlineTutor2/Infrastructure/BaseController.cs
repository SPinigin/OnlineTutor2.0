using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using OnlineTutor2.Models;

namespace OnlineTutor2.Infrastructure
{
    /// <summary>
    /// Базовый контроллер с общими методами для всех контроллеров
    /// </summary>
    public abstract class BaseController : Controller
    {
        protected readonly UserManager<ApplicationUser> UserManager;

        protected BaseController(UserManager<ApplicationUser> userManager)
        {
            UserManager = userManager;
        }

        /// <summary>
        /// Получает текущего пользователя
        /// </summary>
        protected async Task<ApplicationUser?> GetCurrentUserAsync()
        {
            return await UserManager.GetUserAsync(User);
        }

        /// <summary>
        /// Получает ID текущего пользователя
        /// </summary>
        protected string? GetCurrentUserId()
        {
            return UserManager.GetUserId(User);
        }

        /// <summary>
        /// Проверяет, является ли пользователь администратором
        /// </summary>
        protected async Task<bool> IsAdminAsync()
        {
            var user = await GetCurrentUserAsync();
            if (user == null) return false;

            return await UserManager.IsInRoleAsync(user, ApplicationRoles.Admin);
        }

        /// <summary>
        /// Проверяет, является ли пользователь учителем
        /// </summary>
        protected async Task<bool> IsTeacherAsync()
        {
            var user = await GetCurrentUserAsync();
            if (user == null) return false;

            return await UserManager.IsInRoleAsync(user, ApplicationRoles.Teacher);
        }

        /// <summary>
        /// Проверяет, является ли пользователь студентом
        /// </summary>
        protected async Task<bool> IsStudentAsync()
        {
            var user = await GetCurrentUserAsync();
            if (user == null) return false;

            return await UserManager.IsInRoleAsync(user, ApplicationRoles.Student);
        }

        /// <summary>
        /// Устанавливает сообщение об успехе
        /// </summary>
        protected void SetSuccessMessage(string message)
        {
            TempData["SuccessMessage"] = message;
        }

        /// <summary>
        /// Устанавливает сообщение об ошибке
        /// </summary>
        protected void SetErrorMessage(string message)
        {
            TempData["ErrorMessage"] = message;
        }

        /// <summary>
        /// Устанавливает информационное сообщение
        /// </summary>
        protected void SetInfoMessage(string message)
        {
            TempData["InfoMessage"] = message;
        }

        /// <summary>
        /// Обрабатывает исключение и возвращает результат с ошибкой
        /// </summary>
        protected IActionResult HandleError(Exception ex, string? redirectAction = null, string? redirectController = null)
        {
            // Логируем исключение
            var logger = HttpContext.RequestServices.GetService<ILogger>();
            logger?.LogError(ex, "Ошибка в контроллере {Controller}", GetType().Name);

            // Если указано перенаправление, используем его
            if (!string.IsNullOrEmpty(redirectAction))
            {
                SetErrorMessage("Произошла ошибка при обработке запроса. Пожалуйста, попробуйте позже.");
                if (!string.IsNullOrEmpty(redirectController))
                {
                    return RedirectToAction(redirectAction, redirectController);
                }
                return RedirectToAction(redirectAction);
            }

            // Иначе возвращаем страницу ошибки
            return RedirectToAction("Error", "Home", new { statusCode = 500 });
        }
    }
}

