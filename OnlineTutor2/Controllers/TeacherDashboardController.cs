using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using OnlineTutor2.Data;
using OnlineTutor2.Models;
using OnlineTutor2.ViewModels;

namespace OnlineTutor2.Controllers
{
    [Authorize(Roles = ApplicationRoles.Teacher)]
    public class TeacherDashboardController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly ILogger<TeacherDashboardController> _logger;

        public TeacherDashboardController(
            ApplicationDbContext context,
            UserManager<ApplicationUser> userManager,
            ILogger<TeacherDashboardController> logger)
        {
            _context = context;
            _userManager = userManager;
            _logger = logger;
        }

        // GET: TeacherDashboard
        public async Task<IActionResult> Index()
        {
            var currentUser = await _userManager.GetUserAsync(User);

            // Получаем все активные тесты учителя
            var spellingTests = await _context.SpellingTests
                .Where(st => st.TeacherId == currentUser.Id && st.IsActive)
                .Include(st => st.Class)
                .Include(st => st.SpellingTestResults)
                    .ThenInclude(tr => tr.Student)
                        .ThenInclude(s => s.User)
                .OrderByDescending(st => st.CreatedAt)
                .Take(20)
                .ToListAsync();

            var punctuationTests = await _context.PunctuationTests
                .Where(pt => pt.TeacherId == currentUser.Id && pt.IsActive)
                .Include(pt => pt.Class)
                .Include(pt => pt.PunctuationTestResults)
                    .ThenInclude(tr => tr.Student)
                        .ThenInclude(s => s.User)
                .OrderByDescending(pt => pt.CreatedAt)
                .Take(20)
                .ToListAsync();

            var orthoeopyTests = await _context.OrthoeopyTests
                .Where(ot => ot.TeacherId == currentUser.Id && ot.IsActive)
                .Include(ot => ot.Class)
                .Include(ot => ot.OrthoeopyTestResults)
                    .ThenInclude(tr => tr.Student)
                        .ThenInclude(s => s.User)
                .OrderByDescending(ot => ot.CreatedAt)
                .Take(20)
                .ToListAsync();

            var regularTests = await _context.RegularTests
                .Where(rt => rt.TeacherId == currentUser.Id && rt.IsActive)
                .Include(rt => rt.Class)
                .Include(rt => rt.RegularTestResults)
                    .ThenInclude(tr => tr.Student)
                        .ThenInclude(s => s.User)
                .OrderByDescending(rt => rt.CreatedAt)
                .Take(20)
                .ToListAsync();

            var viewModel = new TeacherDashboardViewModel
            {
                Teacher = currentUser,
                SpellingTests = spellingTests,
                PunctuationTests = punctuationTests,
                OrthoeopyTests = orthoeopyTests,
                RegularTests = regularTests
            };

            return View(viewModel);
        }

        // GET: TeacherDashboard/GetRecentActivity
        [HttpGet]
        public async Task<IActionResult> GetRecentActivity()
        {
            var currentUser = await _userManager.GetUserAsync(User);

            // Получаем последние 50 активностей
            var activities = new List<TestActivityViewModel>();

            // Spelling
            var spellingActivities = await _context.SpellingTestResults
                .Include(tr => tr.SpellingTest)
                .Include(tr => tr.Student)
                    .ThenInclude(s => s.User)
                .Where(tr => tr.SpellingTest.TeacherId == currentUser.Id)
                .OrderByDescending(tr => tr.CompletedAt ?? tr.StartedAt)
                .Take(50)
                .Select(tr => new TestActivityViewModel
                {
                    TestId = tr.SpellingTestId,
                    TestTitle = tr.SpellingTest.Title,
                    TestType = "spelling",
                    StudentId = tr.StudentId,
                    StudentName = tr.Student.User.FullName,
                    Status = tr.IsCompleted ? "completed" : "in_progress",
                    Score = tr.Score,
                    MaxScore = tr.MaxScore,
                    Percentage = tr.Percentage,
                    StartedAt = tr.StartedAt,
                    CompletedAt = tr.CompletedAt,
                    LastActivityAt = tr.CompletedAt ?? tr.StartedAt
                })
                .ToListAsync();

            activities.AddRange(spellingActivities);

            // Punctuation
            var punctuationActivities = await _context.PunctuationTestResults
                .Include(tr => tr.PunctuationTest)
                .Include(tr => tr.Student)
                    .ThenInclude(s => s.User)
                .Where(tr => tr.PunctuationTest.TeacherId == currentUser.Id)
                .OrderByDescending(tr => tr.CompletedAt ?? tr.StartedAt)
                .Take(50)
                .Select(tr => new TestActivityViewModel
                {
                    TestId = tr.PunctuationTestId,
                    TestTitle = tr.PunctuationTest.Title,
                    TestType = "punctuation",
                    StudentId = tr.StudentId,
                    StudentName = tr.Student.User.FullName,
                    Status = tr.IsCompleted ? "completed" : "in_progress",
                    Score = tr.Score,
                    MaxScore = tr.MaxScore,
                    Percentage = tr.Percentage,
                    StartedAt = tr.StartedAt,
                    CompletedAt = tr.CompletedAt,
                    LastActivityAt = tr.CompletedAt ?? tr.StartedAt
                })
                .ToListAsync();

            activities.AddRange(punctuationActivities);

            // Orthoepy
            var orthoeopyActivities = await _context.OrthoeopyTestResults
                .Include(tr => tr.OrthoeopyTest)
                .Include(tr => tr.Student)
                    .ThenInclude(s => s.User)
                .Where(tr => tr.OrthoeopyTest.TeacherId == currentUser.Id)
                .OrderByDescending(tr => tr.CompletedAt ?? tr.StartedAt)
                .Take(50)
                .Select(tr => new TestActivityViewModel
                {
                    TestId = tr.OrthoeopyTestId,
                    TestTitle = tr.OrthoeopyTest.Title,
                    TestType = "orthoepy",
                    StudentId = tr.StudentId,
                    StudentName = tr.Student.User.FullName,
                    Status = tr.IsCompleted ? "completed" : "in_progress",
                    Score = tr.Score,
                    MaxScore = tr.MaxScore,
                    Percentage = tr.Percentage,
                    StartedAt = tr.StartedAt,
                    CompletedAt = tr.CompletedAt,
                    LastActivityAt = tr.CompletedAt ?? tr.StartedAt
                })
                .ToListAsync();

            activities.AddRange(orthoeopyActivities);

            // Regular
            var regularActivities = await _context.RegularTestResults
                .Include(tr => tr.RegularTest)
                .Include(tr => tr.Student)
                    .ThenInclude(s => s.User)
                .Where(tr => tr.RegularTest.TeacherId == currentUser.Id)
                .OrderByDescending(tr => tr.CompletedAt ?? tr.StartedAt)
                .Take(50)
                .Select(tr => new TestActivityViewModel
                {
                    TestId = tr.RegularTestId,
                    TestTitle = tr.RegularTest.Title,
                    TestType = "regular",
                    StudentId = tr.StudentId,
                    StudentName = tr.Student.User.FullName,
                    Status = tr.IsCompleted ? "completed" : "in_progress",
                    Score = tr.Score,
                    MaxScore = tr.MaxScore,
                    Percentage = tr.Percentage,
                    StartedAt = tr.StartedAt,
                    CompletedAt = tr.CompletedAt,
                    LastActivityAt = tr.CompletedAt ?? tr.StartedAt
                })
                .ToListAsync();

            activities.AddRange(regularActivities);

            // Сортируем по времени активности
            var sortedActivities = activities
                .OrderByDescending(a => a.LastActivityAt)
                .Take(50)
                .ToList();

            return Json(sortedActivities);
        }
    }
}
