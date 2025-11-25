using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using OnlineTutor2.Data;
using OnlineTutor2.Data.Repositories;
using OnlineTutor2.Models;

namespace OnlineTutor2.Infrastructure
{
    /// <summary>
    /// Базовый контроллер для тестов, содержащий общую функциональность
    /// </summary>
    [Authorize(Roles = ApplicationRoles.Teacher)]
    public abstract class BaseTestController : Controller
    {
        protected readonly IDatabaseConnection Db;
        protected readonly UserManager<ApplicationUser> UserManager;
        protected readonly ILogger Logger;
        protected readonly IClassRepository ClassRepository;

        protected BaseTestController(
            IDatabaseConnection db,
            UserManager<ApplicationUser> userManager,
            ILogger logger,
            IClassRepository classRepository)
        {
            Db = db;
            UserManager = userManager;
            Logger = logger;
            ClassRepository = classRepository;
        }

        /// <summary>
        /// Загружает список классов текущего учителя в ViewBag
        /// </summary>
        protected async Task LoadClassesAsync()
        {
            var currentUser = await UserManager.GetUserAsync(User);
            if (currentUser == null) return;

            var classes = await ClassRepository.GetByTeacherIdAsync(currentUser.Id);
            classes = classes.Where(c => c.IsActive).OrderBy(c => c.Name).ToList();
            
            ViewBag.Classes = new SelectList(classes, "Id", "Name");
        }

        /// <summary>
        /// Получает текущего пользователя-учителя
        /// </summary>
        protected async Task<ApplicationUser?> GetCurrentUserAsync()
        {
            return await UserManager.GetUserAsync(User);
        }

        /// <summary>
        /// Абстрактный метод для получения теста по ID (реализуется в наследниках)
        /// </summary>
        protected abstract Task<object?> GetTestByIdAsync(int testId);
    }
}

