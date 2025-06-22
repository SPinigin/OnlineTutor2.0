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
    public class StudentTestController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;

        public StudentTestController(ApplicationDbContext context, UserManager<ApplicationUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        // GET: StudentTest - Список доступных тестов
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

            // Получаем доступные тесты на правописание
            var availableTests = await _context.SpellingTests
                .Include(st => st.Class)
                .Include(st => st.Questions)
                .Include(st => st.TestResults.Where(tr => tr.StudentId == student.Id))
                .Where(st => st.IsActive &&
                           (st.ClassId == null || st.ClassId == student.ClassId) &&
                           (st.StartDate == null || st.StartDate <= DateTime.Now) &&
                           (st.EndDate == null || st.EndDate >= DateTime.Now))
                .OrderBy(st => st.Title)
                .ToListAsync();

            var viewModel = new StudentTestIndexViewModel
            {
                Student = student,
                AvailableTests = availableTests
            };

            return View(viewModel);
        }

        // GET: StudentTest/Start/5 - Начало прохождения теста
        public async Task<IActionResult> Start(int id)
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

            return RedirectToAction(nameof(Take), new { id = testResult.Id });
        }

        // GET: StudentTest/Take/5 - Прохождение теста
        public async Task<IActionResult> Take(int id)
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

            // Проверка, не завершен ли тест
            if (testResult.IsCompleted)
            {
                return RedirectToAction(nameof(Result), new { id = testResult.Id });
            }

            // Проверка времени
            var timeElapsed = DateTime.Now - testResult.StartedAt;
            var timeLimit = TimeSpan.FromMinutes(testResult.SpellingTest.TimeLimit);

            if (timeElapsed >= timeLimit)
            {
                // Автоматическое завершение по времени
                await CompleteTest(testResult);
                return RedirectToAction(nameof(Result), new { id = testResult.Id });
            }

            var viewModel = new TakeSpellingTestViewModel
            {
                TestResult = testResult,
                TimeRemaining = timeLimit - timeElapsed,
                CurrentQuestionIndex = 0
            };

            return View(viewModel);
        }

        // POST: StudentTest/SubmitAnswer - Сохранение ответа
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SubmitAnswer(SubmitAnswerViewModel model)
        {
            var currentUser = await _userManager.GetUserAsync(User);
            var student = await _context.Students
                .FirstOrDefaultAsync(s => s.UserId == currentUser.Id);

            if (student == null) return Json(new { success = false, message = "Студент не найден" });

            var testResult = await _context.SpellingTestResults
                .Include(tr => tr.SpellingTest)
                .Include(tr => tr.Answers)
                .FirstOrDefaultAsync(tr => tr.Id == model.TestResultId && tr.StudentId == student.Id);

            if (testResult == null || testResult.IsCompleted)
                return Json(new { success = false, message = "Тест не найден или уже завершен" });

            var question = await _context.SpellingQuestions
                .FirstOrDefaultAsync(q => q.Id == model.QuestionId && q.SpellingTestId == testResult.SpellingTestId);

            if (question == null)
                return Json(new { success = false, message = "Вопрос не найден" });

            // Проверка существующего ответа
            var existingAnswer = testResult.Answers
                .FirstOrDefault(a => a.SpellingQuestionId == model.QuestionId);

            bool isCorrect = CheckAnswer(question.CorrectLetter, model.StudentAnswer);
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
                var answer = new SpellingAnswer
                {
                    SpellingTestResultId = testResult.Id,
                    SpellingQuestionId = question.Id,
                    StudentAnswer = model.StudentAnswer?.Trim(),
                    IsCorrect = isCorrect,
                    Points = points,
                    AnsweredAt = DateTime.Now
                };

                _context.SpellingAnswers.Add(answer);
            }

            await _context.SaveChangesAsync();

            return Json(new
            {
                success = true,
                isCorrect = isCorrect,
                points = points,
                correctAnswer = question.CorrectLetter
            });
        }

        // POST: StudentTest/Complete - Завершение теста
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Complete(int testResultId)
        {
            var currentUser = await _userManager.GetUserAsync(User);
            var student = await _context.Students
                .FirstOrDefaultAsync(s => s.UserId == currentUser.Id);

            if (student == null) return NotFound();

            var testResult = await _context.SpellingTestResults
                .Include(tr => tr.Answers)
                .FirstOrDefaultAsync(tr => tr.Id == testResultId && tr.StudentId == student.Id);

            if (testResult == null || testResult.IsCompleted) return NotFound();

            await CompleteTest(testResult);

            TempData["SuccessMessage"] = "Тест успешно завершен!";
            return RedirectToAction(nameof(Result), new { id = testResult.Id });
        }

        // GET: StudentTest/Result/5 - Результат теста
        public async Task<IActionResult> Result(int id)
        {
            var currentUser = await _userManager.GetUserAsync(User);
            var student = await _context.Students
                .FirstOrDefaultAsync(s => s.UserId == currentUser.Id);

            if (student == null) return NotFound();

            var testResult = await _context.SpellingTestResults
                .Include(tr => tr.SpellingTest)
                .Include(tr => tr.Answers)
                    .ThenInclude(a => a.Question)
                .Include(tr => tr.Student)
                    .ThenInclude(s => s.User)
                .FirstOrDefaultAsync(tr => tr.Id == id && tr.StudentId == student.Id);

            if (testResult == null) return NotFound();

            return View(testResult);
        }

        // GET: StudentTest/History - История прохождения тестов
        public async Task<IActionResult> History()
        {
            var currentUser = await _userManager.GetUserAsync(User);
            var student = await _context.Students
                .FirstOrDefaultAsync(s => s.UserId == currentUser.Id);

            if (student == null) return NotFound();

            var testResults = await _context.SpellingTestResults
                .Include(tr => tr.SpellingTest)
                .Where(tr => tr.StudentId == student.Id && tr.IsCompleted)
                .OrderByDescending(tr => tr.CompletedAt)
                .ToListAsync();

            return View(testResults);
        }

        // Вспомогательные методы
        private bool IsTestAvailable(SpellingTest test, Student student)
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

        private bool CheckAnswer(string correctLetter, string studentAnswer)
        {
            if (string.IsNullOrWhiteSpace(studentAnswer) || string.IsNullOrWhiteSpace(correctLetter))
                return false;

            // Нормализуем ответы
            var correct = correctLetter.Trim().ToLowerInvariant();
            var student = studentAnswer.Trim().ToLowerInvariant();

            // Проверка точного совпадения
            if (correct == student) return true;

            // Проверка множественных вариантов (разделенных запятой)
            if (correct.Contains(','))
            {
                var correctVariants = correct.Split(',').Select(v => v.Trim()).ToArray();
                return correctVariants.Contains(student);
            }

            return false;
        }

        private async Task CompleteTest(SpellingTestResult testResult)
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
