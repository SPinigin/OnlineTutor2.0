using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
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
        private readonly ILogger<SpellingTestController> _logger;

        public SpellingTestController(ApplicationDbContext context, 
            UserManager<ApplicationUser> userManager, 
            ISpellingQuestionImportService questionImportService,
            ILogger<SpellingTestController> logger)
        {
            _context = context;
            _userManager = userManager;
            _questionImportService = questionImportService;
            _logger = logger;

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
            var currentUser = await _userManager.GetUserAsync(User);

            if (ModelState.IsValid)
            {
                var test = new SpellingTest
                {
                    Title = model.Title,
                    Description = model.Description,
                    TeacherId = currentUser.Id,
                    TestCategoryId = 1,
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

                _logger.LogInformation("Учитель {TeacherId} создал тест орфографии {TestId}: {Title}, ClassId: {ClassId}, TimeLimit: {TimeLimit}",
                    currentUser.Id, test.Id, test.Title, test.ClassId, test.TimeLimit);

                TempData["SuccessMessage"] = $"Тест \"{test.Title}\" успешно создан! Теперь добавьте вопросы.";
                return RedirectToAction(nameof(Details), new { id = test.Id });
            }

            _logger.LogWarning("Учитель {TeacherId} отправил невалидную форму создания теста орфографии", currentUser.Id);

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
            var currentUser = await _userManager.GetUserAsync(User);

            if (ModelState.IsValid)
            {
                try
                {
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

                    _context.Update(test);
                    await _context.SaveChangesAsync();

                    _logger.LogInformation("Учитель {TeacherId} обновил тест орфографии {TestId}: {Title}, ClassId: {ClassId}",
                        currentUser.Id, id, test.Title, test.ClassId);

                    TempData["SuccessMessage"] = $"Тест \"{test.Title}\" успешно обновлен!";
                    return RedirectToAction(nameof(Details), new { id });
                }
                catch (DbUpdateConcurrencyException ex)
                {
                    _logger.LogError(ex, "Ошибка конкурентности при обновлении теста орфографии {TestId} учителем {TeacherId}", id, currentUser.Id);
                    ModelState.AddModelError("", "Произошла ошибка при сохранении. Попробуйте еще раз.");
                }
            }
            else
            {
                _logger.LogWarning("Учитель {TeacherId} отправил невалидную форму обновления теста орфографии {TestId}", currentUser.Id, id);
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
                        .ThenInclude(s => s.User)
                .Include(st => st.TestResults)
                    .ThenInclude(tr => tr.Answers)
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
                _logger.LogWarning("Учитель {TeacherId} попытался удалить тест орфографии {TestId} с результатами ({ResultsCount})",
                    currentUser.Id, id, test.TestResults.Count);
                TempData["ErrorMessage"] = "Нельзя удалить тест, который уже проходили ученики.";
                return RedirectToAction(nameof(Delete), new { id });
            }

            var testTitle = test.Title;
            _context.SpellingTests.Remove(test);
            await _context.SaveChangesAsync();

            _logger.LogInformation("Учитель {TeacherId} удалил тест орфографии {TestId}: {Title}", currentUser.Id, id, testTitle);

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
            return View(new SpellingQuestionImportViewModel { SpellingTestId = id });
        }

        // POST: SpellingTest/ImportQuestions
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ImportQuestions(SpellingQuestionImportViewModel model)
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
                _logger.LogWarning("Учитель {TeacherId} отправил невалидную форму импорта вопросов для теста орфографии {TestId}", currentUser.Id, model.SpellingTestId);
                return View(model);
            }

            try
            {
                // Проверка файла
                if (model.ExcelFile == null || model.ExcelFile.Length == 0)
                {
                    ModelState.AddModelError("ExcelFile", "Выберите файл для импорта");
                    return View(model);
                }

                // Проверка размера файла
                if (model.ExcelFile.Length > 10 * 1024 * 1024)
                {
                    _logger.LogWarning("Учитель {TeacherId} попытался импортировать слишком большой файл ({FileSize} байт) для теста орфографии {TestId}",
                        currentUser.Id, model.ExcelFile.Length, model.SpellingTestId);
                    ModelState.AddModelError("ExcelFile", "Размер файла не должен превышать 10 МБ");
                    return View(model);
                }

                // Проверка расширения
                var allowedExtensions = new[] { ".xlsx", ".xls" };
                var fileExtension = Path.GetExtension(model.ExcelFile.FileName).ToLowerInvariant();
                if (!allowedExtensions.Contains(fileExtension))
                {
                    _logger.LogWarning("Учитель {TeacherId} попытался импортировать файл недопустимого формата ({FileExtension}) для теста орфографии {TestId}",
                        currentUser.Id, fileExtension, model.SpellingTestId);
                    ModelState.AddModelError("ExcelFile", "Поддерживаются только файлы Excel (.xlsx, .xls)");
                    return View(model);
                }

                // Парсинг файла
                List<ImportSpellingQuestionRow> questions;
                try
                {
                    questions = await _questionImportService.ParseExcelFileAsync(model.ExcelFile);
                    _logger.LogDebug("Парсинг завершен. Найдено {QuestionsCount} вопросов", questions?.Count ?? 0);
                }
                catch (Exception parseEx)
                {
                    _logger.LogError(parseEx, "Ошибка парсинга файла импорта для теста орфографии {TestId} учителем {TeacherId}", model.SpellingTestId, currentUser.Id);
                    ModelState.AddModelError("ExcelFile", $"Ошибка при чтении файла Excel: {parseEx.Message}");
                    return View(model);
                }

                if (questions == null || !questions.Any())
                {
                    _logger.LogWarning("Учитель {TeacherId} импортировал пустой файл для теста орфографии {TestId}", currentUser.Id, model.SpellingTestId);
                    TempData["ErrorMessage"] = "Файл не содержит данных для импорта";
                    return View(model);
                }

                var importData = new
                {
                    TestId = model.SpellingTestId,
                    Questions = questions,
                    PointsPerQuestion = model.PointsPerQuestion
                };

                var sessionKey = $"ImportQuestions_{model.SpellingTestId}_{DateTime.Now.Ticks}";
                HttpContext.Session.SetString(sessionKey, JsonSerializer.Serialize(importData));

                TempData["ImportSessionKey"] = sessionKey;

                _logger.LogInformation("Учитель {TeacherId} инициировал импорт {QuestionsCount} вопросов для теста орфографии {TestId}",
                    currentUser.Id, questions.Count, model.SpellingTestId);

                return RedirectToAction(nameof(PreviewQuestions));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Ошибка импорта вопросов для теста орфографии {TestId} учителем {TeacherId}", model.SpellingTestId, currentUser.Id);
                TempData["ErrorMessage"] = $"Произошла ошибка: {ex.Message}";
                return View(model);
            }
        }


        // GET: SpellingTest/PreviewQuestions
        public async Task<IActionResult> PreviewQuestions()
        {
            try
            {
                var sessionKey = TempData["ImportSessionKey"] as string;
                if (string.IsNullOrEmpty(sessionKey))
                {
                    TempData["ErrorMessage"] = "Данные импорта не найдены. Попробуйте еще раз.";
                    return RedirectToAction(nameof(Index));
                }

                var importDataJson = HttpContext.Session.GetString(sessionKey);
                if (string.IsNullOrEmpty(importDataJson))
                {
                    TempData["ErrorMessage"] = "Данные импорта истекли. Попробуйте еще раз.";
                    return RedirectToAction(nameof(Index));
                }

                var importData = JsonSerializer.Deserialize<JsonElement>(importDataJson);
                var testId = importData.GetProperty("TestId").GetInt32();
                var pointsPerQuestion = importData.GetProperty("PointsPerQuestion").GetInt32();

                var currentUser = await _userManager.GetUserAsync(User);
                var test = await _context.SpellingTests
                    .FirstOrDefaultAsync(st => st.Id == testId && st.TeacherId == currentUser.Id);

                if (test == null)
                {
                    TempData["ErrorMessage"] = "Тест не найден";
                    return RedirectToAction(nameof(Index));
                }

                var questionsArray = importData.GetProperty("Questions");
                var questions = new List<ImportSpellingQuestionRow>();

                foreach (var questionElement in questionsArray.EnumerateArray())
                {
                    try
                    {
                        var question = JsonSerializer.Deserialize<ImportSpellingQuestionRow>(questionElement.GetRawText());
                        if (question != null)
                        {
                            questions.Add(question);
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning(ex, "Ошибка десериализации вопроса при предпросмотре импорта для теста {TestId}", testId);
                    }
                }

                ViewBag.Test = test;
                ViewBag.PointsPerQuestion = pointsPerQuestion;

                TempData["ImportSessionKey"] = sessionKey;

                return View(questions);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Ошибка обработки предпросмотра импорта");
                TempData["ErrorMessage"] = $"Ошибка обработки данных: {ex.Message}";
                return RedirectToAction(nameof(Index));
            }
        }

        // POST: SpellingTest/ConfirmImport
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ConfirmImport()
        {
            var sessionKey = TempData["ImportSessionKey"] as string;
            if (string.IsNullOrEmpty(sessionKey))
            {
                TempData["ErrorMessage"] = "Данные импорта не найдены";
                return RedirectToAction(nameof(Index));
            }

            var importDataJson = HttpContext.Session.GetString(sessionKey);
            if (string.IsNullOrEmpty(importDataJson))
            {
                TempData["ErrorMessage"] = "Данные импорта истекли";
                return RedirectToAction(nameof(Index));
            }

            try
            {
                var importData = JsonSerializer.Deserialize<JsonElement>(importDataJson);
                var testId = importData.GetProperty("TestId").GetInt32();
                var pointsPerQuestion = importData.GetProperty("PointsPerQuestion").GetInt32();

                var currentUser = await _userManager.GetUserAsync(User);
                var test = await _context.SpellingTests
                    .Include(st => st.Questions)
                    .FirstOrDefaultAsync(st => st.Id == testId && st.TeacherId == currentUser.Id);

                if (test == null) return NotFound();

                var questionsArray = importData.GetProperty("Questions");
                var validQuestions = new List<ImportSpellingQuestionRow>();
                var orderIndex = test.Questions.Count;

                foreach (var questionElement in questionsArray.EnumerateArray())
                {
                    var questionData = JsonSerializer.Deserialize<ImportSpellingQuestionRow>(questionElement.GetRawText());
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
                HttpContext.Session.Remove(sessionKey);

                _logger.LogInformation("Учитель {TeacherId} подтвердил импорт {QuestionsCount} вопросов для теста орфографии {TestId}",
                    currentUser.Id, validQuestions.Count, testId);

                TempData["SuccessMessage"] = $"Успешно импортировано {validQuestions.Count} вопросов!";
                return RedirectToAction(nameof(Details), new { id = testId });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Ошибка подтверждения импорта вопросов орфографии");
                TempData["ErrorMessage"] = $"Ошибка при импорте: {ex.Message}";
                return RedirectToAction(nameof(Index));
            }
        }

        // GET: SpellingTest/DownloadQuestionTemplate
        public async Task<IActionResult> DownloadQuestionTemplate()
        {
            var currentUser = _userManager.GetUserAsync(User);

            try
            {
                var templateBytes = await _questionImportService.GenerateTemplateAsync();
                return File(templateBytes,
                    "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                    "Шаблон_вопросов_орфография.xlsx");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Ошибка генерации шаблона вопросов орфографии для учителя {TeacherId}", currentUser.Id);
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
                Points = 1
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

                _logger.LogInformation("Учитель {TeacherId} добавил вопрос {QuestionId} к тесту орфографии {TestId}: Слово: {Word}, Правильная буква: {Letter}",
                    currentUser.Id, question.Id, model.SpellingTestId, model.FullWord, model.CorrectLetter);

                TempData["SuccessMessage"] = "Вопрос успешно добавлен!";
                return RedirectToAction(nameof(Details), new { id = model.SpellingTestId });
            }

            _logger.LogWarning("Учитель {TeacherId} отправил невалидную форму добавления вопроса к тесту орфографии {TestId}", currentUser.Id, model.SpellingTestId);

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

            var oldStatus = test.IsActive;
            test.IsActive = !test.IsActive;
            await _context.SaveChangesAsync();

            _logger.LogInformation("Учитель {TeacherId} изменил статус теста орфографии {TestId}: {Title} с {OldStatus} на {NewStatus}",
                currentUser.Id, id, test.Title, oldStatus, test.IsActive);

            var status = test.IsActive ? "активирован" : "деактивирован";
            TempData["InfoMessage"] = $"Тест \"{test.Title}\" {status}.";

            return RedirectToAction(nameof(Details), new { id });
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

                    _logger.LogInformation("Учитель {TeacherId} обновил вопрос орфографии {QuestionId}: Слово: {Word}, Правильная буква: {Letter}",
                        currentUser.Id, id, model.FullWord, model.CorrectLetter);

                    TempData["SuccessMessage"] = "Вопрос успешно обновлен!";
                    return RedirectToAction(nameof(Details), new { id = existingQuestion.SpellingTestId });
                }
                catch (DbUpdateConcurrencyException ex)
                {
                    _logger.LogError(ex, "Ошибка конкурентности при обновлении вопроса орфографии {QuestionId} учителем {TeacherId}", id, currentUser.Id);
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
                .Include(sq => sq.StudentAnswers)
                .FirstOrDefaultAsync(sq => sq.Id == id && sq.SpellingTest.TeacherId == currentUser.Id);

            if (question == null)
                return Json(new { success = false, message = "Вопрос не найден" });

            // Проверяем, есть ли ответы студентов на этот вопрос
            if (question.StudentAnswers.Any())
            {
                _logger.LogWarning("Учитель {TeacherId} попытался удалить вопрос орфографии {QuestionId} с ответами студентов ({AnswersCount})",
                    currentUser.Id, id, question.StudentAnswers.Count);

                return Json(new
                {
                    success = false,
                    message = "Нельзя удалить вопрос, на который уже отвечали ученики"
                });
            }

            try
            {
                var testId = question.SpellingTestId;
                var word = question.FullWord;
                _context.SpellingQuestions.Remove(question);
                await _context.SaveChangesAsync();

                _logger.LogInformation("Учитель {TeacherId} удалил вопрос орфографии {QuestionId}: {Word} из теста {TestId}",
                    currentUser.Id, id, word, testId);

                await ReorderQuestions(testId); // Перенумеровываем оставшиеся вопросы

                return Json(new
                {
                    success = true,
                    message = "Вопрос успешно удален",
                    testId = testId
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Ошибка удаления вопроса орфографии {QuestionId} учителем {TeacherId}", id, currentUser.Id);
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

                _logger.LogInformation("Учитель {TeacherId} изменил порядок {QuestionsCount} вопросов в тесте орфографии {TestId}",
                    currentUser.Id, questionIds.Count, testId);

                return Json(new { success = true, message = "Порядок вопросов обновлен" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Ошибка изменения порядка вопросов в тесте орфографии {TestId} учителем {TeacherId}", testId, currentUser.Id);
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
