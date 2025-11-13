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
        #region Fields and Constructor

        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly ISpellingQuestionImportService _questionImportService;
        private readonly ILogger<SpellingTestController> _logger;

        public SpellingTestController(
            ApplicationDbContext context,
            UserManager<ApplicationUser> userManager,
            ISpellingQuestionImportService questionImportService,
            ILogger<SpellingTestController> logger)
        {
            _context = context;
            _userManager = userManager;
            _questionImportService = questionImportService;
            _logger = logger;
        }

        #endregion

        #region Test CRUD Operations

        // GET: SpellingTest
        public async Task<IActionResult> Index()
        {
            var currentUser = await _userManager.GetUserAsync(User);
            var tests = await _context.SpellingTests
                .Where(st => st.TeacherId == currentUser.Id)
                .Include(st => st.Class)
                .Include(st => st.SpellingQuestions)
                .Include(st => st.SpellingTestResults)
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
                .Include(st => st.SpellingQuestions.OrderBy(q => q.OrderIndex))
                .Include(st => st.SpellingTestResults)
                    .ThenInclude(tr => tr.Student)
                        .ThenInclude(s => s.User)
                .Include(st => st.SpellingTestResults)
                    .ThenInclude(tr => tr.SpellingAnswers)
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
                    TestCategoryId = TestCategoryConstants.Spelling,
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

                _logger.LogInformation(
                    "Учитель {TeacherId} создал тест орфографии {TestId}: {Title}",
                    currentUser.Id, test.Id, test.Title);

                TempData["SuccessMessage"] = $"Тест \"{test.Title}\" успешно создан! Теперь добавьте вопросы.";
                return RedirectToAction(nameof(Details), new { id = test.Id });
            }

            _logger.LogWarning(
                "Учитель {TeacherId} отправил невалидную форму создания теста орфографии",
                currentUser.Id);

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

                    _logger.LogInformation(
                        "Учитель {TeacherId} обновил тест орфографии {TestId}: {Title}",
                        currentUser.Id, id, test.Title);

                    TempData["SuccessMessage"] = $"Тест \"{test.Title}\" успешно обновлен!";
                    return RedirectToAction(nameof(Details), new { id });
                }
                catch (DbUpdateConcurrencyException ex)
                {
                    _logger.LogError(ex,
                        "Ошибка конкурентности при обновлении теста орфографии {TestId}",
                        id);
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
                .Include(st => st.SpellingQuestions)
                .Include(st => st.SpellingTestResults)
                    .ThenInclude(tr => tr.Student)
                        .ThenInclude(s => s.User)
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
                .Include(st => st.SpellingTestResults)
                .FirstOrDefaultAsync(st => st.Id == id && st.TeacherId == currentUser.Id);

            if (test == null) return NotFound();

            if (test.SpellingTestResults.Any())
            {
                _logger.LogWarning(
                    "Учитель {TeacherId} попытался удалить тест орфографии {TestId} с результатами",
                    currentUser.Id, id);
                TempData["ErrorMessage"] = "Нельзя удалить тест, который уже проходили ученики.";
                return RedirectToAction(nameof(Delete), new { id });
            }

            var testTitle = test.Title;
            _context.SpellingTests.Remove(test);
            await _context.SaveChangesAsync();

            _logger.LogInformation(
                "Учитель {TeacherId} удалил тест орфографии {TestId}: {Title}",
                currentUser.Id, id, testTitle);

            TempData["SuccessMessage"] = $"Тест \"{testTitle}\" успешно удален!";
            return RedirectToAction("Category", "Test", new { id = TestCategoryConstants.Spelling });
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

            _logger.LogInformation(
                "Учитель {TeacherId} изменил статус теста орфографии {TestId} на {Status}",
                currentUser.Id, id, test.IsActive);

            var status = test.IsActive ? "активирован" : "деактивирован";
            TempData["InfoMessage"] = $"Тест \"{test.Title}\" {status}.";

            return RedirectToAction(nameof(Details), new { id });
        }

        #endregion

        #region Question Management

        // GET: SpellingTest/AddQuestion/5
        public async Task<IActionResult> AddQuestion(int id)
        {
            var currentUser = await _userManager.GetUserAsync(User);
            var test = await _context.SpellingTests
                .Include(st => st.SpellingQuestions)
                .FirstOrDefaultAsync(st => st.Id == id && st.TeacherId == currentUser.Id);

            if (test == null) return NotFound();

            var model = new SpellingQuestion
            {
                SpellingTestId = test.Id,
                OrderIndex = test.SpellingQuestions.Count + 1,
                Points = 1
            };

            ViewBag.Test = test;
            return View(model);
        }

        // POST: SpellingTest/AddQuestion
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AddQuestion(SpellingQuestion model)
        {
            var currentUser = await _userManager.GetUserAsync(User);
            var test = await _context.SpellingTests
                .Include(st => st.SpellingQuestions)
                .FirstOrDefaultAsync(st => st.Id == model.SpellingTestId && st.TeacherId == currentUser.Id);

            if (test == null) return NotFound();

            ModelState.Remove("SpellingTest");

            if (ModelState.IsValid)
            {
                var question = new SpellingQuestion
                {
                    SpellingTestId = model.SpellingTestId,
                    WordWithGap = model.WordWithGap,
                    CorrectLetter = model.CorrectLetter,
                    FullWord = model.FullWord,
                    Hint = model.Hint,
                    Points = model.Points > 0 ? model.Points : 1,
                    OrderIndex = model.OrderIndex > 0 ? model.OrderIndex : test.SpellingQuestions.Count + 1
                };

                _context.SpellingQuestions.Add(question);
                await _context.SaveChangesAsync();

                _logger.LogInformation(
                    "Учитель {TeacherId} добавил вопрос к тесту орфографии {TestId}",
                    currentUser.Id, model.SpellingTestId);

                TempData["SuccessMessage"] = "Вопрос успешно добавлен!";
                return RedirectToAction(nameof(Details), new { id = model.SpellingTestId });
            }

            ViewBag.Test = test;
            return View(model);
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

                    _logger.LogInformation(
                        "Учитель {TeacherId} обновил вопрос орфографии {QuestionId}",
                        currentUser.Id, id);

                    TempData["SuccessMessage"] = "Вопрос успешно обновлен!";
                    return RedirectToAction(nameof(Details), new { id = existingQuestion.SpellingTestId });
                }
                catch (DbUpdateConcurrencyException ex)
                {
                    _logger.LogError(ex, "Ошибка обновления вопроса {QuestionId}", id);
                    ModelState.AddModelError("", "Произошла ошибка при сохранении.");
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
                await ReorderQuestions(testId);

                _logger.LogInformation(
                    "Учитель {TeacherId} удалил вопрос {QuestionId} из теста {TestId}",
                    currentUser.Id, id, testId);

                return Json(new
                {
                    success = true,
                    message = "Вопрос успешно удален",
                    testId
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Ошибка удаления вопроса {QuestionId}", id);
                return Json(new { success = false, message = $"Ошибка: {ex.Message}" });
            }
        }

        #endregion

        #region Question Import

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

            if (!ModelState.IsValid) return View(model);

            try
            {
                if (!await ValidateImportFile(model.ExcelFile, model.SpellingTestId, currentUser.Id))
                    return View(model);

                var questions = await _questionImportService.ParseExcelFileAsync(model.ExcelFile);

                if (questions == null || !questions.Any())
                {
                    TempData["ErrorMessage"] = "Файл не содержит данных для импорта";
                    return View(model);
                }

                var sessionKey = $"ImportQuestions_{model.SpellingTestId}_{DateTime.Now.Ticks}";
                var importData = new
                {
                    TestId = model.SpellingTestId,
                    Questions = questions,
                    PointsPerQuestion = model.PointsPerQuestion
                };

                HttpContext.Session.SetString(sessionKey, JsonSerializer.Serialize(importData));
                TempData["ImportSessionKey"] = sessionKey;

                _logger.LogInformation(
                    "Учитель {TeacherId} инициировал импорт {Count} вопросов для теста {TestId}",
                    currentUser.Id, questions.Count, model.SpellingTestId);

                return RedirectToAction(nameof(PreviewQuestions));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Ошибка импорта для теста {TestId}", model.SpellingTestId);
                TempData["ErrorMessage"] = $"Ошибка: {ex.Message}";
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
                    TempData["ErrorMessage"] = "Данные импорта не найдены";
                    return RedirectToAction(nameof(Index));
                }

                var importDataJson = HttpContext.Session.GetString(sessionKey);
                if (string.IsNullOrEmpty(importDataJson))
                {
                    TempData["ErrorMessage"] = "Данные импорта истекли";
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

                var questions = new List<ImportSpellingQuestionRow>();
                foreach (var questionElement in importData.GetProperty("Questions").EnumerateArray())
                {
                    var question = JsonSerializer.Deserialize<ImportSpellingQuestionRow>(questionElement.GetRawText());
                    if (question != null) questions.Add(question);
                }

                ViewBag.Test = test;
                ViewBag.PointsPerQuestion = pointsPerQuestion;
                TempData["ImportSessionKey"] = sessionKey;

                return View(questions);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Ошибка предпросмотра импорта");
                TempData["ErrorMessage"] = "Ошибка обработки данных";
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
                    .Include(st => st.SpellingQuestions)
                    .FirstOrDefaultAsync(st => st.Id == testId && st.TeacherId == currentUser.Id);

                if (test == null) return NotFound();

                var orderIndex = test.SpellingQuestions.Count;
                var importedCount = 0;

                foreach (var questionElement in importData.GetProperty("Questions").EnumerateArray())
                {
                    var questionData = JsonSerializer.Deserialize<ImportSpellingQuestionRow>(questionElement.GetRawText());
                    if (questionData?.IsValid == true)
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
                        importedCount++;
                    }
                }

                await _context.SaveChangesAsync();
                HttpContext.Session.Remove(sessionKey);

                _logger.LogInformation(
                    "Учитель {TeacherId} импортировал {Count} вопросов в тест {TestId}",
                    currentUser.Id, importedCount, testId);

                TempData["SuccessMessage"] = $"Успешно импортировано {importedCount} вопросов!";
                return RedirectToAction(nameof(Details), new { id = testId });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Ошибка подтверждения импорта");
                TempData["ErrorMessage"] = "Ошибка при импорте";
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
                    "Шаблон_вопросов_орфография.xlsx");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Ошибка генерации шаблона");
                TempData["ErrorMessage"] = "Ошибка генерации шаблона";
                return RedirectToAction(nameof(Index));
            }
        }

        #endregion

        #region Helper Methods

        private async Task LoadClasses()
        {
            var currentUser = await _userManager.GetUserAsync(User);
            var classes = await _context.Classes
                .Where(c => c.TeacherId == currentUser.Id && c.IsActive)
                .OrderBy(c => c.Name)
                .ToListAsync();
            ViewBag.Classes = new SelectList(classes, "Id", "Name");
        }

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

        private async Task<bool> ValidateImportFile(IFormFile file, int testId, string teacherId)
        {
            if (file == null || file.Length == 0)
            {
                ModelState.AddModelError("ExcelFile", "Выберите файл для импорта");
                return false;
            }

            if (file.Length > 10 * 1024 * 1024)
            {
                _logger.LogWarning("Файл слишком большой: {Size} байт", file.Length);
                ModelState.AddModelError("ExcelFile", "Размер файла не должен превышать 10 МБ");
                return false;
            }

            var allowedExtensions = new[] { ".xlsx", ".xls" };
            var fileExtension = Path.GetExtension(file.FileName).ToLowerInvariant();
            if (!allowedExtensions.Contains(fileExtension))
            {
                _logger.LogWarning("Недопустимый формат файла: {Extension}", fileExtension);
                ModelState.AddModelError("ExcelFile", "Поддерживаются только файлы Excel (.xlsx, .xls)");
                return false;
            }

            return true;
        }

        #endregion
    }
}
