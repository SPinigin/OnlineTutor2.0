using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using OnlineTutor2.Data;
using OnlineTutor2.Data.Repositories;
using OnlineTutor2.Infrastructure;
using OnlineTutor2.Models;
using OnlineTutor2.Services;

namespace OnlineTutor2.Controllers.Admin
{
    /// <summary>
    /// Базовый контроллер для всех админ-контроллеров с общими методами
    /// </summary>
    [Authorize(Roles = ApplicationRoles.Admin)]
    public abstract class AdminBaseController : BaseController
    {
        protected readonly IDatabaseConnection Db;
        protected readonly IStatisticsRepository StatisticsRepository;
        protected readonly RoleManager<IdentityRole> RoleManager;
        protected readonly ILogger Logger;
        protected readonly IAuditLogService AuditLogService;
        protected readonly IHttpContextAccessor HttpContextAccessor;

        protected AdminBaseController(
            IDatabaseConnection db,
            UserManager<ApplicationUser> userManager,
            RoleManager<IdentityRole> roleManager,
            ILogger logger,
            IAuditLogService auditLogService,
            IHttpContextAccessor httpContextAccessor,
            IStatisticsRepository statisticsRepository)
            : base(userManager)
        {
            Db = db;
            StatisticsRepository = statisticsRepository;
            RoleManager = roleManager;
            Logger = logger;
            AuditLogService = auditLogService;
            HttpContextAccessor = httpContextAccessor;
        }

        /// <summary>
        /// Получает IP-адрес текущего запроса
        /// </summary>
        protected string GetIpAddress()
        {
            return HttpContextAccessor.HttpContext?.Connection?.RemoteIpAddress?.ToString() ?? "Unknown";
        }

        /// <summary>
        /// Получает ID текущего администратора
        /// </summary>
        protected string? GetAdminId()
        {
            return UserManager.GetUserId(User);
        }

        /// <summary>
        /// Получает имя текущего администратора
        /// </summary>
        protected string GetAdminName()
        {
            return User.Identity?.Name ?? "Unknown";
        }

        /// <summary>
        /// Логирует действие администратора
        /// </summary>
        protected async Task LogAdminActionAsync(
            string action,
            string entityType,
            string? entityId,
            string details)
        {
            var adminId = GetAdminId();
            var adminName = GetAdminName();

            if (adminId != null)
            {
                await AuditLogService.LogActionAsync(
                    adminId,
                    adminName,
                    action,
                    entityType,
                    entityId,
                    details,
                    GetIpAddress()
                );
            }
        }
    }
}

