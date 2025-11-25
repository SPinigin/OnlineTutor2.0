using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using OnlineTutor2.Data;
using OnlineTutor2.Data.Repositories;
using OnlineTutor2.Models;
using OnlineTutor2.Services;

namespace OnlineTutor2.Controllers.Admin
{
    /// <summary>
    /// Контроллер для экспорта данных администратором
    /// </summary>
    public class AdminExportController : AdminBaseController
    {
        private readonly IExportService _exportService;

        public AdminExportController(
            IDatabaseConnection db,
            UserManager<ApplicationUser> userManager,
            RoleManager<IdentityRole> roleManager,
            ILogger<AdminExportController> logger,
            IAuditLogService auditLogService,
            IHttpContextAccessor httpContextAccessor,
            IExportService exportService,
            IStatisticsRepository statisticsRepository)
            : base(db, userManager, roleManager, logger, auditLogService, httpContextAccessor, statisticsRepository)
        {
            _exportService = exportService;
        }

        // GET: Admin/Export
        public IActionResult Index()
        {
            return View();
        }

        // GET: Admin/Export/Users
        [HttpGet]
        public async Task<IActionResult> Users(string format = "excel")
        {
            try
            {
                var adminId = GetAdminId();
                var adminName = GetAdminName();

                byte[] fileData;
                string fileName;
                string contentType;

                if (format == "csv")
                {
                    fileData = await _exportService.ExportUsersToCSVAsync();
                    fileName = $"Пользователи_{DateTime.Now:yyyy-MM-dd}.csv";
                    contentType = "text/csv";
                }
                else
                {
                    fileData = await _exportService.ExportUsersToExcelAsync();
                    fileName = $"Пользователи_{DateTime.Now:yyyy-MM-dd}.xlsx";
                    contentType = "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet";
                }

                await LogAdminActionAsync(
                    "Export Users",
                    AuditEntityTypes.System,
                    null,
                    $"Произведен экспорт пользователей в формат {format.ToUpper()}"
                );

                return File(fileData, contentType, fileName);
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "Ошибка при экспорте пользователей");
                SetErrorMessage("Ошибка при экспорте пользователей.");
                return RedirectToAction("Index", "AdminUsers");
            }
        }

        // GET: Admin/Export/Teachers
        [HttpGet]
        public async Task<IActionResult> Teachers(string format = "excel")
        {
            try
            {
                var adminId = GetAdminId();
                var adminName = GetAdminName();

                byte[] fileData;
                string fileName;
                string contentType;

                if (format == "csv")
                {
                    fileData = await _exportService.ExportTeachersToCSVAsync();
                    fileName = $"Учителя_{DateTime.Now:yyyy-MM-dd}.csv";
                    contentType = "text/csv";
                }
                else
                {
                    fileData = await _exportService.ExportTeachersToExcelAsync();
                    fileName = $"Учителя_{DateTime.Now:yyyy-MM-dd}.xlsx";
                    contentType = "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet";
                }

                await LogAdminActionAsync(
                    "Export Teachers",
                    AuditEntityTypes.System,
                    null,
                    $"Произведен экспорт учителей в формат {format.ToUpper()}"
                );

                return File(fileData, contentType, fileName);
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "Ошибка при экспорте учителей");
                SetErrorMessage("Ошибка при экспорте учителей.");
                return RedirectToAction("Index", "AdminTeachers");
            }
        }

        // GET: Admin/Export/Students
        [HttpGet]
        public async Task<IActionResult> Students(string format = "excel")
        {
            try
            {
                var adminId = GetAdminId();
                var adminName = GetAdminName();

                byte[] fileData;
                string fileName;
                string contentType;

                if (format == "csv")
                {
                    fileData = await _exportService.ExportStudentsToCSVAsync();
                    fileName = $"Студенты_{DateTime.Now:yyyy-MM-dd}.csv";
                    contentType = "text/csv";
                }
                else
                {
                    fileData = await _exportService.ExportStudentsToExcelAsync();
                    fileName = $"Студенты_{DateTime.Now:yyyy-MM-dd}.xlsx";
                    contentType = "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet";
                }

                await LogAdminActionAsync(
                    "Export Students",
                    AuditEntityTypes.System,
                    null,
                    $"Произведен экспорт студентов в формат {format.ToUpper()}"
                );

                return File(fileData, contentType, fileName);
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "Ошибка при экспорте студентов");
                SetErrorMessage("Ошибка при экспорте студентов.");
                return RedirectToAction("Index", "AdminUsers");
            }
        }

        // GET: Admin/Export/Classes
        [HttpGet]
        public async Task<IActionResult> Classes(string format = "excel")
        {
            try
            {
                var adminId = GetAdminId();
                var adminName = GetAdminName();

                byte[] fileData;
                string fileName;
                string contentType;

                if (format == "csv")
                {
                    fileData = await _exportService.ExportClassesToCSVAsync();
                    fileName = $"Классы_{DateTime.Now:yyyy-MM-dd}.csv";
                    contentType = "text/csv";
                }
                else
                {
                    fileData = await _exportService.ExportClassesToExcelAsync();
                    fileName = $"Классы_{DateTime.Now:yyyy-MM-dd}.xlsx";
                    contentType = "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet";
                }

                await LogAdminActionAsync(
                    "Export Classes",
                    AuditEntityTypes.System,
                    null,
                    $"Произведен экспорт классов в формат {format.ToUpper()}"
                );

                return File(fileData, contentType, fileName);
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "Ошибка при экспорте классов");
                SetErrorMessage("Ошибка при экспорте классов.");
                return RedirectToAction("Index", "AdminClasses");
            }
        }

        // GET: Admin/Export/Tests
        [HttpGet]
        public async Task<IActionResult> Tests(string format = "excel")
        {
            try
            {
                var adminId = GetAdminId();
                var adminName = GetAdminName();

                byte[] fileData;
                string fileName;
                string contentType;

                if (format == "csv")
                {
                    fileData = await _exportService.ExportTestsToCSVAsync();
                    fileName = $"Тесты_{DateTime.Now:yyyy-MM-dd}.csv";
                    contentType = "text/csv";
                }
                else
                {
                    fileData = await _exportService.ExportTestsToExcelAsync();
                    fileName = $"Тесты_{DateTime.Now:yyyy-MM-dd}.xlsx";
                    contentType = "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet";
                }

                await LogAdminActionAsync(
                    "Export Tests",
                    AuditEntityTypes.System,
                    null,
                    $"Произведен экспорт тестов в формат {format.ToUpper()}"
                );

                return File(fileData, contentType, fileName);
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "Ошибка при экспорте тестов");
                SetErrorMessage("Ошибка при экспорте тестов.");
                return RedirectToAction("Index", "AdminTests");
            }
        }

        // GET: Admin/Export/TestResults
        [HttpGet]
        public async Task<IActionResult> TestResults(string format = "excel")
        {
            try
            {
                var adminId = GetAdminId();
                var adminName = GetAdminName();

                byte[] fileData;
                string fileName;
                string contentType;

                if (format == "csv")
                {
                    fileData = await _exportService.ExportTestResultsToCSVAsync();
                    fileName = $"Результаты_{DateTime.Now:yyyy-MM-dd}.csv";
                    contentType = "text/csv";
                }
                else
                {
                    fileData = await _exportService.ExportTestResultsToExcelAsync();
                    fileName = $"Результаты_{DateTime.Now:yyyy-MM-dd}.xlsx";
                    contentType = "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet";
                }

                await LogAdminActionAsync(
                    "Export Test Results",
                    AuditEntityTypes.System,
                    null,
                    $"Произведен экспорт результатов в формат {format.ToUpper()}"
                );

                return File(fileData, contentType, fileName);
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "Ошибка при экспорте результатов");
                SetErrorMessage("Ошибка при экспорте результатов.");
                return RedirectToAction("Index", "AdminTestResults");
            }
        }

        // GET: Admin/Export/AuditLogs
        [HttpGet]
        public async Task<IActionResult> AuditLogs(string format = "excel")
        {
            try
            {
                var adminId = GetAdminId();
                var adminName = GetAdminName();

                byte[] fileData;
                string fileName;
                string contentType;

                if (format == "csv")
                {
                    fileData = await _exportService.ExportAuditLogsToCSVAsync();
                    fileName = $"Logs_{DateTime.Now:yyyy-MM-dd}.csv";
                    contentType = "text/csv";
                }
                else
                {
                    fileData = await _exportService.ExportAuditLogsToExcelAsync();
                    fileName = $"Logs_{DateTime.Now:yyyy-MM-dd}.xlsx";
                    contentType = "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet";
                }

                await LogAdminActionAsync(
                    "Export Audit Logs",
                    AuditEntityTypes.System,
                    null,
                    $"Произведен экспорт журнала в формат {format.ToUpper()}"
                );

                return File(fileData, contentType, fileName);
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "Ошибка при экспорте журнала");
                SetErrorMessage("Ошибка при экспорте журнала.");
                return RedirectToAction("Index", "AdminAuditLogs");
            }
        }

        // GET: Admin/Export/FullSystem
        [HttpGet]
        public async Task<IActionResult> FullSystem()
        {
            try
            {
                var adminId = GetAdminId();
                var adminName = GetAdminName();

                var fileData = await _exportService.ExportFullSystemToExcelAsync();
                var fileName = $"FullSystem_Export_{DateTime.Now:yyyy-MM-dd}.xlsx";
                var contentType = "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet";

                await LogAdminActionAsync(
                    "Export Full System",
                    AuditEntityTypes.System,
                    null,
                    "Exported full system data to Excel"
                );

                return File(fileData, contentType, fileName);
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "Error exporting full system");
                SetErrorMessage("Ошибка при экспорте всех данных.");
                return RedirectToAction("Index", "Admin");
            }
        }
    }
}

