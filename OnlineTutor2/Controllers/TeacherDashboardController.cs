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

        public async Task<IActionResult> Index()
        {
            var currentUser = await _userManager.GetUserAsync(User);

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

        [HttpGet]
        public async Task<IActionResult> GetRecentActivity()
        {
            var currentUser = await _userManager.GetUserAsync(User);
            var activities = new List<TestActivityViewModel>();

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
                    LastActivityAt = tr.CompletedAt ?? tr.StartedAt,
                    TestResultId = tr.Id
                })
                .ToListAsync();

            activities.AddRange(spellingActivities);

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
                    LastActivityAt = tr.CompletedAt ?? tr.StartedAt,
                    TestResultId = tr.Id
                })
                .ToListAsync();

            activities.AddRange(punctuationActivities);

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
                    LastActivityAt = tr.CompletedAt ?? tr.StartedAt,
                    TestResultId = tr.Id
                })
                .ToListAsync();

            activities.AddRange(orthoeopyActivities);

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
                    LastActivityAt = tr.CompletedAt ?? tr.StartedAt,
                    TestResultId = tr.Id
                })
                .ToListAsync();

            activities.AddRange(regularActivities);

            var sortedActivities = activities
                .OrderByDescending(a => a.LastActivityAt)
                .Take(50)
                .ToList();

            return Json(sortedActivities);
        }

        // GET: TeacherDashboard/GetTestResult
        [HttpGet]
        public async Task<IActionResult> GetTestResult(string testType, int testResultId)
        {
            try
            {
                var currentUser = await _userManager.GetUserAsync(User);

                object result = null;

                switch (testType.ToLower())
                {
                    case "spelling":
                        result = await _context.SpellingTestResults
                            .Include(tr => tr.SpellingTest)
                                .ThenInclude(st => st.SpellingQuestions.OrderBy(q => q.OrderIndex))
                            .Include(tr => tr.SpellingAnswers)
                                .ThenInclude(a => a.SpellingQuestion)
                            .Include(tr => tr.Student)
                                .ThenInclude(s => s.User)
                            .FirstOrDefaultAsync(tr => tr.Id == testResultId && tr.SpellingTest.TeacherId == currentUser.Id);
                        break;

                    case "punctuation":
                        result = await _context.PunctuationTestResults
                            .Include(tr => tr.PunctuationTest)
                                .ThenInclude(pt => pt.PunctuationQuestions.OrderBy(q => q.OrderIndex))
                            .Include(tr => tr.PunctuationAnswers)
                                .ThenInclude(a => a.PunctuationQuestion)
                            .Include(tr => tr.Student)
                                .ThenInclude(s => s.User)
                            .FirstOrDefaultAsync(tr => tr.Id == testResultId && tr.PunctuationTest.TeacherId == currentUser.Id);
                        break;

                    case "orthoepy":
                        result = await _context.OrthoeopyTestResults
                            .Include(tr => tr.OrthoeopyTest)
                                .ThenInclude(ot => ot.OrthoeopyQuestions.OrderBy(q => q.OrderIndex))
                            .Include(tr => tr.OrthoeopyAnswers)
                                .ThenInclude(a => a.OrthoeopyQuestion)
                            .Include(tr => tr.Student)
                                .ThenInclude(s => s.User)
                            .FirstOrDefaultAsync(tr => tr.Id == testResultId && tr.OrthoeopyTest.TeacherId == currentUser.Id);
                        break;

                    case "regular":
                        result = await _context.RegularTestResults
                            .Include(tr => tr.RegularTest)
                                .ThenInclude(rt => rt.RegularQuestions.OrderBy(q => q.OrderIndex))
                                    .ThenInclude(q => q.Options.OrderBy(o => o.OrderIndex))
                            .Include(tr => tr.RegularAnswers)
                                .ThenInclude(a => a.RegularQuestion)
                            .Include(tr => tr.Student)
                                .ThenInclude(s => s.User)
                            .FirstOrDefaultAsync(tr => tr.Id == testResultId && tr.RegularTest.TeacherId == currentUser.Id);
                        break;
                }

                if (result == null)
                {
                    return NotFound();
                }

                // Возвращаем частичное представление
                return PartialView("~/Views/StudentTest/Result.cshtml", result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Ошибка загрузки результата теста {TestType} {ResultId}", testType, testResultId);
                return StatusCode(500, "Ошибка загрузки результата");
            }
        }
    }
}
