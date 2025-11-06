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
        private readonly ILogger<StudentTestController> _logger;

        public StudentTestController(
            ApplicationDbContext context, 
            UserManager<ApplicationUser> userManager,
            ILogger<StudentTestController> logger)
        {
            _context = context;
            _userManager = userManager;
            _logger = logger;
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
                _logger.LogWarning("Профиль студента не найден для пользователя {UserId}", currentUser.Id);
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
                    .Include(st => st.SpellingQuestions)
                    .Include(st => st.SpellingTestResults.Where(tr => tr.StudentId == student.Id))
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
                    .Include(pt => pt.PunctuationQuestions)
                    .Include(pt => pt.PunctuationTestResults.Where(tr => tr.StudentId == student.Id))
                    .Where(pt => pt.IsActive &&
                               (pt.ClassId == null || pt.ClassId == student.ClassId) &&
                               (pt.StartDate == null || pt.StartDate <= DateTime.Now) &&
                               (pt.EndDate == null || pt.EndDate >= DateTime.Now))
                    .OrderBy(pt => pt.Title)
                    .ToListAsync();
            }

            // Получаем тесты на орфоэпию
            if (category == null || category == "orthoepy")
            {
                viewModel.OrthoeopyTests = await _context.OrthoeopyTests
                    .Include(ot => ot.Class)
                    .Include(ot => ot.OrthoeopyQuestions)
                    .Include(ot => ot.OrthoeopyTestResults.Where(tr => tr.StudentId == student.Id))
                    .Where(ot => ot.IsActive &&
                               (ot.ClassId == null || ot.ClassId == student.ClassId) &&
                               (ot.StartDate == null || ot.StartDate <= DateTime.Now) &&
                               (ot.EndDate == null || ot.EndDate >= DateTime.Now))
                    .OrderBy(ot => ot.Title)
                    .ToListAsync();
            }

            // Получаем тесты классические
            if (category == null || category == "regular")
            {
                viewModel.RegularTests = await _context.RegularTests
                    .Include(rt => rt.Class)
                    .Include(rt => rt.RegularQuestions)
                    .Include(rt => rt.RegularTestResults.Where(tr => tr.StudentId == student.Id))
                    .Where(rt => rt.IsActive &&
                               (rt.ClassId == null || rt.ClassId == student.ClassId) &&
                               (rt.StartDate == null || rt.StartDate <= DateTime.Now) &&
                               (rt.EndDate == null || rt.EndDate >= DateTime.Now))
                    .OrderBy(rt => rt.Title)
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
                .Include(st => st.SpellingQuestions.OrderBy(q => q.OrderIndex))
                .Include(st => st.SpellingTestResults.Where(tr => tr.StudentId == student.Id))
                .FirstOrDefaultAsync(st => st.Id == id && st.IsActive);

            if (test == null) return NotFound();

            if (!IsSpellingTestAvailable(test, student))
            {
                TempData["ErrorMessage"] = "Тест недоступен для прохождения.";
                return RedirectToAction(nameof(Index));
            }

            var attemptCount = test.SpellingTestResults.Count(tr => tr.StudentId == student.Id);
            if (attemptCount >= test.MaxAttempts)
            {
                TempData["ErrorMessage"] = $"Превышено количество попыток ({test.MaxAttempts}).";
                return RedirectToAction(nameof(Index));
            }

            var ongoingResult = test.SpellingTestResults
                .FirstOrDefault(tr => tr.StudentId == student.Id && !tr.IsCompleted);

            if (ongoingResult != null)
            {
                _logger.LogInformation("Студент {StudentId} продолжает незавершенный тест орфографии {TestId}, ResultId: {ResultId}",
                    student.Id, id, ongoingResult.Id);
                return RedirectToAction(nameof(TakeSpelling), new { id = ongoingResult.Id });
            }

            var testResult = new SpellingTestResult
            {
                SpellingTestId = test.Id,
                StudentId = student.Id,
                StartedAt = DateTime.Now,
                AttemptNumber = attemptCount + 1,
                MaxScore = test.SpellingQuestions.Sum(q => q.Points),
                IsCompleted = false
            };

            _context.SpellingTestResults.Add(testResult);
            await _context.SaveChangesAsync();

            _logger.LogInformation("Студент {StudentId} начал тест орфографии {TestId}, ResultId: {ResultId}, Попытка: {AttemptNumber}",
                student.Id, id, testResult.Id, testResult.AttemptNumber);

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
                    .ThenInclude(st => st.SpellingQuestions.OrderBy(q => q.OrderIndex))
                .Include(tr => tr.SpellingAnswers)
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
                _logger.LogInformation("Время теста орфографии {ResultId} истекло для студента {StudentId}. Прошло: {TimeElapsed}, Лимит: {TimeLimit}",
                    id, student.Id, timeElapsed, timeLimit);
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
        public async Task<IActionResult> SubmitSpellingAnswer(SubmitSpellingAnswerViewModel model)
        {
            var currentUser = await _userManager.GetUserAsync(User);
            var student = await _context.Students
                .FirstOrDefaultAsync(s => s.UserId == currentUser.Id);

            if (student == null) return Json(new { success = false, message = "Студент не найден" });

            var testResult = await _context.SpellingTestResults
                .Include(tr => tr.SpellingTest)
                .Include(tr => tr.SpellingAnswers)
                .FirstOrDefaultAsync(tr => tr.Id == model.TestResultId && tr.StudentId == student.Id);

            if (testResult == null || testResult.IsCompleted)
                return Json(new { success = false, message = "Тест не найден или уже завершен" });

            var question = await _context.SpellingQuestions
                .FirstOrDefaultAsync(q => q.Id == model.QuestionId && q.SpellingTestId == testResult.SpellingTestId);

            if (question == null)
                return Json(new { success = false, message = "Вопрос не найден" });

            var existingAnswer = testResult.SpellingAnswers
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

            _logger.LogInformation("Студент {StudentId} ответил на вопрос {QuestionId} в тесте орфографии {ResultId}. Правильно: {IsCorrect}, Баллы: {Points}",
                student.Id, model.QuestionId, model.TestResultId, isCorrect, points);

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
            var currentUser = await _userManager.GetUserAsync(User);

            try
            {
                var student = await _context.Students
                    .FirstOrDefaultAsync(s => s.UserId == currentUser.Id);

                if (student == null) return NotFound();

                var testResult = await _context.SpellingTestResults
                    .Include(tr => tr.SpellingAnswers)
                    .FirstOrDefaultAsync(tr => tr.Id == testResultId && tr.StudentId == student.Id);

                if (testResult == null) return NotFound();

                if (testResult.IsCompleted)
                {
                    // Уже завершен, просто перенаправляем на результат
                    return RedirectToAction(nameof(SpellingResult), new { id = testResult.Id });
                }

                await CompleteSpellingTest(testResult);

                _logger.LogInformation("Студент {StudentId} завершил тест орфографии {ResultId}. Баллы: {Score}/{MaxScore}, Процент: {Percentage}", 
                    student.Id, testResultId, testResult.Score, testResult.MaxScore, testResult.Percentage);

                return RedirectToAction("Result", new { id = testResult.Id });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Ошибка завершения теста орфографии {ResultId} студентом {StudentId}", testResultId, currentUser.Id);
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
                    .ThenInclude(st => st.SpellingQuestions.OrderBy(q => q.OrderIndex))
                .Include(tr => tr.SpellingAnswers)
                    .ThenInclude(a => a.SpellingQuestion)
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
                .Include(pt => pt.PunctuationQuestions.OrderBy(q => q.OrderIndex))
                .Include(pt => pt.PunctuationTestResults.Where(tr => tr.StudentId == student.Id))
                .FirstOrDefaultAsync(pt => pt.Id == id && pt.IsActive);

            if (test == null) return NotFound();

            if (!IsPunctuationTestAvailable(test, student))
            {
                TempData["ErrorMessage"] = "Тест недоступен для прохождения.";
                return RedirectToAction(nameof(Index));
            }

            var attemptCount = test.PunctuationTestResults.Count(tr => tr.StudentId == student.Id);
            if (attemptCount >= test.MaxAttempts)
            {
                _logger.LogWarning("Студент {StudentId} превысил лимит попыток ({MaxAttempts}) для теста пунктуации {TestId}",
                    student.Id, test.MaxAttempts, id);
                TempData["ErrorMessage"] = $"Превышено количество попыток ({test.MaxAttempts}).";
                return RedirectToAction(nameof(Index));
            }

            var ongoingResult = test.PunctuationTestResults
                .FirstOrDefault(tr => tr.StudentId == student.Id && !tr.IsCompleted);

            if (ongoingResult != null)
            {
                _logger.LogInformation("Студент {StudentId} продолжает незавершенный тест пунктуации {TestId}, ResultId: {ResultId}",
                    student.Id, id, ongoingResult.Id);
                return RedirectToAction(nameof(TakePunctuation), new { id = ongoingResult.Id });
            }

            var testResult = new PunctuationTestResult
            {
                PunctuationTestId = test.Id,
                StudentId = student.Id,
                StartedAt = DateTime.Now,
                AttemptNumber = attemptCount + 1,
                MaxScore = test.PunctuationQuestions.Sum(q => q.Points),
                IsCompleted = false
            };

            _context.PunctuationTestResults.Add(testResult);
            await _context.SaveChangesAsync();

            _logger.LogInformation("Студент {StudentId} начал тест пунктуации {TestId}, ResultId: {ResultId}, Попытка: {AttemptNumber}",
                student.Id, id, testResult.Id, testResult.AttemptNumber);

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
                    .ThenInclude(pt => pt.PunctuationQuestions.OrderBy(q => q.OrderIndex))
                .Include(tr => tr.PunctuationAnswers)
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
                _logger.LogInformation("Время теста пунктуации {ResultId} истекло для студента {StudentId}. Прошло: {TimeElapsed}, Лимит: {TimeLimit}",
                    id, student.Id, timeElapsed, timeLimit);
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
        public async Task<IActionResult> SubmitPunctuationAnswer(SubmitPunctuationAnswerViewModel model)
        {
            var currentUser = await _userManager.GetUserAsync(User);
            var student = await _context.Students
                .FirstOrDefaultAsync(s => s.UserId == currentUser.Id);

            if (student == null) return Json(new { success = false, message = "Студент не найден" });

            var testResult = await _context.PunctuationTestResults
                .Include(tr => tr.PunctuationTest)
                .Include(tr => tr.PunctuationAnswers)
                .FirstOrDefaultAsync(tr => tr.Id == model.TestResultId && tr.StudentId == student.Id);

            if (testResult == null || testResult.IsCompleted)
                return Json(new { success = false, message = "Тест не найден или уже завершен" });

            var question = await _context.PunctuationQuestions
                .FirstOrDefaultAsync(q => q.Id == model.QuestionId && q.PunctuationTestId == testResult.PunctuationTestId);

            if (question == null)
                return Json(new { success = false, message = "Вопрос не найден" });

            var existingAnswer = testResult.PunctuationAnswers
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

            _logger.LogInformation("Студент {StudentId} ответил на вопрос {QuestionId} в тесте пунктуации {ResultId}. Правильно: {IsCorrect}, Баллы: {Points}",
                student.Id, model.QuestionId, model.TestResultId, isCorrect, points);

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
                    .Include(tr => tr.PunctuationAnswers)
                    .FirstOrDefaultAsync(tr => tr.Id == testResultId && tr.StudentId == student.Id);

                if (testResult == null) return NotFound();

                if (testResult.IsCompleted)
                {
                    // Уже завершен, просто перенаправляем на результат
                    return RedirectToAction(nameof(PunctuationResult), new { id = testResult.Id });
                }

                await CompletePunctuationTest(testResult);

                _logger.LogInformation("Студент {StudentId} завершил тест пунктуации {ResultId}. Баллы: {Score}/{MaxScore}, Процент: {Percentage}",
                    student.Id, testResultId, testResult.Score, testResult.MaxScore, testResult.Percentage);

                return RedirectToAction("Result", new { id = testResult.Id });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Ошибка завершения теста пунктуации {ResultId} студентом {StudentId}", testResultId, _userManager.GetUserId(User));
                TempData["ErrorMessage"] = "Произошла ошибка при завершении теста.";
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
                    .ThenInclude(pt => pt.PunctuationQuestions.OrderBy(q => q.OrderIndex))
                .Include(tr => tr.PunctuationAnswers)
                    .ThenInclude(a => a.PunctuationQuestion)
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

            // Пробуем найти результат spelling теста
            var spellingResult = await _context.SpellingTestResults
                .Include(tr => tr.SpellingTest)
                    .ThenInclude(st => st.SpellingQuestions.OrderBy(q => q.OrderIndex))
                .Include(tr => tr.SpellingAnswers)
                    .ThenInclude(a => a.SpellingQuestion)
                .Include(tr => tr.Student)
                    .ThenInclude(s => s.User)
                .FirstOrDefaultAsync(tr => tr.Id == id && tr.StudentId == student.Id);

            if (spellingResult != null)
            {
                return View(spellingResult);
            }

            // Пробуем найти punctuation тест
            var punctuationResult = await _context.PunctuationTestResults
                .Include(tr => tr.PunctuationTest)
                    .ThenInclude(pt => pt.PunctuationQuestions.OrderBy(q => q.OrderIndex))
                .Include(tr => tr.PunctuationAnswers)
                    .ThenInclude(a => a.PunctuationQuestion)
                .Include(tr => tr.Student)
                    .ThenInclude(s => s.User)
                .FirstOrDefaultAsync(tr => tr.Id == id && tr.StudentId == student.Id);

            if (punctuationResult != null)
            {
                return View(punctuationResult);
            }

            // Пробуем найти orthoepy тест
            var orthoeopyResult = await _context.OrthoeopyTestResults
                .Include(tr => tr.OrthoeopyTest)
                    .ThenInclude(ot => ot.OrthoeopyQuestions.OrderBy(q => q.OrderIndex))
                .Include(tr => tr.OrthoeopyAnswers)
                    .ThenInclude(a => a.OrthoeopyQuestion)
                .Include(tr => tr.Student)
                    .ThenInclude(s => s.User)
                .FirstOrDefaultAsync(tr => tr.Id == id && tr.StudentId == student.Id);

            if (orthoeopyResult != null)
            {
                return View(orthoeopyResult);
            }

            // Пробуем найти regular тест
            var regularResult = await _context.RegularTestResults
                .Include(tr => tr.RegularTest)
                    .ThenInclude(rt => rt.RegularQuestions.OrderBy(q => q.OrderIndex))
                        .ThenInclude(q => q.Options.OrderBy(o => o.OrderIndex))
                .Include(tr => tr.RegularAnswers)
                    .ThenInclude(a => a.RegularQuestion)
                .Include(tr => tr.Student)
                    .ThenInclude(s => s.User)
                .FirstOrDefaultAsync(tr => tr.Id == id && tr.StudentId == student.Id);

            if (regularResult != null)
            {
                return View(regularResult);
            }

            return NotFound();
        }

        #endregion

        #region Orthoepy Tests

        // GET: StudentTest/StartOrthoepy/5 - Начало теста на орфоэпию
        public async Task<IActionResult> StartOrthoepy(int id)
        {
            var currentUser = await _userManager.GetUserAsync(User);
            var student = await _context.Students
                .FirstOrDefaultAsync(s => s.UserId == currentUser.Id);

            if (student == null) return NotFound();

            var test = await _context.OrthoeopyTests
                .Include(ot => ot.OrthoeopyQuestions.OrderBy(q => q.OrderIndex))
                .Include(ot => ot.OrthoeopyTestResults.Where(tr => tr.StudentId == student.Id))
                .FirstOrDefaultAsync(ot => ot.Id == id && ot.IsActive);

            if (test == null) return NotFound();

            if (!IsOrthoeopyTestAvailable(test, student))
            {
                TempData["ErrorMessage"] = "Тест недоступен для прохождения.";
                return RedirectToAction(nameof(Index));
            }

            var attemptCount = test.OrthoeopyTestResults.Count(tr => tr.StudentId == student.Id);
            if (attemptCount >= test.MaxAttempts)
            {
                _logger.LogWarning("Студент {StudentId} превысил лимит попыток ({MaxAttempts}) для теста орфоэпии {TestId}",
                    student.Id, test.MaxAttempts, id);
                TempData["ErrorMessage"] = $"Превышено количество попыток ({test.MaxAttempts}).";
                return RedirectToAction(nameof(Index));
            }

            var ongoingResult = test.OrthoeopyTestResults
                .FirstOrDefault(tr => tr.StudentId == student.Id && !tr.IsCompleted);

            if (ongoingResult != null)
            {
                _logger.LogInformation("Студент {StudentId} продолжает незавершенный тест орфоэпии {TestId}, ResultId: {ResultId}",
                    student.Id, id, ongoingResult.Id);
                return RedirectToAction(nameof(TakeOrthoepy), new { id = ongoingResult.Id });
            }

            var testResult = new OrthoeopyTestResult
            {
                OrthoeopyTestId = test.Id,
                StudentId = student.Id,
                StartedAt = DateTime.Now,
                AttemptNumber = attemptCount + 1,
                MaxScore = test.OrthoeopyQuestions.Sum(q => q.Points),
                IsCompleted = false
            };

            _context.OrthoeopyTestResults.Add(testResult);
            await _context.SaveChangesAsync();

            _logger.LogInformation("Студент {StudentId} начал тест орфоэпии {TestId}, ResultId: {ResultId}, Попытка: {AttemptNumber}",
                student.Id, id, testResult.Id, testResult.AttemptNumber);

            return RedirectToAction(nameof(TakeOrthoepy), new { id = testResult.Id });
        }

        // GET: StudentTest/TakeOrthoepy/5 - Прохождение теста на орфоэпию
        public async Task<IActionResult> TakeOrthoepy(int id)
        {
            var currentUser = await _userManager.GetUserAsync(User);
            var student = await _context.Students
                .FirstOrDefaultAsync(s => s.UserId == currentUser.Id);

            if (student == null) return NotFound();

            var testResult = await _context.OrthoeopyTestResults
                .Include(tr => tr.OrthoeopyTest)
                    .ThenInclude(ot => ot.OrthoeopyQuestions.OrderBy(q => q.OrderIndex))
                .Include(tr => tr.OrthoeopyAnswers)
                .FirstOrDefaultAsync(tr => tr.Id == id && tr.StudentId == student.Id);

            if (testResult == null) return NotFound();

            if (testResult.IsCompleted)
            {
                return RedirectToAction(nameof(OrthoeopyResult), new { id = testResult.Id });
            }

            var timeElapsed = DateTime.Now - testResult.StartedAt;
            var timeLimit = TimeSpan.FromMinutes(testResult.OrthoeopyTest.TimeLimit);

            if (timeElapsed >= timeLimit)
            {
                _logger.LogInformation("Время теста орфоэпии {ResultId} истекло для студента {StudentId}. Прошло: {TimeElapsed}, Лимит: {TimeLimit}",
                    id, student.Id, timeElapsed, timeLimit);
                await CompleteOrthoeopyTest(testResult);
                return RedirectToAction(nameof(OrthoeopyResult), new { id = testResult.Id });
            }

            var viewModel = new TakeOrthoeopyTestViewModel
            {
                TestResult = testResult,
                TimeRemaining = timeLimit - timeElapsed,
                CurrentQuestionIndex = 0
            };

            return View(viewModel);
        }

        // POST: StudentTest/SubmitOrthoeopyAnswer - Сохранение ответа на орфоэпию
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SubmitOrthoeopyAnswer(int TestResultId, int QuestionId, int SelectedStressPosition)
        {
            var currentUser = await _userManager.GetUserAsync(User);
            var student = await _context.Students
                .FirstOrDefaultAsync(s => s.UserId == currentUser.Id);

            if (student == null) return Json(new { success = false, message = "Студент не найден" });

            var testResult = await _context.OrthoeopyTestResults
                .Include(tr => tr.OrthoeopyTest)
                .Include(tr => tr.OrthoeopyAnswers)
                .FirstOrDefaultAsync(tr => tr.Id == TestResultId && tr.StudentId == student.Id);

            if (testResult == null || testResult.IsCompleted)
                return Json(new { success = false, message = "Тест не найден или уже завершен" });

            var question = await _context.OrthoeopyQuestions
                .FirstOrDefaultAsync(q => q.Id == QuestionId && q.OrthoeopyTestId == testResult.OrthoeopyTestId);

            if (question == null)
                return Json(new { success = false, message = "Вопрос не найден" });

            var existingAnswer = testResult.OrthoeopyAnswers
                .FirstOrDefault(a => a.OrthoeopyQuestionId == QuestionId);

            bool isCorrect = question.StressPosition == SelectedStressPosition;
            int points = isCorrect ? question.Points : 0;

            if (existingAnswer != null)
            {
                existingAnswer.SelectedStressPosition = SelectedStressPosition;
                existingAnswer.IsCorrect = isCorrect;
                existingAnswer.Points = points;
                existingAnswer.AnsweredAt = DateTime.Now;
            }
            else
            {
                var answer = new OrthoeopyAnswer
                {
                    OrthoeopyTestResultId = testResult.Id,
                    OrthoeopyQuestionId = question.Id,
                    SelectedStressPosition = SelectedStressPosition,
                    IsCorrect = isCorrect,
                    Points = points,
                    AnsweredAt = DateTime.Now
                };

                _context.OrthoeopyAnswers.Add(answer);
            }

            await _context.SaveChangesAsync();

            _logger.LogInformation("Студент {StudentId} ответил на вопрос {QuestionId} в тесте орфоэпии {ResultId}. Выбрана позиция: {SelectedPosition}, Правильная: {CorrectPosition}, Правильно: {IsCorrect}, Баллы: {Points}",
                student.Id, QuestionId, TestResultId, SelectedStressPosition, question.StressPosition, isCorrect, points);

            return Json(new
            {
                success = true,
                isCorrect = isCorrect,
                points = points,
                correctPosition = question.StressPosition
            });
        }

        // POST: StudentTest/CompleteOrthoepy - Завершение теста на орфоэпию
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CompleteOrthoepy(int testResultId)
        {
            var currentUser = await _userManager.GetUserAsync(User);

            try
            {
                var student = await _context.Students
                    .FirstOrDefaultAsync(s => s.UserId == currentUser.Id);

                if (student == null) return NotFound();

                var testResult = await _context.OrthoeopyTestResults
                    .Include(tr => tr.OrthoeopyAnswers)
                    .FirstOrDefaultAsync(tr => tr.Id == testResultId && tr.StudentId == student.Id);

                if (testResult == null) return NotFound();

                if (testResult.IsCompleted)
                {
                    return RedirectToAction(nameof(OrthoeopyResult), new { id = testResult.Id });
                }

                await CompleteOrthoeopyTest(testResult);

                _logger.LogInformation("Студент {StudentId} завершил тест орфоэпии {ResultId}. Баллы: {Score}/{MaxScore}, Процент: {Percentage}",
                    student.Id, testResultId, testResult.Score, testResult.MaxScore, testResult.Percentage);

                return RedirectToAction("Result", new { id = testResult.Id });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Ошибка завершения теста орфоэпии {ResultId} студентом {StudentId}", testResultId, currentUser.Id);
                TempData["ErrorMessage"] = "Произошла ошибка при завершении теста.";
                Console.WriteLine(ex);
                return RedirectToAction(nameof(Index));
            }
        }

        // GET: StudentTest/OrthoeopyResult/5 - Результат теста на орфоэпию
        public async Task<IActionResult> OrthoeopyResult(int id)
        {
            var currentUser = await _userManager.GetUserAsync(User);
            var student = await _context.Students
                .FirstOrDefaultAsync(s => s.UserId == currentUser.Id);

            if (student == null) return NotFound();

            var testResult = await _context.OrthoeopyTestResults
                .Include(tr => tr.OrthoeopyTest)
                    .ThenInclude(ot => ot.OrthoeopyQuestions.OrderBy(q => q.OrderIndex))
                .Include(tr => tr.OrthoeopyAnswers)
                    .ThenInclude(a => a.OrthoeopyQuestion)
                .Include(tr => tr.Student)
                    .ThenInclude(s => s.User)
                .FirstOrDefaultAsync(tr => tr.Id == id && tr.StudentId == student.Id);

            if (testResult == null) return NotFound();

            return View("Result", testResult);
        }

        #endregion

        #region Regular Tests

        // GET: StudentTest/StartRegular/5
        public async Task<IActionResult> StartRegular(int id)
        {
            var currentUser = await _userManager.GetUserAsync(User);
            var student = await _context.Students
                .FirstOrDefaultAsync(s => s.UserId == currentUser.Id);

            if (student == null) return NotFound();

            var test = await _context.RegularTests
                .Include(rt => rt.RegularQuestions.OrderBy(q => q.OrderIndex))
                    .ThenInclude(q => q.Options.OrderBy(o => o.OrderIndex))
                .Include(rt => rt.RegularTestResults.Where(tr => tr.StudentId == student.Id))
                .FirstOrDefaultAsync(rt => rt.Id == id && rt.IsActive);

            if (test == null) return NotFound();

            if (!IsRegularTestAvailable(test, student))
            {
                TempData["ErrorMessage"] = "Тест недоступен для прохождения.";
                return RedirectToAction(nameof(Index));
            }

            var attemptCount = test.RegularTestResults.Count(tr => tr.StudentId == student.Id);
            if (attemptCount >= test.MaxAttempts)
            {
                _logger.LogWarning("Студент {StudentId} превысил лимит попыток ({MaxAttempts}) для классического теста {TestId}",
                    student.Id, test.MaxAttempts, id);
                TempData["ErrorMessage"] = $"Превышено количество попыток ({test.MaxAttempts}).";
                return RedirectToAction(nameof(Index));
            }

            var ongoingResult = test.RegularTestResults
                .FirstOrDefault(tr => tr.StudentId == student.Id && !tr.IsCompleted);

            if (ongoingResult != null)
            {
                _logger.LogInformation("Студент {StudentId} продолжает незавершенный классический тест {TestId}, ResultId: {ResultId}",
                    student.Id, id, ongoingResult.Id);
                return RedirectToAction(nameof(TakeRegular), new { id = ongoingResult.Id });
            }

            var testResult = new RegularTestResult
            {
                RegularTestId = test.Id,
                StudentId = student.Id,
                StartedAt = DateTime.Now,
                AttemptNumber = attemptCount + 1,
                MaxScore = test.RegularQuestions.Sum(q => q.Points),
                IsCompleted = false
            };

            _context.RegularTestResults.Add(testResult);
            await _context.SaveChangesAsync();

            _logger.LogInformation("Студент {StudentId} начал классический тест {TestId}, ResultId: {ResultId}, Попытка: {AttemptNumber}",
                student.Id, id, testResult.Id, testResult.AttemptNumber);

            return RedirectToAction(nameof(TakeRegular), new { id = testResult.Id });
        }

        // GET: StudentTest/TakeRegular/5
        public async Task<IActionResult> TakeRegular(int id)
        {
            var currentUser = await _userManager.GetUserAsync(User);
            var student = await _context.Students
                .FirstOrDefaultAsync(s => s.UserId == currentUser.Id);

            if (student == null) return NotFound();

            var testResult = await _context.RegularTestResults
                .Include(tr => tr.RegularTest)
                    .ThenInclude(rt => rt.RegularQuestions.OrderBy(q => q.OrderIndex))
                        .ThenInclude(q => q.Options.OrderBy(o => o.OrderIndex))
                .Include(tr => tr.RegularAnswers)
                .FirstOrDefaultAsync(tr => tr.Id == id && tr.StudentId == student.Id);

            if (testResult == null) return NotFound();

            if (testResult.IsCompleted)
            {
                return RedirectToAction(nameof(RegularResult), new { id = testResult.Id });
            }

            var timeElapsed = DateTime.Now - testResult.StartedAt;
            var timeLimit = TimeSpan.FromMinutes(testResult.RegularTest.TimeLimit);

            if (timeElapsed >= timeLimit)
            {
                _logger.LogInformation("Время классического теста {ResultId} истекло для студента {StudentId}",
                    id, student.Id);
                await CompleteRegularTest(testResult);
                return RedirectToAction(nameof(RegularResult), new { id = testResult.Id });
            }

            var viewModel = new TakeRegularTestViewModel
            {
                TestResult = testResult,
                TimeRemaining = timeLimit - timeElapsed,
                CurrentQuestionIndex = 0
            };

            return View(viewModel);
        }

        // POST: StudentTest/SubmitRegularAnswer
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SubmitRegularAnswer(SubmitRegularAnswerViewModel model)
        {
            var currentUser = await _userManager.GetUserAsync(User);
            var student = await _context.Students
                .FirstOrDefaultAsync(s => s.UserId == currentUser.Id);

            if (student == null)
                return Json(new { success = false, message = "Студент не найден" });

            var testResult = await _context.RegularTestResults
                .Include(tr => tr.RegularTest)
                .Include(tr => tr.RegularAnswers)
                .FirstOrDefaultAsync(tr => tr.Id == model.TestResultId && tr.StudentId == student.Id);

            if (testResult == null || testResult.IsCompleted)
                return Json(new { success = false, message = "Тест не найден или уже завершен" });

            var question = await _context.RegularQuestions
                .Include(q => q.Options)
                .FirstOrDefaultAsync(q => q.Id == model.QuestionId && q.TestId == testResult.RegularTestId);

            if (question == null)
                return Json(new { success = false, message = "Вопрос не найден" });

            var existingAnswer = testResult.RegularAnswers
                .FirstOrDefault(a => a.QuestionId == model.QuestionId);

            bool isCorrect = false;
            int points = 0;
            string selectedOptionIdsStr = "";

            // Проверка ответа в зависимости от типа вопроса
            switch (question.Type)
            {
                case QuestionType.SingleChoice:
                    if (model.SelectedOptionId.HasValue)
                    {
                        selectedOptionIdsStr = model.SelectedOptionId.Value.ToString();
                        var selectedOption = question.Options.FirstOrDefault(o => o.Id == model.SelectedOptionId.Value);
                        isCorrect = selectedOption?.IsCorrect ?? false;
                    }
                    break;

                case QuestionType.MultipleChoice:
                    if (model.SelectedOptionIds != null && model.SelectedOptionIds.Any())
                    {
                        selectedOptionIdsStr = string.Join(",", model.SelectedOptionIds.OrderBy(id => id));
                        var correctOptionIds = question.Options.Where(o => o.IsCorrect).Select(o => o.Id).OrderBy(id => id);
                        var selectedIds = model.SelectedOptionIds.OrderBy(id => id);
                        isCorrect = correctOptionIds.SequenceEqual(selectedIds);
                    }
                    break;

                case QuestionType.TrueFalse:
                    if (model.SelectedOptionId.HasValue)
                    {
                        selectedOptionIdsStr = model.SelectedOptionId.Value.ToString();
                        var selectedOption = question.Options.FirstOrDefault(o => o.Id == model.SelectedOptionId.Value);
                        isCorrect = selectedOption?.IsCorrect ?? false;
                    }
                    break;
            }

            points = isCorrect ? question.Points : 0;

            if (existingAnswer != null)
            {
                existingAnswer.SelectedOptionIds = selectedOptionIdsStr;
                existingAnswer.IsCorrect = isCorrect;
                existingAnswer.Points = points;
                existingAnswer.AnsweredAt = DateTime.Now;
            }
            else
            {
                var answer = new RegularAnswer
                {
                    TestResultId = testResult.Id,
                    QuestionId = question.Id,
                    SelectedOptionIds = selectedOptionIdsStr,
                    IsCorrect = isCorrect,
                    Points = points,
                    AnsweredAt = DateTime.Now
                };

                _context.RegularAnswers.Add(answer);
            }

            await _context.SaveChangesAsync();

            _logger.LogInformation("Студент {StudentId} ответил на вопрос {QuestionId} классического теста {ResultId}. Правильно: {IsCorrect}, Баллы: {Points}",
                student.Id, model.QuestionId, model.TestResultId, isCorrect, points);

            // Возвращаем правильные ответы для проверки
            var correctAnswers = question.Options.Where(o => o.IsCorrect).Select(o => o.Id).ToList();

            return Json(new
            {
                success = true,
                isCorrect = isCorrect,
                points = points,
                correctAnswers = correctAnswers,
                questionType = question.Type.ToString()
            });
        }

        // POST: StudentTest/CompleteRegular
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CompleteRegular(int testResultId)
        {
            try
            {
                var currentUser = await _userManager.GetUserAsync(User);
                var student = await _context.Students
                    .FirstOrDefaultAsync(s => s.UserId == currentUser.Id);

                if (student == null) return NotFound();

                var testResult = await _context.RegularTestResults
                    .Include(tr => tr.RegularAnswers)
                    .FirstOrDefaultAsync(tr => tr.Id == testResultId && tr.StudentId == student.Id);

                if (testResult == null) return NotFound();

                if (testResult.IsCompleted)
                {
                    return RedirectToAction(nameof(RegularResult), new { id = testResult.Id });
                }

                await CompleteRegularTest(testResult);

                _logger.LogInformation("Студент {StudentId} завершил классический тест {ResultId}. Баллы: {Score}/{MaxScore}, Процент: {Percentage}",
                    student.Id, testResultId, testResult.Score, testResult.MaxScore, testResult.Percentage);

                return RedirectToAction(nameof(Result), new { id = testResult.Id });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Ошибка завершения классического теста {ResultId}", testResultId);
                TempData["ErrorMessage"] = "Произошла ошибка при завершении теста.";
                return RedirectToAction(nameof(Index));
            }
        }

        // GET: StudentTest/RegularResult/5
        public async Task<IActionResult> RegularResult(int id)
        {
            var currentUser = await _userManager.GetUserAsync(User);
            var student = await _context.Students
                .FirstOrDefaultAsync(s => s.UserId == currentUser.Id);

            if (student == null) return NotFound();

            var testResult = await _context.RegularTestResults
                .Include(tr => tr.RegularTest)
                    .ThenInclude(rt => rt.RegularQuestions.OrderBy(q => q.OrderIndex))
                        .ThenInclude(q => q.Options.OrderBy(o => o.OrderIndex))
                .Include(tr => tr.RegularAnswers)
                .Include(tr => tr.Student)
                    .ThenInclude(s => s.User)
                .FirstOrDefaultAsync(tr => tr.Id == id && tr.StudentId == student.Id);

            if (testResult == null) return NotFound();

            return View("Result", testResult);
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

            if (testType == null || testType == "orthoepy")
            {
                viewModel.OrthoeopyResults = await _context.OrthoeopyTestResults
                    .Include(tr => tr.OrthoeopyTest)
                    .Where(tr => tr.StudentId == student.Id && tr.IsCompleted)
                    .OrderByDescending(tr => tr.CompletedAt)
                    .ToListAsync();
            }

            if (testType == null || testType == "regular")
            {
                viewModel.RegularResults = await _context.RegularTestResults
                    .Include(tr => tr.RegularTest)
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

        private bool IsOrthoeopyTestAvailable(OrthoeopyTest test, Student student)
        {
            if (test.ClassId.HasValue && test.ClassId != student.ClassId)
                return false;

            if (test.StartDate.HasValue && test.StartDate > DateTime.Now)
                return false;

            if (test.EndDate.HasValue && test.EndDate < DateTime.Now)
                return false;

            return true;
        }

        private bool IsRegularTestAvailable(RegularTest test, Student student)
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
            testResult.Score = testResult.SpellingAnswers.Sum(a => a.Points);
            testResult.Percentage = testResult.MaxScore > 0
                ? Math.Round((double)testResult.Score / testResult.MaxScore * 100, 2)
                : 0;

            await _context.SaveChangesAsync();
        }

        private async Task CompletePunctuationTest(PunctuationTestResult testResult)
        {
            testResult.CompletedAt = DateTime.Now;
            testResult.IsCompleted = true;
            testResult.Score = testResult.PunctuationAnswers.Sum(a => a.Points);
            testResult.Percentage = testResult.MaxScore > 0
                ? Math.Round((double)testResult.Score / testResult.MaxScore * 100, 2)
                : 0;

            await _context.SaveChangesAsync();
        }

        private async Task CompleteOrthoeopyTest(OrthoeopyTestResult testResult)
        {
            testResult.CompletedAt = DateTime.Now;
            testResult.IsCompleted = true;
            testResult.Score = testResult.OrthoeopyAnswers.Sum(a => a.Points);
            testResult.Percentage = testResult.MaxScore > 0
                ? Math.Round((double)testResult.Score / testResult.MaxScore * 100, 2)
                : 0;

            await _context.SaveChangesAsync();
        }

        private async Task CompleteRegularTest(RegularTestResult testResult)
        {
            testResult.CompletedAt = DateTime.Now;
            testResult.IsCompleted = true;
            testResult.Score = testResult.RegularAnswers.Sum(a => a.Points);
            testResult.Percentage = testResult.MaxScore > 0
                ? Math.Round((double)testResult.Score / testResult.MaxScore * 100, 2)
                : 0;

            await _context.SaveChangesAsync();
        }

        #endregion
    }
}
