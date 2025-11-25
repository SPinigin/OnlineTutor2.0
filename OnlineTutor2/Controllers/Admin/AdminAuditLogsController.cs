using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using OnlineTutor2.Data;
using OnlineTutor2.Data.Repositories;
using OnlineTutor2.Models;
using OnlineTutor2.Services;

namespace OnlineTutor2.Controllers.Admin
{
    /// <summary>
    /// Контроллер для управления журналом аудита администратором
    /// </summary>
    public class AdminAuditLogsController : AdminBaseController
    {
        public AdminAuditLogsController(
            IDatabaseConnection db,
            UserManager<ApplicationUser> userManager,
            RoleManager<IdentityRole> roleManager,
            ILogger<AdminAuditLogsController> logger,
            IAuditLogService auditLogService,
            IHttpContextAccessor httpContextAccessor,
            IStatisticsRepository statisticsRepository)
            : base(db, userManager, roleManager, logger, auditLogService, httpContextAccessor, statisticsRepository)
        {
        }

        // GET: Admin/AuditLogs
        public async Task<IActionResult> Index(
            DateTime? fromDate,
            DateTime? toDate,
            string? action,
            string? entityType,
            int page = 1)
        {
            const int pageSize = 50;

            var logs = await AuditLogService.GetLogsAsync(
                fromDate,
                toDate,
                null,
                action,
                entityType,
                page,
                pageSize
            );

            var totalCount = await AuditLogService.GetLogsCountAsync(
                fromDate,
                toDate,
                null,
                action,
                entityType
            );

            ViewBag.CurrentPage = page;
            ViewBag.TotalPages = (int)Math.Ceiling(totalCount / (double)pageSize);
            ViewBag.FromDate = fromDate;
            ViewBag.ToDate = toDate;
            ViewBag.Action = action;
            ViewBag.EntityType = entityType;

            // Получаем уникальные действия и типы сущностей для фильтров через SQL
            var actionsSql = "SELECT DISTINCT Action FROM AuditLogs ORDER BY Action";
            var actions = await Db.QueryAsync<dynamic>(actionsSql);
            ViewBag.Actions = actions.Select(a => (string)a.Action).ToList();

            var entityTypesSql = "SELECT DISTINCT EntityType FROM AuditLogs ORDER BY EntityType";
            var entityTypes = await Db.QueryAsync<dynamic>(entityTypesSql);
            ViewBag.EntityTypes = entityTypes.Select(e => (string)e.EntityType).ToList();

            return View(logs);
        }

        // POST: Admin/AuditLogs/ClearOld
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ClearOld(int daysToKeep = 90)
        {
            var adminId = GetAdminId();
            var adminName = GetAdminName();

            try
            {
                await AuditLogService.ClearOldLogsAsync(daysToKeep);

                await LogAdminActionAsync(
                    "Audit Logs Cleared",
                    AuditEntityTypes.System,
                    null,
                    $"Удалены логи старше {daysToKeep} дней"
                );

                SetSuccessMessage($"Старые логи (старше {daysToKeep} дней) успешно удалены!");
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "Ошибка при очистке старых логов");
                SetErrorMessage("Ошибка при очистке старых логов.");
            }

            return RedirectToAction(nameof(Index));
        }
    }
}

