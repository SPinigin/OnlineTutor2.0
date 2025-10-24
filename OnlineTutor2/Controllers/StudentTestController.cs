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

            // Получаем тесты на орфографию
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

        #region Spelling Tests

        // GET: StudentTest/StartSpelling/5 - Начало теста на орфографию
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

        // GET: StudentTest/TakeSpelling/5 - Прохождение теста на орфографию
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
                return RedirectToAction(nameof(SpellingResult), new { id = testResult.Id });
            }

            var timeElapsed = DateTime.Now - testResult.StartedAt;
            var timeLimit = TimeSpan.FromMinutes(testResult.SpellingTest.TimeLimit);

            if (timeElapsed >= timeLimit)
            {
                await CompleteSpellingTest(testResult);
                return RedirectToAction(nameof(SpellingResult), new { id = testResult.Id });
            }

            var viewModel = new TakeSpellingTestViewModel
            {
                TestResult = testResult,
                TimeRemaining = timeLimit - timeElapsed,
                CurrentQuestionIndex = 0
            };

            return View(viewModel);
        }

        // POST: StudentTest/SubmitSpellingAnswer - Сохранение ответа на орфографию
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SubmitSpellingAnswer(SubmitAnswerViewModel model)
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

            var existingAnswer = testResult.Answers
                .FirstOrDefault(a => a.SpellingQuestionId == model.QuestionId);

            bool isCorrect = CheckSpellingAnswer(question.CorrectLetter, model.StudentAnswer);
            int points = isCorrect ? question.Points : 0;

            if (existingAnswer != null)
            {
                existingAnswer.StudentAnswer = model.StudentAnswer?.Trim();
                existingAnswer.IsCorrect = isCorrect;
                existingAnswer.Points = points;
                existingAnswer.AnsweredAt = DateTime.Now;
            }
            else
            {
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

        // POST: StudentTest/CompleteSpelling - Завершение теста на орфографию
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CompleteSpelling(int testResultId)
        {
            try
            {
                var currentUser = await _userManager.GetUserAsync(User);
                var student = await _context.Students
                    .FirstOrDefaultAsync(s => s.UserId == currentUser.Id);

                if (student == null) return NotFound();

                var testResult = await _context.SpellingTestResults
                    .Include(tr => tr.Answers)
                    .FirstOrDefaultAsync(tr => tr.Id == testResultId && tr.StudentId == student.Id);

                if (testResult == null) return NotFound();

                if (testResult.IsCompleted)
                {
                    // Уже завершен, просто перенаправляем на результат
                    return RedirectToAction(nameof(SpellingResult), new { id = testResult.Id });
                }

                await CompleteSpellingTest(testResult);

                TempData["SuccessMessage"] = "Тест успешно завершен!";
                return RedirectToAction("Result", new { id = testResult.Id });
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = "Произошла ошибка при завершении теста.";
                Console.WriteLine(ex);
                return RedirectToAction(nameof(Index));
            }
        }

        // GET: StudentTest/SpellingResult/5 - Результат теста на орфографию
        public async Task<IActionResult> SpellingResult(int id)
        {
            var currentUser = await _userManager.GetUserAsync(User);
            var student = await _context.Students
                .FirstOrDefaultAsync(s => s.UserId == currentUser.Id);

            if (student == null) return NotFound();

            var testResult = await _context.SpellingTestResults
                .Include(tr => tr.SpellingTest)
                    .ThenInclude(st => st.Questions.OrderBy(q => q.OrderIndex))
                .Include(tr => tr.Answers)
                    .ThenInclude(a => a.Question)
                .Include(tr => tr.Student)
                    .ThenInclude(s => s.User)
                .FirstOrDefaultAsync(tr => tr.Id == id && tr.StudentId == student.Id);

            if (testResult == null) return NotFound();

            return View("Result", testResult);
        }

        #endregion

        #region Punctuation Tests

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
                return RedirectToAction(nameof(PunctuationResult), new { id = testResult.Id });
            }

            var timeElapsed = DateTime.Now - testResult.StartedAt;
            var timeLimit = TimeSpan.FromMinutes(testResult.PunctuationTest.TimeLimit);

            if (timeElapsed >= timeLimit)
            {
                await CompletePunctuationTest(testResult);
                return RedirectToAction(nameof(PunctuationResult), new { id = testResult.Id });
            }

            var viewModel = new TakePunctuationTestViewModel
            {
                TestResult = testResult,
                TimeRemaining = timeLimit - timeElapsed,
                CurrentQuestionIndex = 0
            };

            return View(viewModel);
        }

        // POST: StudentTest/SubmitPunctuationAnswer - Сохранение ответа на пунктуацию
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SubmitPunctuationAnswer(SubmitAnswerViewModel model) // Используем ту же модель
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

            var existingAnswer = testResult.Answers
                .FirstOrDefault(a => a.PunctuationQuestionId == model.QuestionId);

            bool isCorrect = CheckPunctuationAnswer(question.CorrectPositions, model.StudentAnswer);
            int points = isCorrect ? question.Points : 0;

            if (existingAnswer != null)
            {
                existingAnswer.StudentAnswer = model.StudentAnswer?.Trim();
                existingAnswer.IsCorrect = isCorrect;
                existingAnswer.Points = points;
                existingAnswer.AnsweredAt = DateTime.Now;
            }
            else
            {
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

        // POST: StudentTest/CompletePunctuation - Завершение теста на пунктуацию
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CompletePunctuation(int testResultId)
        {
            try
            {
                var currentUser = await _userManager.GetUserAsync(User);
                var student = await _context.Students
                    .FirstOrDefaultAsync(s => s.UserId == currentUser.Id);

                if (student == null) return NotFound();

                var testResult = await _context.PunctuationTestResults
                    .Include(tr => tr.Answers)
                    .FirstOrDefaultAsync(tr => tr.Id == testResultId && tr.StudentId == student.Id);

                if (testResult == null) return NotFound();

                if (testResult.IsCompleted)
                {
                    // Уже завершен, просто перенаправляем на результат
                    return RedirectToAction(nameof(PunctuationResult), new { id = testResult.Id });
                }

                await CompletePunctuationTest(testResult);

                TempData["SuccessMessage"] = "Тест успешно завершен!";
                return RedirectToAction("Result", new { id = testResult.Id });
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = "Произошла ошибка при завершении теста.";
                Console.WriteLine(ex);
                return RedirectToAction(nameof(Index));
            }
        }

        // GET: StudentTest/PunctuationResult/5 - Результат теста на пунктуацию
        public async Task<IActionResult> PunctuationResult(int id)
        {
            var currentUser = await _userManager.GetUserAsync(User);
            var student = await _context.Students
                .FirstOrDefaultAsync(s => s.UserId == currentUser.Id);

            if (student == null) return NotFound();

            var testResult = await _context.PunctuationTestResults
                .Include(tr => tr.PunctuationTest)
                    .ThenInclude(pt => pt.Questions.OrderBy(q => q.OrderIndex))
                .Include(tr => tr.Answers)
                    .ThenInclude(a => a.Question)
                .Include(tr => tr.Student)
                    .ThenInclude(s => s.User)
                .FirstOrDefaultAsync(tr => tr.Id == id && tr.StudentId == student.Id);

            if (testResult == null) return NotFound();

            return View("Result", testResult);
        }

        public async Task<IActionResult> Result(int id)
        {
            var currentUser = await _userManager.GetUserAsync(User);
            var student = await _context.Students
                .FirstOrDefaultAsync(s => s.UserId == currentUser.Id);

            if (student == null) return NotFound();

            // Сначала пробуем найти результат spelling теста
            var spellingResult = await _context.SpellingTestResults
                .Include(tr => tr.SpellingTest)
                    .ThenInclude(st => st.Questions.OrderBy(q => q.OrderIndex))
                .Include(tr => tr.Answers)
                    .ThenInclude(a => a.Question)
                .Include(tr => tr.Student)
                    .ThenInclude(s => s.User)
                .FirstOrDefaultAsync(tr => tr.Id == id && tr.StudentId == student.Id);

            if (spellingResult != null)
            {
                return View(spellingResult);
            }

            // Если не найден, пробуем punctuation тест
            var punctuationResult = await _context.PunctuationTestResults
                .Include(tr => tr.PunctuationTest)
                    .ThenInclude(pt => pt.Questions.OrderBy(q => q.OrderIndex))
                .Include(tr => tr.Answers)
                    .ThenInclude(a => a.Question)
                .Include(tr => tr.Student)
                    .ThenInclude(s => s.User)
                .FirstOrDefaultAsync(tr => tr.Id == id && tr.StudentId == student.Id);

            if (punctuationResult != null)
            {
                return View(punctuationResult);
            }

            return NotFound();
        }

        #endregion

        #region History and Common Actions

        // GET: StudentTest/History - История прохождения всех тестов
        public async Task<IActionResult> History(string? testType = null)
        {
            var currentUser = await _userManager.GetUserAsync(User);
            var student = await _context.Students
                .FirstOrDefaultAsync(s => s.UserId == currentUser.Id);

            if (student == null) return NotFound();

            var viewModel = new StudentTestHistoryViewModel
            {
                Student = student
            };

            if (testType == null || testType == "spelling")
            {
                viewModel.SpellingResults = await _context.SpellingTestResults
                    .Include(tr => tr.SpellingTest)
                    .Where(tr => tr.StudentId == student.Id && tr.IsCompleted)
                    .OrderByDescending(tr => tr.CompletedAt)
                    .ToListAsync();
            }

            if (testType == null || testType == "punctuation")
            {
                viewModel.PunctuationResults = await _context.PunctuationTestResults
                    .Include(tr => tr.PunctuationTest)
                    .Where(tr => tr.StudentId == student.Id && tr.IsCompleted)
                    .OrderByDescending(tr => tr.CompletedAt)
                    .ToListAsync();
            }

            ViewBag.TestType = testType;
            return View(viewModel);
        }

        #endregion

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

        private bool CheckSpellingAnswer(string correctLetter, string studentAnswer)
        {
            if (string.IsNullOrWhiteSpace(studentAnswer) || string.IsNullOrWhiteSpace(correctLetter))
                return false;

            var correct = correctLetter.Trim().ToLowerInvariant();
            var student = studentAnswer.Trim().ToLowerInvariant();

            if (correct == student) return true;

            if (correct.Contains(','))
            {
                var correctVariants = correct.Split(',').Select(v => v.Trim()).ToArray();
                return correctVariants.Contains(student);
            }

            return false;
        }

        private bool CheckPunctuationAnswer(string correctPositions, string studentAnswer)
        {
            if (string.IsNullOrWhiteSpace(studentAnswer) && string.IsNullOrWhiteSpace(correctPositions))
            {
                return true; // Оба пустые - правильно
            }

            if (string.IsNullOrWhiteSpace(studentAnswer) || string.IsNullOrWhiteSpace(correctPositions))
            {
                return false; // Один пустой, другой нет
            }

            // Нормализуем ответы - убираем пробелы и сортируем символы
            var correctSet = correctPositions.Trim()
                .Where(c => char.IsDigit(c))
                .OrderBy(c => c)
                .ToHashSet();

            var studentSet = studentAnswer.Trim()
                .Where(c => char.IsDigit(c))
                .OrderBy(c => c)
                .ToHashSet();

            // Сравниваем множества
            return correctSet.SetEquals(studentSet);
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
