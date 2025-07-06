using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using OnlineTutor2.Data;
using OnlineTutor2.Models;
using OnlineTutor2.ViewModels;

namespace OnlineTutor2.Controllers
{
    [Authorize(Roles = ApplicationRoles.Student)]
    public class StudentTestController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;

        public StudentTestController(ApplicationDbContext context, UserManager<ApplicationUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        // GET: StudentTest - Список всех доступных тестов
        public async Task<IActionResult> Index(string? category = null)
        {
            var currentUser = await _userManager.GetUserAsync(User);
            var student = await _context.Students
                .Include(s => s.Class)
                .FirstOrDefaultAsync(s => s.UserId == currentUser.Id);

            if (student == null)
            {
                TempData["ErrorMessage"] = "Профиль ученика не найден.";
                return RedirectToAction("Index", "Student");
            }

            var viewModel = new StudentAllTestsIndexViewModel
            {
                Student = student
            };

            // Получаем тесты на правописание
            if (category == null || category == "spelling")
            {
                viewModel.SpellingTests = await _context.SpellingTests
                    .Include(st => st.Class)
                    .Include(st => st.Questions)
                    .Include(st => st.TestResults.Where(tr => tr.StudentId == student.Id))
                    .Where(st => st.IsActive &&
                               (st.ClassId == null || st.ClassId == student.ClassId) &&
                               (st.StartDate == null || st.StartDate <= DateTime.Now) &&
                               (st.EndDate == null || st.EndDate >= DateTime.Now))
                    .OrderBy(st => st.Title)
                    .ToListAsync();
            }

            // Получаем тесты на пунктуацию
            if (category == null || category == "punctuation")
            {
                viewModel.PunctuationTests = await _context.PunctuationTests
                    .Include(pt => pt.Class)
                    .Include(pt => pt.Questions)
                    .Include(pt => pt.TestResults.Where(tr => tr.StudentId == student.Id))
                    .Where(pt => pt.IsActive &&
                               (pt.ClassId == null || pt.ClassId == student.ClassId) &&
                               (pt.StartDate == null || pt.StartDate <= DateTime.Now) &&
                               (pt.EndDate == null || pt.EndDate >= DateTime.Now))
                    .OrderBy(pt => pt.Title)
                    .ToListAsync();
            }

            ViewBag.CurrentCategory = category;
            return View(viewModel);
        }

        // GET: StudentTest/StartSpelling/5 - Начало теста на правописание
        public async Task<IActionResult> StartSpelling(int id)
        {
            var currentUser = await _userManager.GetUserAsync(User);
            var student = await _context.Students
                .FirstOrDefaultAsync(s => s.UserId == currentUser.Id);

            if (student == null) return NotFound();

            var test = await _context.SpellingTests
                .Include(st => st.Questions.OrderBy(q => q.OrderIndex))
                .Include(st => st.TestResults.Where(tr => tr.StudentId == student.Id))
                .FirstOrDefaultAsync(st => st.Id == id && st.IsActive);

            if (test == null) return NotFound();

            if (!IsSpellingTestAvailable(test, student))
            {
                TempData["ErrorMessage"] = "Тест недоступен для прохождения.";
                return RedirectToAction(nameof(Index));
            }

            var attemptCount = test.TestResults.Count(tr => tr.StudentId == student.Id);
            if (attemptCount >= test.MaxAttempts)
            {
                TempData["ErrorMessage"] = $"Превышено максимальное количество попыток ({test.MaxAttempts}).";
                return RedirectToAction(nameof(Index));
            }

            var ongoingResult = test.TestResults
                .FirstOrDefault(tr => tr.StudentId == student.Id && !tr.IsCompleted);

            if (ongoingResult != null)
            {
                return RedirectToAction(nameof(TakeSpelling), new { id = ongoingResult.Id });
            }

            var testResult = new SpellingTestResult
            {
                SpellingTestId = test.Id,
                StudentId = student.Id,
                StartedAt = DateTime.Now,
                AttemptNumber = attemptCount + 1,
                MaxScore = test.Questions.Sum(q => q.Points),
                IsCompleted = false
            };

            _context.SpellingTestResults.Add(testResult);
            await _context.SaveChangesAsync();

            return RedirectToAction(nameof(TakeSpelling), new { id = testResult.Id });
        }

        // GET: StudentTest/StartPunctuation/5 - Начало теста на пунктуацию
        public async Task<IActionResult> StartPunctuation(int id)
        {
            var currentUser = await _userManager.GetUserAsync(User);
            var student = await _context.Students
                .FirstOrDefaultAsync(s => s.UserId == currentUser.Id);

            if (student == null) return NotFound();

            var test = await _context.PunctuationTests
                .Include(pt => pt.Questions.OrderBy(q => q.OrderIndex))
                .Include(pt => pt.TestResults.Where(tr => tr.StudentId == student.Id))
                .FirstOrDefaultAsync(pt => pt.Id == id && pt.IsActive);

            if (test == null) return NotFound();

            if (!IsPunctuationTestAvailable(test, student))
            {
                TempData["ErrorMessage"] = "Тест недоступен для прохождения.";
                return RedirectToAction(nameof(Index));
            }

            var attemptCount = test.TestResults.Count(tr => tr.StudentId == student.Id);
            if (attemptCount >= test.MaxAttempts)
            {
                TempData["ErrorMessage"] = $"Превышено максимальное количество попыток ({test.MaxAttempts}).";
                return RedirectToAction(nameof(Index));
            }

            var ongoingResult = test.TestResults
                .FirstOrDefault(tr => tr.StudentId == student.Id && !tr.IsCompleted);

            if (ongoingResult != null)
            {
                return RedirectToAction(nameof(TakePunctuation), new { id = ongoingResult.Id });
            }

            var testResult = new PunctuationTestResult
            {
                PunctuationTestId = test.Id,
                StudentId = student.Id,
                StartedAt = DateTime.Now,
                AttemptNumber = attemptCount + 1,
                MaxScore = test.Questions.Sum(q => q.Points),
                IsCompleted = false
            };

            _context.PunctuationTestResults.Add(testResult);
            await _context.SaveChangesAsync();

            return RedirectToAction(nameof(TakePunctuation), new { id = testResult.Id });
        }

        // GET: StudentTest/TakeSpelling/5 - Прохождение теста на правописание
        public async Task<IActionResult> TakeSpelling(int id)
        {
            var currentUser = await _userManager.GetUserAsync(User);
            var student = await _context.Students
                .FirstOrDefaultAsync(s => s.UserId == currentUser.Id);

            if (student == null) return NotFound();

            var testResult = await _context.SpellingTestResults
                .Include(tr => tr.SpellingTest)
                    .ThenInclude(st => st.Questions.OrderBy(q => q.OrderIndex))
                .Include(tr => tr.Answers)
                .FirstOrDefaultAsync(tr => tr.Id == id && tr.StudentId == student.Id);

            if (testResult == null) return NotFound();

            if (testResult.IsCompleted)
            {
                return RedirectToAction(nameof(SpellingTestResult), new { id = testResult.Id });
            }

            var timeElapsed = DateTime.Now - testResult.StartedAt;
            var timeLimit = TimeSpan.FromMinutes(testResult.SpellingTest.TimeLimit);

            if (timeElapsed >= timeLimit)
            {
                await CompleteSpellingTest(testResult);
                return RedirectToAction(nameof(SpellingTestResult), new { id = testResult.Id });
            }

            var viewModel = new TakeSpellingTestViewModel
            {
                TestResult = testResult,
                TimeRemaining = timeLimit - timeElapsed,
                CurrentQuestionIndex = 0
            };

            return View(viewModel);
        }

        // GET: StudentTest/TakePunctuation/5 - Прохождение теста на пунктуацию
        public async Task<IActionResult> TakePunctuation(int id)
        {
            var currentUser = await _userManager.GetUserAsync(User);
            var student = await _context.Students
                .FirstOrDefaultAsync(s => s.UserId == currentUser.Id);

            if (student == null) return NotFound();

            var testResult = await _context.PunctuationTestResults
                .Include(tr => tr.PunctuationTest)
                    .ThenInclude(pt => pt.Questions.OrderBy(q => q.OrderIndex))
                .Include(tr => tr.Answers)
                .FirstOrDefaultAsync(tr => tr.Id == id && tr.StudentId == student.Id);

            if (testResult == null) return NotFound();

            if (testResult.IsCompleted)
            {
                return RedirectToAction(nameof(PunctuationTestResult), new { id = testResult.Id });
            }

            var timeElapsed = DateTime.Now - testResult.StartedAt;
            var timeLimit = TimeSpan.FromMinutes(testResult.PunctuationTest.TimeLimit);

            if (timeElapsed >= timeLimit)
            {
                await CompletePunctuationTest(testResult);
                return RedirectToAction(nameof(PunctuationTestResult), new { id = testResult.Id });
            }

            var viewModel = new TakePunctuationTestViewModel
            {
                TestResult = testResult,
                TimeRemaining = timeLimit - timeElapsed,
                CurrentQuestionIndex = 0
            };

            return View(viewModel);
        }

        // Остальные методы (SubmitSpellingAnswer, SubmitPunctuationAnswer, Complete, Result, History и вспомогательные методы)
        // ... (скопировать из существующих контроллеров)

        #region Private Methods

        private bool IsSpellingTestAvailable(SpellingTest test, Student student)
        {
            if (test.ClassId.HasValue && test.ClassId != student.ClassId)
                return false;

            if (test.StartDate.HasValue && test.StartDate > DateTime.Now)
                return false;

            if (test.EndDate.HasValue && test.EndDate < DateTime.Now)
                return false;

            return true;
        }

        private bool IsPunctuationTestAvailable(PunctuationTest test, Student student)
        {
            if (test.ClassId.HasValue && test.ClassId != student.ClassId)
                return false;

            if (test.StartDate.HasValue && test.StartDate > DateTime.Now)
                return false;

            if (test.EndDate.HasValue && test.EndDate < DateTime.Now)
                return false;

            return true;
        }

        private async Task CompleteSpellingTest(SpellingTestResult testResult)
        {
            testResult.CompletedAt = DateTime.Now;
            testResult.IsCompleted = true;
            testResult.Score = testResult.Answers.Sum(a => a.Points);
            testResult.Percentage = testResult.MaxScore > 0
                ? Math.Round((double)testResult.Score / testResult.MaxScore * 100, 2)
                : 0;

            await _context.SaveChangesAsync();
        }

        private async Task CompletePunctuationTest(PunctuationTestResult testResult)
        {
            testResult.CompletedAt = DateTime.Now;
            testResult.IsCompleted = true;
            testResult.Score = testResult.Answers.Sum(a => a.Points);
            testResult.Percentage = testResult.MaxScore > 0
                ? Math.Round((double)testResult.Score / testResult.MaxScore * 100, 2)
                : 0;

            await _context.SaveChangesAsync();
        }

        #endregion
    }
}
