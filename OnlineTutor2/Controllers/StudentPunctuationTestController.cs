using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using OnlineTutor2.Data;
using OnlineTutor2.Models;
using OnlineTutor2.ViewModels;
using System.Text.Json;

namespace OnlineTutor2.Controllers
{
    [Authorize(Roles = ApplicationRoles.Student)]
    public class StudentPunctuationTestController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;

        public StudentPunctuationTestController(ApplicationDbContext context, UserManager<ApplicationUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        // GET: StudentPunctuationTest - Список доступных тестов
        public async Task<IActionResult> Index()
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

            // Получаем доступные тесты на пунктуацию
            var availableTests = await _context.PunctuationTests
                .Include(pt => pt.Class)
                .Include(pt => pt.Questions)
                .Include(pt => pt.TestResults.Where(tr => tr.StudentId == student.Id))
                .Where(pt => pt.IsActive &&
                           (pt.ClassId == null || pt.ClassId == student.ClassId) &&
                           (pt.StartDate == null || pt.StartDate <= DateTime.Now) &&
                           (pt.EndDate == null || pt.EndDate >= DateTime.Now))
                .OrderBy(pt => pt.Title)
                .ToListAsync();

            var viewModel = new StudentPunctuationTestIndexViewModel
            {
                Student = student,
                AvailableTests = availableTests
            };

            return View(viewModel);
        }

        // GET: StudentPunctuationTest/Start/5 - Начало прохождения теста
        public async Task<IActionResult> Start(int id)
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

            // Проверка доступности теста
            if (!IsTestAvailable(test, student))
            {
                TempData["ErrorMessage"] = "Тест недоступен для прохождения.";
                return RedirectToAction(nameof(Index));
            }

            // Проверка количества попыток
            var attemptCount = test.TestResults.Count(tr => tr.StudentId == student.Id);
            if (attemptCount >= test.MaxAttempts)
            {
                TempData["ErrorMessage"] = $"Превышено максимальное количество попыток ({test.MaxAttempts}).";
                return RedirectToAction(nameof(Index));
            }

            // Проверка незавершенной попытки
            var ongoingResult = test.TestResults
                .FirstOrDefault(tr => tr.StudentId == student.Id && !tr.IsCompleted);

            if (ongoingResult != null)
            {
                return RedirectToAction(nameof(Take), new { id = ongoingResult.Id });
            }

            // Создаем новый результат теста
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

            return RedirectToAction(nameof(Take), new { id = testResult.Id });
        }

        // GET: StudentPunctuationTest/Take/5 - Прохождение теста
        public async Task<IActionResult> Take(int id)
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

            // Проверка, не завершен ли тест
            if (testResult.IsCompleted)
            {
                return RedirectToAction(nameof(Result), new { id = testResult.Id });
            }

            // Проверка времени
            var timeElapsed = DateTime.Now - testResult.StartedAt;
            var timeLimit = TimeSpan.FromMinutes(testResult.PunctuationTest.TimeLimit);

            if (timeElapsed >= timeLimit)
            {
                // Автоматическое завершение по времени
                await CompleteTest(testResult);
                return RedirectToAction(nameof(Result), new { id = testResult.Id });
            }

            var viewModel = new TakePunctuationTestViewModel
            {
                TestResult = testResult,
                TimeRemaining = timeLimit - timeElapsed,
                CurrentQuestionIndex = 0
            };

            return View(viewModel);
        }

        // POST: StudentPunctuationTest/SubmitAnswer - Сохранение ответа
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SubmitAnswer(SubmitPunctuationAnswerViewModel model)
        {
            var currentUser = await _userManager.GetUserAsync(User);
            var student = await _context.Students
                .FirstOrDefaultAsync(s => s.UserId == currentUser.Id);

            if (student == null) return Json(new { success = false, message = "Студент не найден" });

            var testResult = await _context.PunctuationTestResults
                .Include(tr => tr.PunctuationTest)
                .Include(tr => tr.Answers)
                .FirstOrDefaultAsync(tr => tr.Id == model.TestResultId && tr.StudentId == student.Id);

            if (testResult == null || testResult.IsCompleted)
                return Json(new { success = false, message = "Тест не найден или уже завершен" });

            var question = await _context.PunctuationQuestions
                .FirstOrDefaultAsync(q => q.Id == model.QuestionId && q.PunctuationTestId == testResult.PunctuationTestId);

            if (question == null)
                return Json(new { success = false, message = "Вопрос не найден" });

            // Проверка существующего ответа
            var existingAnswer = testResult.Answers
                .FirstOrDefault(a => a.PunctuationQuestionId == model.QuestionId);

            bool isCorrect = CheckPunctuationAnswer(question.CorrectPositions, model.StudentAnswer);
            int points = isCorrect ? question.Points : 0;

            if (existingAnswer != null)
            {
                // Обновляем существующий ответ
                existingAnswer.StudentAnswer = model.StudentAnswer?.Trim();
                existingAnswer.IsCorrect = isCorrect;
                existingAnswer.Points = points;
                existingAnswer.AnsweredAt = DateTime.Now;
            }
            else
            {
                // Создаем новый ответ
                var answer = new PunctuationAnswer
                {
                    PunctuationTestResultId = testResult.Id,
                    PunctuationQuestionId = question.Id,
                    StudentAnswer = model.StudentAnswer?.Trim(),
                    IsCorrect = isCorrect,
                    Points = points,
                    AnsweredAt = DateTime.Now
                };

                _context.PunctuationAnswers.Add(answer);
            }

            await _context.SaveChangesAsync();

            return Json(new
            {
                success = true,
                isCorrect = isCorrect,
                points = points,
                correctAnswer = question.CorrectPositions
            });
        }

        // POST: StudentPunctuationTest/Complete - Завершение теста
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Complete(int testResultId)
        {
            var currentUser = await _userManager.GetUserAsync(User);
            var student = await _context.Students
                .FirstOrDefaultAsync(s => s.UserId == currentUser.Id);

            if (student == null) return NotFound();

            var testResult = await _context.PunctuationTestResults
                .Include(tr => tr.Answers)
                .FirstOrDefaultAsync(tr => tr.Id == testResultId && tr.StudentId == student.Id);

            if (testResult == null || testResult.IsCompleted) return NotFound();

            await CompleteTest(testResult);

            TempData["SuccessMessage"] = "Тест успешно завершен!";
            return RedirectToAction(nameof(Result), new { id = testResult.Id });
        }

        // GET: StudentPunctuationTest/Result/5 - Результат теста
        public async Task<IActionResult> Result(int id)
        {
            var currentUser = await _userManager.GetUserAsync(User);
            var student = await _context.Students
                .FirstOrDefaultAsync(s => s.UserId == currentUser.Id);

            if (student == null) return NotFound();

            var testResult = await _context.PunctuationTestResults
                .Include(tr => tr.PunctuationTest)
                .Include(tr => tr.Answers)
                    .ThenInclude(a => a.Question)
                .Include(tr => tr.Student)
                    .ThenInclude(s => s.User)
                .FirstOrDefaultAsync(tr => tr.Id == id && tr.StudentId == student.Id);

            if (testResult == null) return NotFound();

            return View(testResult);
        }

        // GET: StudentPunctuationTest/History - История прохождения тестов
        public async Task<IActionResult> History()
        {
            var currentUser = await _userManager.GetUserAsync(User);
            var student = await _context.Students
                .FirstOrDefaultAsync(s => s.UserId == currentUser.Id);

            if (student == null) return NotFound();

            var testResults = await _context.PunctuationTestResults
                .Include(tr => tr.PunctuationTest)
                .Where(tr => tr.StudentId == student.Id && tr.IsCompleted)
                .OrderByDescending(tr => tr.CompletedAt)
                .ToListAsync();

            return View(testResults);
        }

        private bool IsTestAvailable(PunctuationTest test, Student student)
        {
            // Проверка класса
            if (test.ClassId.HasValue && test.ClassId != student.ClassId)
                return false;

            // Проверка периода доступности
            if (test.StartDate.HasValue && test.StartDate > DateTime.Now)
                return false;

            if (test.EndDate.HasValue && test.EndDate < DateTime.Now)
                return false;

            return true;
        }

        private bool CheckPunctuationAnswer(string correctPositions, string studentAnswer)
        {
            if (string.IsNullOrWhiteSpace(studentAnswer) || string.IsNullOrWhiteSpace(correctPositions))
            {
                // Если правильный ответ пустой и студент тоже не указал позиции
                return string.IsNullOrWhiteSpace(correctPositions) && string.IsNullOrWhiteSpace(studentAnswer);
            }

            // Нормализуем ответы - убираем пробелы и сортируем
            var correctSet = correctPositions.Split(',')
                .Select(p => p.Trim())
                .Where(p => !string.IsNullOrEmpty(p))
                .OrderBy(p => int.TryParse(p, out int num) ? num : 0)
                .ToHashSet();

            var studentSet = studentAnswer.Split(',')
                .Select(p => p.Trim())
                .Where(p => !string.IsNullOrEmpty(p))
                .OrderBy(p => int.TryParse(p, out int num) ? num : 0)
                .ToHashSet();

            // Сравниваем множества
            return correctSet.SetEquals(studentSet);
        }

        private async Task CompleteTest(PunctuationTestResult testResult)
        {
            testResult.CompletedAt = DateTime.Now;
            testResult.IsCompleted = true;
            testResult.Score = testResult.Answers.Sum(a => a.Points);
            testResult.Percentage = testResult.MaxScore > 0
                ? Math.Round((double)testResult.Score / testResult.MaxScore * 100, 2)
                : 0;

            await _context.SaveChangesAsync();
        }
    }
}
