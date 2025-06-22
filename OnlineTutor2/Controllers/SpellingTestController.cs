using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using OnlineTutor2.Data;
using OnlineTutor2.Models;
using OnlineTutor2.Services;
using OnlineTutor2.ViewModels;
using System.Text.Json;

namespace OnlineTutor2.Controllers
{
    [Authorize(Roles = ApplicationRoles.Teacher)]
    public class SpellingTestController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly ISpellingQuestionImportService _questionImportService;

        public SpellingTestController(ApplicationDbContext context, UserManager<ApplicationUser> userManager, ISpellingQuestionImportService questionImportService)
        {
            _context = context;
            _userManager = userManager;
            _questionImportService = questionImportService;
        }

        // GET: SpellingTest
        public async Task<IActionResult> Index()
        {
            var currentUser = await _userManager.GetUserAsync(User);
            var tests = await _context.SpellingTests
                .Where(st => st.TeacherId == currentUser.Id)
                .Include(st => st.Class)
                .Include(st => st.Questions)
                .OrderByDescending(st => st.CreatedAt)
                .ToListAsync();

            return View(tests);
        }

        // GET: SpellingTest/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null) return NotFound();

            var currentUser = await _userManager.GetUserAsync(User);
            var test = await _context.SpellingTests
                .Include(st => st.Teacher)
                .Include(st => st.Class)
                .Include(st => st.Questions.OrderBy(q => q.OrderIndex))
                .Include(st => st.TestResults)
                    .ThenInclude(tr => tr.Student)
                        .ThenInclude(s => s.User)
                .Include(st => st.TestResults)
                    .ThenInclude(tr => tr.Answers)
                .FirstOrDefaultAsync(st => st.Id == id && st.TeacherId == currentUser.Id);

            if (test == null) return NotFound();

            return View(test);
        }

        // GET: SpellingTest/Create
        public async Task<IActionResult> Create()
        {
            await LoadClasses();
            return View();
        }

        // POST: SpellingTest/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(CreateSpellingTestViewModel model)
        {
            if (ModelState.IsValid)
            {
                var currentUser = await _userManager.GetUserAsync(User);

                var test = new SpellingTest
                {
                    Title = model.Title,
                    Description = model.Description,
                    TeacherId = currentUser.Id,
                    TestCategoryId = 1, // Добавьте эту строку - ID категории "Тесты на правописание"
                    ClassId = model.ClassId,
                    TimeLimit = model.TimeLimit,
                    MaxAttempts = model.MaxAttempts,
                    StartDate = model.StartDate,
                    EndDate = model.EndDate,
                    ShowHints = model.ShowHints,
                    ShowCorrectAnswers = model.ShowCorrectAnswers,
                    IsActive = model.IsActive
                };

                _context.SpellingTests.Add(test);
                await _context.SaveChangesAsync();

                TempData["SuccessMessage"] = $"Тест \"{test.Title}\" успешно создан! Теперь добавьте вопросы.";
                return RedirectToAction(nameof(Details), new { id = test.Id });
            }

            await LoadClasses();
            return View(model);
        }

        // GET: SpellingTest/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null) return NotFound();

            var currentUser = await _userManager.GetUserAsync(User);
            var test = await _context.SpellingTests
                .FirstOrDefaultAsync(st => st.Id == id && st.TeacherId == currentUser.Id);

            if (test == null) return NotFound();

            var model = new CreateSpellingTestViewModel
            {
                Title = test.Title,
                Description = test.Description,
                ClassId = test.ClassId,
                TimeLimit = test.TimeLimit,
                MaxAttempts = test.MaxAttempts,
                StartDate = test.StartDate,
                EndDate = test.EndDate,
                ShowHints = test.ShowHints,
                ShowCorrectAnswers = test.ShowCorrectAnswers,
                IsActive = test.IsActive
            };

            await LoadClasses();
            ViewBag.TestId = id;
            return View(model);
        }

        // POST: SpellingTest/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, CreateSpellingTestViewModel model)
        {
            if (ModelState.IsValid)
            {
                try
                {
                    var currentUser = await _userManager.GetUserAsync(User);
                    var test = await _context.SpellingTests
                        .FirstOrDefaultAsync(st => st.Id == id && st.TeacherId == currentUser.Id);

                    if (test == null) return NotFound();

                    test.Title = model.Title;
                    test.Description = model.Description;
                    test.ClassId = model.ClassId;
                    test.TimeLimit = model.TimeLimit;
                    test.MaxAttempts = model.MaxAttempts;
                    test.StartDate = model.StartDate;
                    test.EndDate = model.EndDate;
                    test.ShowHints = model.ShowHints;
                    test.ShowCorrectAnswers = model.ShowCorrectAnswers;
                    test.IsActive = model.IsActive;
                    // TestCategoryId не изменяем при редактировании

                    _context.Update(test);
                    await _context.SaveChangesAsync();

                    TempData["SuccessMessage"] = $"Тест \"{test.Title}\" успешно обновлен!";
                    return RedirectToAction(nameof(Details), new { id });
                }
                catch (DbUpdateConcurrencyException)
                {
                    ModelState.AddModelError("", "Произошла ошибка при сохранении. Попробуйте еще раз.");
                }
            }

            await LoadClasses();
            ViewBag.TestId = id;
            return View(model);
        }

        // GET: SpellingTest/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null) return NotFound();

            var currentUser = await _userManager.GetUserAsync(User);
            var test = await _context.SpellingTests
                .Include(st => st.Class)
                .Include(st => st.Questions)
                .Include(st => st.TestResults)
                    .ThenInclude(tr => tr.Student)
                        .ThenInclude(s => s.User) // Добавляем загрузку пользователя
                .Include(st => st.TestResults)
                    .ThenInclude(tr => tr.Answers) // Добавляем загрузку ответов
                .FirstOrDefaultAsync(st => st.Id == id && st.TeacherId == currentUser.Id);

            if (test == null) return NotFound();

            return View(test);
        }

        // POST: SpellingTest/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var currentUser = await _userManager.GetUserAsync(User);
            var test = await _context.SpellingTests
                .Include(st => st.TestResults)
                .FirstOrDefaultAsync(st => st.Id == id && st.TeacherId == currentUser.Id);

            if (test == null) return NotFound();

            if (test.TestResults.Any())
            {
                TempData["ErrorMessage"] = "Нельзя удалить тест, который уже проходили ученики.";
                return RedirectToAction(nameof(Delete), new { id });
            }

            _context.SpellingTests.Remove(test);
            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = $"Тест \"{test.Title}\" успешно удален!";
            return RedirectToAction(nameof(Index));
        }

        private async Task LoadClasses()
        {
            var currentUser = await _userManager.GetUserAsync(User);
            var classes = await _context.Classes
                .Where(c => c.TeacherId == currentUser.Id && c.IsActive)
                .ToListAsync();
            ViewBag.Classes = new SelectList(classes, "Id", "Name");
        }

        // GET: SpellingTest/ImportQuestions/5
        public async Task<IActionResult> ImportQuestions(int id)
        {
            var currentUser = await _userManager.GetUserAsync(User);
            var test = await _context.SpellingTests
                .FirstOrDefaultAsync(st => st.Id == id && st.TeacherId == currentUser.Id);

            if (test == null) return NotFound();

            ViewBag.Test = test;
            return View(new QuestionImportViewModel { SpellingTestId = id });
        }

        // POST: SpellingTest/ImportQuestions
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ImportQuestions(QuestionImportViewModel model)
        {
            var currentUser = await _userManager.GetUserAsync(User);
            var test = await _context.SpellingTests
                .FirstOrDefaultAsync(st => st.Id == model.SpellingTestId && st.TeacherId == currentUser.Id);

            if (test == null)
            {
                TempData["ErrorMessage"] = "Тест не найден";
                return RedirectToAction(nameof(Index));
            }

            ViewBag.Test = test;

            if (!ModelState.IsValid)
            {
                return View(model);
            }

            try
            {
                // Детальное логирование
                Console.WriteLine($"[DEBUG] Starting import for test {model.SpellingTestId}");

                // Проверка файла
                if (model.ExcelFile == null || model.ExcelFile.Length == 0)
                {
                    ModelState.AddModelError("ExcelFile", "Выберите файл для импорта");
                    return View(model);
                }

                Console.WriteLine($"[DEBUG] File info: Name={model.ExcelFile.FileName}, Size={model.ExcelFile.Length}, ContentType={model.ExcelFile.ContentType}");

                // Проверка размера файла
                if (model.ExcelFile.Length > 10 * 1024 * 1024)
                {
                    ModelState.AddModelError("ExcelFile", "Размер файла не должен превышать 10 МБ");
                    return View(model);
                }

                // Проверка расширения
                var allowedExtensions = new[] { ".xlsx", ".xls" };
                var fileExtension = Path.GetExtension(model.ExcelFile.FileName).ToLowerInvariant();
                if (!allowedExtensions.Contains(fileExtension))
                {
                    ModelState.AddModelError("ExcelFile", "Поддерживаются только файлы Excel (.xlsx, .xls)");
                    return View(model);
                }

                Console.WriteLine($"[DEBUG] File extension check passed: {fileExtension}");

                // Проверяем, что сервис доступен
                if (_questionImportService == null)
                {
                    throw new InvalidOperationException("Сервис импорта не доступен");
                }

                Console.WriteLine("[DEBUG] Starting file parsing...");

                // Парсинг файла с дополнительной обработкой ошибок
                List<ImportQuestionRow> questions;
                try
                {
                    questions = await _questionImportService.ParseExcelFileAsync(model.ExcelFile);
                    Console.WriteLine($"[DEBUG] Parsing completed. Found {questions?.Count ?? 0} questions");
                }
                catch (Exception parseEx)
                {
                    Console.WriteLine($"[ERROR] Parse error: {parseEx}");
                    ModelState.AddModelError("ExcelFile", $"Ошибка при чтении файла Excel: {parseEx.Message}");
                    return View(model);
                }

                if (questions == null || !questions.Any())
                {
                    TempData["ErrorMessage"] = "Файл не содержит данных для импорта";
                    return View(model);
                }

                Console.WriteLine("[DEBUG] Creating import data for TempData...");

                // Сохраняем данные в TempData
                try
                {
                    var importData = new
                    {
                        TestId = model.SpellingTestId,
                        Questions = questions,
                        PointsPerQuestion = model.PointsPerQuestion
                    };

                    var jsonString = System.Text.Json.JsonSerializer.Serialize(importData);
                    TempData["ImportQuestions"] = jsonString;

                    Console.WriteLine($"[DEBUG] TempData saved, JSON length: {jsonString.Length}");
                    Console.WriteLine("[DEBUG] Redirecting to PreviewQuestions...");

                    return RedirectToAction(nameof(PreviewQuestions));
                }
                catch (Exception jsonEx)
                {
                    Console.WriteLine($"[ERROR] JSON serialization error: {jsonEx}");
                    TempData["ErrorMessage"] = "Ошибка при подготовке данных для предварительного просмотра";
                    return View(model);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[ERROR] General error in ImportQuestions: {ex}");
                TempData["ErrorMessage"] = $"Произошла ошибка: {ex.Message}";
                return View(model);
            }
        }

        // GET: SpellingTest/PreviewQuestions
        public async Task<IActionResult> PreviewQuestions()
        {
            try
            {
                Console.WriteLine("[DEBUG] PreviewQuestions called");

                var importDataJson = TempData["ImportQuestions"] as string;
                if (string.IsNullOrEmpty(importDataJson))
                {
                    Console.WriteLine("[ERROR] No import data found in TempData");
                    TempData["ErrorMessage"] = "Данные импорта не найдены. Попробуйте еще раз.";
                    return RedirectToAction(nameof(Index));
                }

                Console.WriteLine($"[DEBUG] Import data JSON length: {importDataJson.Length}");

                var importData = System.Text.Json.JsonSerializer.Deserialize<JsonElement>(importDataJson);
                var testId = importData.GetProperty("TestId").GetInt32();
                var pointsPerQuestion = importData.GetProperty("PointsPerQuestion").GetInt32();

                Console.WriteLine($"[DEBUG] TestId: {testId}, PointsPerQuestion: {pointsPerQuestion}");

                var currentUser = await _userManager.GetUserAsync(User);
                var test = await _context.SpellingTests
                    .FirstOrDefaultAsync(st => st.Id == testId && st.TeacherId == currentUser.Id);

                if (test == null)
                {
                    Console.WriteLine("[ERROR] Test not found");
                    TempData["ErrorMessage"] = "Тест не найден";
                    return RedirectToAction(nameof(Index));
                }

                var questionsArray = importData.GetProperty("Questions");
                var questions = new List<ImportQuestionRow>();

                foreach (var questionElement in questionsArray.EnumerateArray())
                {
                    try
                    {
                        var question = System.Text.Json.JsonSerializer.Deserialize<ImportQuestionRow>(questionElement.GetRawText());
                        if (question != null)
                        {
                            questions.Add(question);
                        }
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"[ERROR] Error deserializing question: {ex.Message}");
                    }
                }

                Console.WriteLine($"[DEBUG] Questions deserialized: {questions.Count}");

                ViewBag.Test = test;
                ViewBag.PointsPerQuestion = pointsPerQuestion;
                TempData["ImportQuestions"] = importDataJson; // Сохраняем обратно

                return View(questions);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[ERROR] PreviewQuestions error: {ex}");
                TempData["ErrorMessage"] = $"Ошибка обработки данных: {ex.Message}";
                return RedirectToAction(nameof(Index));
            }
        }

        // POST: SpellingTest/ConfirmImport
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ConfirmImport()
        {
            var importDataJson = TempData["ImportQuestions"] as string;
            if (string.IsNullOrEmpty(importDataJson))
            {
                TempData["ErrorMessage"] = "Данные импорта не найдены";
                return RedirectToAction(nameof(Index));
            }

            try
            {
                var importData = System.Text.Json.JsonSerializer.Deserialize<JsonElement>(importDataJson);
                var testId = importData.GetProperty("TestId").GetInt32();
                var pointsPerQuestion = importData.GetProperty("PointsPerQuestion").GetInt32();

                var currentUser = await _userManager.GetUserAsync(User);
                var test = await _context.SpellingTests
                    .Include(st => st.Questions)
                    .FirstOrDefaultAsync(st => st.Id == testId && st.TeacherId == currentUser.Id);

                if (test == null) return NotFound();

                var questionsArray = importData.GetProperty("Questions");
                var validQuestions = new List<ImportQuestionRow>();
                var orderIndex = test.Questions.Count; // Продолжаем нумерацию

                foreach (var questionElement in questionsArray.EnumerateArray())
                {
                    var questionData = System.Text.Json.JsonSerializer.Deserialize<ImportQuestionRow>(questionElement.GetRawText());
                    if (questionData.IsValid)
                    {
                        validQuestions.Add(questionData);
                    }
                }

                // Создаем вопросы в базе данных
                foreach (var questionData in validQuestions)
                {
                    var question = new SpellingQuestion
                    {
                        SpellingTestId = testId,
                        WordWithGap = questionData.WordWithGap,
                        CorrectLetter = questionData.CorrectLetter,
                        FullWord = questionData.FullWord,
                        Hint = questionData.Hint,
                        Points = pointsPerQuestion,
                        OrderIndex = ++orderIndex
                    };

                    _context.SpellingQuestions.Add(question);
                }

                await _context.SaveChangesAsync();

                TempData["SuccessMessage"] = $"Успешно импортировано {validQuestions.Count} вопросов!";
                return RedirectToAction(nameof(Details), new { id = testId });
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = $"Ошибка при импорте: {ex.Message}";
                return RedirectToAction(nameof(Index));
            }
        }

        // GET: SpellingTest/DownloadQuestionTemplate
        public async Task<IActionResult> DownloadQuestionTemplate()
        {
            try
            {
                var templateBytes = await _questionImportService.GenerateTemplateAsync();
                return File(templateBytes,
                    "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                    "Шаблон_вопросов_правописание.xlsx");
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = $"Ошибка генерации шаблона: {ex.Message}";
                return RedirectToAction(nameof(Index));
            }
        }

        // GET: SpellingTest/AddQuestion/5
        public async Task<IActionResult> AddQuestion(int id)
        {
            var currentUser = await _userManager.GetUserAsync(User);
            var test = await _context.SpellingTests
                .Include(st => st.Questions)
                .FirstOrDefaultAsync(st => st.Id == id && st.TeacherId == currentUser.Id);

            if (test == null) return NotFound();

            // Создаем модель с уже заполненными данными
            var model = new SpellingQuestion
            {
                SpellingTestId = test.Id,
                OrderIndex = test.Questions.Count + 1,
                Points = 1 // Значение по умолчанию
            };

            ViewBag.Test = test;
            ViewBag.NextOrderIndex = test.Questions.Count + 1;

            return View(model);
        }

        // POST: SpellingTest/AddQuestion
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AddQuestion(SpellingQuestion model)
        {
            var currentUser = await _userManager.GetUserAsync(User);
            var test = await _context.SpellingTests
                .Include(st => st.Questions)
                .FirstOrDefaultAsync(st => st.Id == model.SpellingTestId && st.TeacherId == currentUser.Id);

            if (test == null) return NotFound();

            // Убираем проверку навигационного свойства из валидации
            ModelState.Remove("SpellingTest");

            if (ModelState.IsValid)
            {
                // Создаем новый объект без Id (чтобы EF сгенерировал его автоматически)
                var question = new SpellingQuestion
                {
                    SpellingTestId = model.SpellingTestId,
                    WordWithGap = model.WordWithGap,
                    CorrectLetter = model.CorrectLetter,
                    FullWord = model.FullWord,
                    Hint = model.Hint,
                    Points = model.Points > 0 ? model.Points : 1,
                    OrderIndex = model.OrderIndex > 0 ? model.OrderIndex : test.Questions.Count + 1
                };

                _context.SpellingQuestions.Add(question);
                await _context.SaveChangesAsync();

                TempData["SuccessMessage"] = "Вопрос успешно добавлен!";
                return RedirectToAction(nameof(Details), new { id = model.SpellingTestId });
            }

            // При ошибке валидации перезагружаем данные для представления
            ViewBag.Test = test;
            ViewBag.NextOrderIndex = test.Questions.Count + 1;
            return View(model);
        }

        // POST: SpellingTest/ToggleStatus/5
        [HttpPost]
        public async Task<IActionResult> ToggleStatus(int id)
        {
            var currentUser = await _userManager.GetUserAsync(User);
            var test = await _context.SpellingTests
                .FirstOrDefaultAsync(st => st.Id == id && st.TeacherId == currentUser.Id);

            if (test == null) return NotFound();

            test.IsActive = !test.IsActive;
            await _context.SaveChangesAsync();

            var status = test.IsActive ? "активирован" : "деактивирован";
            TempData["InfoMessage"] = $"Тест \"{test.Title}\" {status}.";

            return RedirectToAction(nameof(Details), new { id });
        }

        // GET: SpellingTest/Analytics/5 - Аналитика теста
        public async Task<IActionResult> Analytics(int id)
        {
            var currentUser = await _userManager.GetUserAsync(User);
            var test = await _context.SpellingTests
                .Include(st => st.Teacher)
                .Include(st => st.Class)
                    .ThenInclude(c => c.Students)
                        .ThenInclude(s => s.User)
                .Include(st => st.Questions.OrderBy(q => q.OrderIndex))
                .Include(st => st.TestResults)
                    .ThenInclude(tr => tr.Student)
                        .ThenInclude(s => s.User)
                .Include(st => st.TestResults)
                    .ThenInclude(tr => tr.Answers)
                        .ThenInclude(a => a.Question)
                .FirstOrDefaultAsync(st => st.Id == id && st.TeacherId == currentUser.Id);

            if (test == null) return NotFound();

            var analytics = await BuildAnalyticsAsync(test);
            return View(analytics);
        }

        private async Task<SpellingTestAnalyticsViewModel> BuildAnalyticsAsync(SpellingTest test)
        {
            var analytics = new SpellingTestAnalyticsViewModel
            {
                Test = test
            };

            // Получаем всех учеников класса (если тест назначен классу)
            var allStudents = new List<Student>();
            if (test.Class != null)
            {
                allStudents = test.Class.Students.ToList();
            }
            else
            {
                // Если тест доступен всем, получаем всех учеников учителя
                var teacherClassIds = await _context.Classes
                    .Where(c => c.TeacherId == test.TeacherId)
                    .Select(c => c.Id)
                    .ToListAsync();

                allStudents = await _context.Students
                    .Include(s => s.User)
                    .Where(s => s.ClassId.HasValue && teacherClassIds.Contains(s.ClassId.Value))
                    .ToListAsync();
            }

            // Строим статистику
            analytics.Statistics = BuildStatistics(test, allStudents);

            // Строим результаты учеников
            analytics.StudentResults = BuildStudentResults(test, allStudents);

            // Ученики, которые не проходили тест
            var studentsWithResults = test.TestResults.Select(tr => tr.StudentId).Distinct().ToList();
            analytics.StudentsNotTaken = allStudents
                .Where(s => !studentsWithResults.Contains(s.Id))
                .ToList();

            // Аналитика по вопросам
            analytics.QuestionAnalytics = BuildQuestionAnalytics(test);

            return analytics;
        }

        private TestStatistics BuildStatistics(SpellingTest test, List<Student> allStudents)
        {
            var completedResults = test.TestResults.Where(tr => tr.IsCompleted).ToList();
            var inProgressResults = test.TestResults.Where(tr => !tr.IsCompleted).ToList();
            var studentsWithResults = test.TestResults.Select(tr => tr.StudentId).Distinct().Count();

            var stats = new TestStatistics
            {
                TotalStudents = allStudents.Count,
                StudentsCompleted = completedResults.Select(tr => tr.StudentId).Distinct().Count(),
                StudentsInProgress = inProgressResults.Select(tr => tr.StudentId).Distinct().Count(),
                StudentsNotStarted = allStudents.Count - studentsWithResults
            };

            if (completedResults.Any())
            {
                stats.AverageScore = Math.Round(completedResults.Average(tr => tr.Score), 1);
                stats.AveragePercentage = Math.Round(completedResults.Average(tr => tr.Percentage), 1);
                stats.HighestScore = completedResults.Max(tr => tr.Score);
                stats.LowestScore = completedResults.Min(tr => tr.Score);
                stats.FirstCompletion = completedResults.Min(tr => tr.CompletedAt);
                stats.LastCompletion = completedResults.Max(tr => tr.CompletedAt);

                // Среднее время выполнения
                var completionTimes = completedResults
                    .Where(tr => tr.CompletedAt.HasValue)
                    .Select(tr => tr.CompletedAt.Value - tr.StartedAt)
                    .ToList();

                if (completionTimes.Any())
                {
                    var averageTicks = (long)completionTimes.Average(ts => ts.Ticks);
                    stats.AverageCompletionTime = new TimeSpan(averageTicks);
                }

                // Распределение оценок
                stats.GradeDistribution = new Dictionary<string, int>
                {
                    ["Отлично (80-100%)"] = completedResults.Count(tr => tr.Percentage >= 80),
                    ["Хорошо (60-79%)"] = completedResults.Count(tr => tr.Percentage >= 60 && tr.Percentage < 80),
                    ["Удовлетворительно (40-59%)"] = completedResults.Count(tr => tr.Percentage >= 40 && tr.Percentage < 60),
                    ["Неудовлетворительно (0-39%)"] = completedResults.Count(tr => tr.Percentage < 40)
                };
            }

            return stats;
        }

        private List<StudentTestResultViewModel> BuildStudentResults(SpellingTest test, List<Student> allStudents)
        {
            var studentResults = new List<StudentTestResultViewModel>();

            foreach (var student in allStudents)
            {
                var results = test.TestResults.Where(tr => tr.StudentId == student.Id).ToList();
                var completedResults = results.Where(tr => tr.IsCompleted).ToList();

                var studentResult = new StudentTestResultViewModel
                {
                    Student = student,
                    Results = results,
                    AttemptsUsed = results.Count,
                    HasCompleted = completedResults.Any(),
                    IsInProgress = results.Any(tr => !tr.IsCompleted)
                };

                if (completedResults.Any())
                {
                    studentResult.BestResult = completedResults.OrderByDescending(tr => tr.Percentage).First();
                    studentResult.LatestResult = completedResults.OrderByDescending(tr => tr.CompletedAt).First();

                    // Общее время, потраченное на все попытки
                    var totalTime = completedResults
                        .Where(tr => tr.CompletedAt.HasValue)
                        .Sum(tr => (tr.CompletedAt.Value - tr.StartedAt).Ticks);

                    if (totalTime > 0)
                    {
                        studentResult.TotalTimeSpent = new TimeSpan(totalTime);
                    }
                }

                studentResults.Add(studentResult);
            }

            return studentResults.OrderBy(sr => sr.Student.User.LastName).ToList();
        }

        private List<QuestionAnalyticsViewModel> BuildQuestionAnalytics(SpellingTest test)
        {
            var questionAnalytics = new List<QuestionAnalyticsViewModel>();

            foreach (var question in test.Questions.OrderBy(q => q.OrderIndex))
            {
                var answers = test.TestResults
                    .SelectMany(tr => tr.Answers)
                    .Where(a => a.SpellingQuestionId == question.Id)
                    .ToList();

                var analytics = new QuestionAnalyticsViewModel
                {
                    Question = question,
                    TotalAnswers = answers.Count,
                    CorrectAnswers = answers.Count(a => a.IsCorrect),
                    IncorrectAnswers = answers.Count(a => !a.IsCorrect)
                };

                if (answers.Any())
                {
                    analytics.SuccessRate = Math.Round((double)analytics.CorrectAnswers / analytics.TotalAnswers * 100, 1);

                    // Анализ частых ошибок
                    var incorrectAnswers = answers
                        .Where(a => !a.IsCorrect && !string.IsNullOrEmpty(a.StudentAnswer))
                        .GroupBy(a => a.StudentAnswer.ToLower())
                        .Select(g => new CommonMistakeViewModel
                        {
                            IncorrectAnswer = g.Key,
                            Count = g.Count(),
                            Percentage = Math.Round((double)g.Count() / analytics.IncorrectAnswers * 100, 1),
                            StudentNames = g.Select(a => a.TestResult.Student.User.FullName).ToList()
                        })
                        .OrderByDescending(m => m.Count)
                        .Take(5)
                        .ToList();

                    analytics.CommonMistakes = incorrectAnswers;
                }

                questionAnalytics.Add(analytics);
            }

            // Отмечаем самые сложные и легкие вопросы
            if (questionAnalytics.Any(qa => qa.TotalAnswers > 0))
            {
                var lowestSuccessRate = questionAnalytics.Where(qa => qa.TotalAnswers > 0).Min(qa => qa.SuccessRate);
                var highestSuccessRate = questionAnalytics.Where(qa => qa.TotalAnswers > 0).Max(qa => qa.SuccessRate);

                foreach (var qa in questionAnalytics)
                {
                    if (qa.TotalAnswers > 0)
                    {
                        qa.IsMostDifficult = qa.SuccessRate == lowestSuccessRate;
                        qa.IsEasiest = qa.SuccessRate == highestSuccessRate;
                    }
                }
            }

            return questionAnalytics;
        }

        // GET: SpellingTest/EditQuestion/5
        public async Task<IActionResult> EditQuestion(int? id)
        {
            if (id == null) return NotFound();

            var currentUser = await _userManager.GetUserAsync(User);
            var question = await _context.SpellingQuestions
                .Include(sq => sq.SpellingTest)
                .FirstOrDefaultAsync(sq => sq.Id == id && sq.SpellingTest.TeacherId == currentUser.Id);

            if (question == null) return NotFound();

            ViewBag.Test = question.SpellingTest;
            return View(question);
        }

        // POST: SpellingTest/EditQuestion/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditQuestion(int id, SpellingQuestion model)
        {
            if (id != model.Id) return NotFound();

            var currentUser = await _userManager.GetUserAsync(User);
            var existingQuestion = await _context.SpellingQuestions
                .Include(sq => sq.SpellingTest)
                .FirstOrDefaultAsync(sq => sq.Id == id && sq.SpellingTest.TeacherId == currentUser.Id);

            if (existingQuestion == null) return NotFound();

            // Убираем проверку навигационного свойства из валидации
            ModelState.Remove("SpellingTest");

            if (ModelState.IsValid)
            {
                try
                {
                    existingQuestion.WordWithGap = model.WordWithGap;
                    existingQuestion.CorrectLetter = model.CorrectLetter;
                    existingQuestion.FullWord = model.FullWord;
                    existingQuestion.Hint = model.Hint;
                    existingQuestion.Points = model.Points > 0 ? model.Points : 1;
                    existingQuestion.OrderIndex = model.OrderIndex > 0 ? model.OrderIndex : existingQuestion.OrderIndex;

                    _context.Update(existingQuestion);
                    await _context.SaveChangesAsync();

                    TempData["SuccessMessage"] = "Вопрос успешно обновлен!";
                    return RedirectToAction(nameof(Details), new { id = existingQuestion.SpellingTestId });
                }
                catch (DbUpdateConcurrencyException)
                {
                    ModelState.AddModelError("", "Произошла ошибка при сохранении. Попробуйте еще раз.");
                }
            }

            ViewBag.Test = existingQuestion.SpellingTest;
            return View(model);
        }

        // POST: SpellingTest/DeleteQuestion/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteQuestion(int id)
        {
            var currentUser = await _userManager.GetUserAsync(User);
            var question = await _context.SpellingQuestions
                .Include(sq => sq.SpellingTest)
                .Include(sq => sq.StudentAnswers) // Проверяем, есть ли ответы студентов
                .FirstOrDefaultAsync(sq => sq.Id == id && sq.SpellingTest.TeacherId == currentUser.Id);

            if (question == null)
                return Json(new { success = false, message = "Вопрос не найден" });

            // Проверяем, есть ли ответы студентов на этот вопрос
            if (question.StudentAnswers.Any())
            {
                return Json(new
                {
                    success = false,
                    message = "Нельзя удалить вопрос, на который уже отвечали ученики"
                });
            }

            try
            {
                var testId = question.SpellingTestId;
                _context.SpellingQuestions.Remove(question);
                await _context.SaveChangesAsync();

                // Перенумеровываем оставшиеся вопросы
                await ReorderQuestions(testId);

                return Json(new
                {
                    success = true,
                    message = "Вопрос успешно удален",
                    testId = testId
                });
            }
            catch (Exception ex)
            {
                return Json(new
                {
                    success = false,
                    message = "Ошибка при удалении вопроса: " + ex.Message
                });
            }
        }

        // POST: SpellingTest/ReorderQuestions
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ReorderQuestions(int testId, List<int> questionIds)
        {
            var currentUser = await _userManager.GetUserAsync(User);
            var test = await _context.SpellingTests
                .Include(st => st.Questions)
                .FirstOrDefaultAsync(st => st.Id == testId && st.TeacherId == currentUser.Id);

            if (test == null)
                return Json(new { success = false, message = "Тест не найден" });

            try
            {
                for (int i = 0; i < questionIds.Count; i++)
                {
                    var question = test.Questions.FirstOrDefault(q => q.Id == questionIds[i]);
                    if (question != null)
                    {
                        question.OrderIndex = i + 1;
                    }
                }

                await _context.SaveChangesAsync();
                return Json(new { success = true, message = "Порядок вопросов обновлен" });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Ошибка: " + ex.Message });
            }
        }

        // Вспомогательный метод для перенумерации вопросов
        private async Task ReorderQuestions(int testId)
        {
            var questions = await _context.SpellingQuestions
                .Where(q => q.SpellingTestId == testId)
                .OrderBy(q => q.OrderIndex)
                .ToListAsync();

            for (int i = 0; i < questions.Count; i++)
            {
                questions[i].OrderIndex = i + 1;
            }

            await _context.SaveChangesAsync();
        }
    }
}
