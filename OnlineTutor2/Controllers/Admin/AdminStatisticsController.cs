using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using OnlineTutor2.Data;
using OnlineTutor2.Data.Repositories;
using OnlineTutor2.Models;
using OnlineTutor2.Services;
using OnlineTutor2.ViewModels;

namespace OnlineTutor2.Controllers.Admin
{
    /// <summary>
    /// Контроллер для статистики администратора
    /// </summary>
    public class AdminStatisticsController : AdminBaseController
    {
        private readonly IAuditLogRepository _auditLogRepository;

        public AdminStatisticsController(
            IDatabaseConnection db,
            UserManager<ApplicationUser> userManager,
            RoleManager<IdentityRole> roleManager,
            ILogger<AdminStatisticsController> logger,
            IAuditLogService auditLogService,
            IHttpContextAccessor httpContextAccessor,
            IStatisticsRepository statisticsRepository,
            IAuditLogRepository auditLogRepository)
            : base(db, userManager, roleManager, logger, auditLogService, httpContextAccessor, statisticsRepository)
        {
            _auditLogRepository = auditLogRepository;
        }

        // GET: Admin/Statistics
        public async Task<IActionResult> Index()
        {
            var stats = new AdminStatisticsViewModel();
            var now = DateTime.Now;
            var thirtyDaysAgo = now.AddDays(-30);

            // Регистрации пользователей за последние 30 дней через репозиторий
            var registrationsDict = await StatisticsRepository.GetUserRegistrationsByDateAsync(thirtyDaysAgo);
            // Заполняем пропущенные дни нулями
            for (var date = thirtyDaysAgo.Date; date <= now.Date; date = date.AddDays(1))
            {
                if (!registrationsDict.ContainsKey(date))
                {
                    registrationsDict[date] = 0;
                }
            }
            var sortedRegistrations = new SortedDictionary<DateTime, int>(registrationsDict);
            stats.UserRegistrationsByDate = new Dictionary<DateTime, int>(sortedRegistrations);

            // Тесты по типам через репозиторий
            stats.TestsByType = await StatisticsRepository.GetTestsByTypeAsync();

            // Результаты тестов за последние 30 дней через репозиторий
            var spellingResults = await StatisticsRepository.GetTestResultsByDateAsync(thirtyDaysAgo, "spelling");
            var regularResults = await StatisticsRepository.GetTestResultsByDateAsync(thirtyDaysAgo, "regular");
            var punctuationResults = await StatisticsRepository.GetTestResultsByDateAsync(thirtyDaysAgo, "punctuation");
            var orthoeopyResults = await StatisticsRepository.GetTestResultsByDateAsync(thirtyDaysAgo, "orthoeopy");

            // Объединяем результаты
            var allResultsDict = new Dictionary<DateTime, int>();
            foreach (var r in spellingResults) { allResultsDict[r.Key] = (allResultsDict.ContainsKey(r.Key) ? allResultsDict[r.Key] : 0) + r.Value; }
            foreach (var r in regularResults) { allResultsDict[r.Key] = (allResultsDict.ContainsKey(r.Key) ? allResultsDict[r.Key] : 0) + r.Value; }
            foreach (var r in punctuationResults) { allResultsDict[r.Key] = (allResultsDict.ContainsKey(r.Key) ? allResultsDict[r.Key] : 0) + r.Value; }
            foreach (var r in orthoeopyResults) { allResultsDict[r.Key] = (allResultsDict.ContainsKey(r.Key) ? allResultsDict[r.Key] : 0) + r.Value; }

            for (var date = thirtyDaysAgo.Date; date <= now.Date; date = date.AddDays(1))
            {
                if (!allResultsDict.ContainsKey(date))
                {
                    allResultsDict[date] = 0;
                }
            }
            
            var sortedResults = new SortedDictionary<DateTime, int>(allResultsDict);
            stats.TestResultsByDate = new Dictionary<DateTime, int>(sortedResults);

            // Средний балл по типам тестов через репозиторий
            stats.AverageScoresByType = await StatisticsRepository.GetAverageScoresByTypeAsync();

            // Действия администраторов через репозиторий
            stats.AdminActionsByType = await StatisticsRepository.GetAdminActionsByTypeAsync(thirtyDaysAgo);

            // Активность по дням недели через репозиторий
            stats.ActivityByDayOfWeek = await StatisticsRepository.GetActivityByDayOfWeekAsync(thirtyDaysAgo);

            // Активные/неактивные пользователи через репозиторий
            stats.ActiveUsers = await StatisticsRepository.GetActiveUsersCountAsync();
            stats.InactiveUsers = await StatisticsRepository.GetInactiveUsersCountAsync();

            // Топ учителей через репозиторий
            stats.TopTeachersByTests = await StatisticsRepository.GetTopTeachersByTestsAsync(5);

            // Топ студентов через репозиторий
            stats.TopStudentsByResults = await StatisticsRepository.GetTopStudentsByResultsAsync(5);

            return View(stats);
        }

        // GET: Admin/Statistics/SystemInfo
        public async Task<IActionResult> SystemInfo()
        {
            var systemInfo = new AdminSystemInfoViewModel
            {
                DatabaseInfo = new DatabaseInfoViewModel
                {
                    TotalUsers = await StatisticsRepository.GetTotalUsersCountAsync(),
                    TotalStudents = await StatisticsRepository.GetTotalStudentsCountAsync(),
                    TotalTeachers = await StatisticsRepository.GetTotalTeachersCountAsync(),
                    TotalClasses = await StatisticsRepository.GetTotalClassesCountAsync(),
                    TotalSpellingTests = await StatisticsRepository.GetTotalSpellingTestsCountAsync(),
                    TotalRegularTests = await StatisticsRepository.GetTotalRegularTestsCountAsync(),
                    TotalSpellingQuestions = await StatisticsRepository.GetTotalSpellingQuestionsCountAsync(),
                    TotalRegularQuestions = await StatisticsRepository.GetTotalRegularQuestionsCountAsync(),
                    TotalSpellingResults = await StatisticsRepository.GetTotalSpellingResultsCountAsync(),
                    TotalRegularResults = await StatisticsRepository.GetTotalRegularResultsCountAsync(),
                    TotalMaterials = await StatisticsRepository.GetTotalMaterialsCountAsync()
                }
            };

            return View(systemInfo);
        }
    }
}

