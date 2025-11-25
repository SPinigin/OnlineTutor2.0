using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using OnlineTutor2.Data;
using OnlineTutor2.Data.Repositories;
using OnlineTutor2.Models;
using OnlineTutor2.Services;
using OnlineTutor2.ViewModels;

namespace OnlineTutor2.Controllers
{
    /// <summary>
    /// Главный контроллер администратора - только Dashboard
    /// Остальные функции вынесены в отдельные контроллеры в папке Admin
    /// </summary>
    [Authorize(Roles = ApplicationRoles.Admin)]
    public class AdminController : Controller
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly IAuditLogService _auditLogService;
        private readonly IStudentRepository _studentRepository;
        private readonly ITeacherRepository _teacherRepository;
        private readonly IClassRepository _classRepository;
        private readonly ISpellingTestRepository _spellingTestRepository;
        private readonly IRegularTestRepository _regularTestRepository;
        private readonly IPunctuationTestRepository _punctuationTestRepository;
        private readonly IOrthoeopyTestRepository _orthoeopyTestRepository;
        private readonly ISpellingTestResultRepository _spellingTestResultRepository;
        private readonly IPunctuationTestResultRepository _punctuationTestResultRepository;
        private readonly IRegularTestResultRepository _regularTestResultRepository;
        private readonly IOrthoeopyTestResultRepository _orthoeopyTestResultRepository;
        private readonly IStatisticsRepository _statisticsRepository;

        public AdminController(
            UserManager<ApplicationUser> userManager,
            IAuditLogService auditLogService,
            IStudentRepository studentRepository,
            ITeacherRepository teacherRepository,
            IClassRepository classRepository,
            ISpellingTestRepository spellingTestRepository,
            IRegularTestRepository regularTestRepository,
            IPunctuationTestRepository punctuationTestRepository,
            IOrthoeopyTestRepository orthoeopyTestRepository,
            ISpellingTestResultRepository spellingTestResultRepository,
            IPunctuationTestResultRepository punctuationTestResultRepository,
            IRegularTestResultRepository regularTestResultRepository,
            IOrthoeopyTestResultRepository orthoeopyTestResultRepository,
            IStatisticsRepository statisticsRepository)
        {
            _userManager = userManager;
            _auditLogService = auditLogService;
            _studentRepository = studentRepository;
            _teacherRepository = teacherRepository;
            _classRepository = classRepository;
            _spellingTestRepository = spellingTestRepository;
            _regularTestRepository = regularTestRepository;
            _punctuationTestRepository = punctuationTestRepository;
            _orthoeopyTestRepository = orthoeopyTestRepository;
            _spellingTestResultRepository = spellingTestResultRepository;
            _punctuationTestResultRepository = punctuationTestResultRepository;
            _regularTestResultRepository = regularTestResultRepository;
            _orthoeopyTestResultRepository = orthoeopyTestResultRepository;
            _statisticsRepository = statisticsRepository;
        }

        // GET: Admin - Dashboard
        public async Task<IActionResult> Index()
        {
            // Получаем статистику через репозиторий
            var stats = new AdminDashboardViewModel
            {
                TotalUsers = await _statisticsRepository.GetTotalUsersCountAsync(),
                TotalStudents = await _statisticsRepository.GetTotalStudentsCountAsync(),
                TotalTeachers = await _statisticsRepository.GetTotalTeachersCountAsync(),
                TotalClasses = await _statisticsRepository.GetTotalClassesCountAsync(),
                TotalSpellingTests = await _statisticsRepository.GetTotalSpellingTestsCountAsync(),
                TotalRegularTests = await _statisticsRepository.GetTotalRegularTestsCountAsync(),
                TotalPunctuationTests = await _statisticsRepository.GetTotalPunctuationTestsCountAsync(),
                TotalOrthoeopyTests = await _statisticsRepository.GetTotalOrthoeopyTestsCountAsync(),
                TotalTestResults = await _statisticsRepository.GetTotalTestResultsCountAsync(),
                PendingTeachers = await _statisticsRepository.GetPendingTeachersCountAsync(),
                RecentUsers = await _statisticsRepository.GetRecentUsersAsync(5),
                RecentTests = await _statisticsRepository.GetRecentSpellingTestsAsync(5)
            };

            ViewBag.RecentLogs = await _auditLogService.GetLogsAsync(page: 1, pageSize: 5);

            return View(stats);
        }
    }
}
