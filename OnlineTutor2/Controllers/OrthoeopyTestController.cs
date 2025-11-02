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
    public class OrthoeopyTestController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly IOrthoeopyQuestionImportService _questionImportService;
        private readonly ILogger<OrthoeopyTestController> _logger;

        public OrthoeopyTestController(
            ApplicationDbContext context,
            UserManager<ApplicationUser> userManager,
            IOrthoeopyQuestionImportService questionImportService,
            ILogger<OrthoeopyTestController> logger)
        {
            _context = context;
            _userManager = userManager;
            _questionImportService = questionImportService;
            _logger = logger;
        }

        // GET: OrthoeopyTest
        public async Task<IActionResult> Index()
        {
            var currentUser = await _userManager.GetUserAsync(User);
            var tests = await _context.OrthoeopyTests
                .Where(ot => ot.TeacherId == currentUser.Id)
                .Include(ot => ot.Class)
                .Include(ot => ot.Questions)
                .Include(ot => ot.TestResults)
                .OrderByDescending(ot => ot.CreatedAt)
                .ToListAsync();

            return View(tests);
        }

        // GET: OrthoeopyTest/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null) return NotFound();

            var currentUser = await _userManager.GetUserAsync(User);
            var test = await _context.OrthoeopyTests
                .Include(ot => ot.Teacher)
                .Include(ot => ot.Class)
                .Include(ot => ot.Questions.OrderBy(q => q.OrderIndex))
                .Include(ot => ot.TestResults)
                    .ThenInclude(tr => tr.Student)
                        .ThenInclude(s => s.User)
                .Include(ot => ot.TestResults)
                    .ThenInclude(tr => tr.Answers)
                .FirstOrDefaultAsync(ot => ot.Id == id && ot.TeacherId == currentUser.Id);

            if (test == null) return NotFound();

            return View(test);
        }

        // GET: OrthoeopyTest/Create
        public async Task<IActionResult> Create()
        {
            await LoadClasses();
            return View();
        }

        // POST: OrthoeopyTest/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(CreateOrthoeopyTestViewModel model)
        {
            var currentUser = await _userManager.GetUserAsync(User);

            if (ModelState.IsValid)
            {
                var test = new OrthoeopyTest
                {
                    Title = model.Title,
                    Description = model.Description,
                    TeacherId = currentUser.Id,
                    TestCategoryId = 6,
                    ClassId = model.ClassId,
                    TimeLimit = model.TimeLimit,
                    MaxAttempts = model.MaxAttempts,
                    StartDate = model.StartDate,
                    EndDate = model.EndDate,
                    ShowHints = model.ShowHints,
                    ShowCorrectAnswers = model.ShowCorrectAnswers,
                    IsActive = model.IsActive
                };

                _context.OrthoeopyTests.Add(test);
                await _context.SaveChangesAsync();

                _logger.LogInformation("Учитель {TeacherId} создал тест орфоэпии {TestId}: {Title}, ClassId: {ClassId}, TimeLimit: {TimeLimit}",
                    currentUser.Id, test.Id, test.Title, test.ClassId, test.TimeLimit);

                TempData["SuccessMessage"] = $"Тест \"{test.Title}\" успешно создан! Теперь добавьте вопросы.";
                return RedirectToAction(nameof(Details), new { id = test.Id });
            }

            _logger.LogWarning("Учитель {TeacherId} отправил невалидную форму создания теста орфоэпии", currentUser.Id);

            await LoadClasses();
            return View(model);
        }

        // GET: OrthoeopyTest/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null) return NotFound();

            var currentUser = await _userManager.GetUserAsync(User);
            var test = await _context.OrthoeopyTests
                .FirstOrDefaultAsync(ot => ot.Id == id && ot.TeacherId == currentUser.Id);

            if (test == null) return NotFound();

            var model = new CreateOrthoeopyTestViewModel
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

        // POST: OrthoeopyTest/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, CreateOrthoeopyTestViewModel model)
        {
            var currentUser = await _userManager.GetUserAsync(User);

            if (ModelState.IsValid)
            {
                try
                {
                    var test = await _context.OrthoeopyTests
                        .FirstOrDefaultAsync(ot => ot.Id == id && ot.TeacherId == currentUser.Id);

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

                    _logger.LogInformation("Учитель {TeacherId} обновил тест орфоэпии {TestId}: {Title}, ClassId: {ClassId}",
                        currentUser.Id, id, test.Title, test.ClassId);

                    TempData["SuccessMessage"] = $"Тест \"{test.Title}\" успешно обновлен!";
                    return RedirectToAction(nameof(Details), new { id });
                }
                catch (DbUpdateConcurrencyException ex)
                {
                    _logger.LogError(ex, "Ошибка конкурентности при обновлении теста орфоэпии {TestId} учителем {TeacherId}", id, currentUser.Id);
                    ModelState.AddModelError("", "Произошла ошибка при сохранении. Попробуйте еще раз.");
                }
            }

            await LoadClasses();
            ViewBag.TestId = id;
            return View(model);
        }

        // GET: OrthoeopyTest/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null) return NotFound();

            var currentUser = await _userManager.GetUserAsync(User);
            var test = await _context.OrthoeopyTests
                .Include(ot => ot.Class)
                .Include(ot => ot.Questions)
                .Include(ot => ot.TestResults)
                    .ThenInclude(tr => tr.Student)
                        .ThenInclude(s => s.User)
                .Include(ot => ot.TestResults)
                    .ThenInclude(tr => tr.Answers)
                .FirstOrDefaultAsync(ot => ot.Id == id && ot.TeacherId == currentUser.Id);

            if (test == null) return NotFound();

            return View(test);
        }

        // POST: OrthoeopyTest/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var currentUser = await _userManager.GetUserAsync(User);

            var test = await _context.OrthoeopyTests
                .Include(ot => ot.TestResults)
                .FirstOrDefaultAsync(ot => ot.Id == id && ot.TeacherId == currentUser.Id);

            if (test == null) return NotFound();

            if (test.TestResults.Any())
            {
                _logger.LogWarning("Учитель {TeacherId} попытался удалить тест орфоэпии {TestId} с результатами ({ResultsCount})",
                   currentUser.Id, id, test.TestResults.Count);
                TempData["ErrorMessage"] = "Нельзя удалить тест, который уже проходили ученики.";
                return RedirectToAction(nameof(Delete), new { id });
            }

            var testTitle = test.Title;
            _context.OrthoeopyTests.Remove(test);
            await _context.SaveChangesAsync();

            _logger.LogInformation("Учитель {TeacherId} удалил тест орфоэпии {TestId}: {Title}", currentUser.Id, id, testTitle);

            TempData["SuccessMessage"] = $"Тест \"{test.Title}\" успешно удален!";
            return RedirectToAction(nameof(Index));
        }

        // GET: OrthoeopyTest/AddQuestion/5
        public async Task<IActionResult> AddQuestion(int id)
        {
            var currentUser = await _userManager.GetUserAsync(User);
            var test = await _context.OrthoeopyTests
                .Include(ot => ot.Questions)
                .FirstOrDefaultAsync(ot => ot.Id == id && ot.TeacherId == currentUser.Id);

            if (test == null) return NotFound();

            var model = new OrthoeopyQuestion
            {
                OrthoeopyTestId = test.Id,
                OrderIndex = test.Questions.Count + 1,
                Points = 1,
                StressPosition = 1
            };

            ViewBag.Test = test;
            ViewBag.NextOrderIndex = test.Questions.Count + 1;

            return View(model);
        }

        // POST: OrthoeopyTest/AddQuestion
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AddQuestion(OrthoeopyQuestion model)
        {
            var currentUser = await _userManager.GetUserAsync(User);
            var test = await _context.OrthoeopyTests
                .Include(ot => ot.Questions)
                .FirstOrDefaultAsync(ot => ot.Id == model.OrthoeopyTestId && ot.TeacherId == currentUser.Id);

            if (test == null) return NotFound();

            ModelState.Remove("OrthoeopyTest");

            if (ModelState.IsValid)
            {
                var question = new OrthoeopyQuestion
                {
                    OrthoeopyTestId = model.OrthoeopyTestId,
                    Word = model.Word,
                    StressPosition = model.StressPosition,
                    WordWithStress = model.WordWithStress,
                    Hint = model.Hint,
                    WrongStressPositions = model.WrongStressPositions,
                    Points = model.Points > 0 ? model.Points : 1,
                    OrderIndex = model.OrderIndex > 0 ? model.OrderIndex : test.Questions.Count + 1
                };

                _context.OrthoeopyQuestions.Add(question);
                await _context.SaveChangesAsync();

                _logger.LogInformation("Учитель {TeacherId} добавил вопрос {QuestionId} к тесту орфоэпии {TestId}: Слово: {Word}, Позиция ударения: {StressPosition}",
                    currentUser.Id, question.Id, model.OrthoeopyTestId, model.Word, model.StressPosition);

                TempData["SuccessMessage"] = "Вопрос успешно добавлен!";
                return RedirectToAction(nameof(Details), new { id = model.OrthoeopyTestId });
            }

            _logger.LogWarning("Учитель {TeacherId} отправил невалидную форму добавления вопроса к тесту орфоэпии {TestId}", currentUser.Id, model.OrthoeopyTestId);

            ViewBag.Test = test;
            ViewBag.NextOrderIndex = test.Questions.Count + 1;
            return View(model);
        }

        // GET: OrthoeopyTest/EditQuestion/5
        public async Task<IActionResult> EditQuestion(int? id)
        {
            if (id == null) return NotFound();

            var currentUser = await _userManager.GetUserAsync(User);
            var question = await _context.OrthoeopyQuestions
                .Include(oq => oq.OrthoeopyTest)
                .FirstOrDefaultAsync(oq => oq.Id == id && oq.OrthoeopyTest.TeacherId == currentUser.Id);

            if (question == null) return NotFound();

            ViewBag.Test = question.OrthoeopyTest;
            return View(question);
        }

        // POST: OrthoeopyTest/EditQuestion/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditQuestion(int id, OrthoeopyQuestion model)
        {
            if (id != model.Id) return NotFound();

            var currentUser = await _userManager.GetUserAsync(User);
            var existingQuestion = await _context.OrthoeopyQuestions
                .Include(oq => oq.OrthoeopyTest)
                .FirstOrDefaultAsync(oq => oq.Id == id && oq.OrthoeopyTest.TeacherId == currentUser.Id);

            if (existingQuestion == null) return NotFound();

            ModelState.Remove("OrthoeopyTest");

            if (ModelState.IsValid)
            {
                try
                {
                    existingQuestion.Word = model.Word;
                    existingQuestion.StressPosition = model.StressPosition;
                    existingQuestion.WordWithStress = model.WordWithStress;
                    existingQuestion.Hint = model.Hint;
                    existingQuestion.WrongStressPositions = model.WrongStressPositions;
                    existingQuestion.Points = model.Points > 0 ? model.Points : 1;
                    existingQuestion.OrderIndex = model.OrderIndex > 0 ? model.OrderIndex : existingQuestion.OrderIndex;

                    _context.Update(existingQuestion);
                    await _context.SaveChangesAsync();

                    _logger.LogInformation("Учитель {TeacherId} обновил вопрос орфоэпии {QuestionId}: Слово: {Word}, Позиция ударения: {StressPosition}",
                        currentUser.Id, id, model.Word, model.StressPosition);

                    TempData["SuccessMessage"] = "Вопрос успешно обновлен!";
                    return RedirectToAction(nameof(Details), new { id = existingQuestion.OrthoeopyTestId });
                }
                catch (DbUpdateConcurrencyException ex)
                {
                    _logger.LogError(ex, "Ошибка конкурентности при обновлении вопроса орфоэпии {QuestionId} учителем {TeacherId}", id, currentUser.Id);
                    ModelState.AddModelError("", "Произошла ошибка при сохранении. Попробуйте еще раз.");
                }
            }
            else
            {
                _logger.LogWarning("Учитель {TeacherId} отправил невалидную форму обновления вопроса орфоэпии {QuestionId}", currentUser.Id, id);
            }

                ViewBag.Test = existingQuestion.OrthoeopyTest;
            return View(model);
        }

        // POST: OrthoeopyTest/DeleteQuestion/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteQuestion(int id)
        {
            var currentUser = await _userManager.GetUserAsync(User);
            var question = await _context.OrthoeopyQuestions
                .Include(oq => oq.OrthoeopyTest)
                .Include(oq => oq.StudentAnswers)
                .FirstOrDefaultAsync(oq => oq.Id == id && oq.OrthoeopyTest.TeacherId == currentUser.Id);

            if (question == null)
                return Json(new { success = false, message = "Вопрос не найден" });

            if (question.StudentAnswers.Any())
            {
                _logger.LogWarning("Учитель {TeacherId} попытался удалить вопрос орфоэпии {QuestionId} с ответами студентов ({AnswersCount})",
                    currentUser.Id, id, question.StudentAnswers.Count);

                return Json(new
                {
                    success = false,
                    message = "Нельзя удалить вопрос, на который уже отвечали ученики"
                });
            }

            try
            {
                var testId = question.OrthoeopyTestId;
                var word = question.Word;
                _context.OrthoeopyQuestions.Remove(question);
                await _context.SaveChangesAsync();

                await ReorderQuestions(testId);

                _logger.LogInformation("Учитель {TeacherId} удалил вопрос орфоэпии {QuestionId}: {Word} из теста {TestId}",
                    currentUser.Id, id, word, testId);

                return Json(new
                {
                    success = true,
                    message = "Вопрос успешно удален",
                    testId = testId
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Ошибка удаления вопроса орфоэпии {QuestionId} учителем {TeacherId}", id, currentUser.Id);
                return Json(new
                {
                    success = false,
                    message = "Ошибка при удалении вопроса: " + ex.Message
                });
            }
        }

        // POST: OrthoeopyTest/ToggleStatus/5
        [HttpPost]
        public async Task<IActionResult> ToggleStatus(int id)
        {
            var currentUser = await _userManager.GetUserAsync(User);
            var test = await _context.OrthoeopyTests
                .FirstOrDefaultAsync(ot => ot.Id == id && ot.TeacherId == currentUser.Id);

            if (test == null) return NotFound();

            var oldStatus = test.IsActive;
            test.IsActive = !test.IsActive;
            await _context.SaveChangesAsync();

            _logger.LogInformation("Учитель {TeacherId} изменил статус теста орфоэпии {TestId}: {Title} с {OldStatus} на {NewStatus}",
                currentUser.Id, id, test.Title, oldStatus, test.IsActive);

            var status = test.IsActive ? "активирован" : "деактивирован";
            TempData["InfoMessage"] = $"Тест \"{test.Title}\" {status}.";

            return RedirectToAction(nameof(Details), new { id });
        }

        // GET: OrthoeopyTest/ImportQuestions/5
        public async Task<IActionResult> ImportQuestions(int id)
        {
            var currentUser = await _userManager.GetUserAsync(User);
            var test = await _context.OrthoeopyTests
                .FirstOrDefaultAsync(ot => ot.Id == id && ot.TeacherId == currentUser.Id);

            if (test == null) return NotFound();

            ViewBag.Test = test;
            return View(new OrthoeopyQuestionImportViewModel { OrthoeopyTestId = id });
        }

        // POST: OrthoeopyTest/ImportQuestions
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ImportQuestions(OrthoeopyQuestionImportViewModel model)
        {
            var currentUser = await _userManager.GetUserAsync(User);
            var test = await _context.OrthoeopyTests
                .FirstOrDefaultAsync(ot => ot.Id == model.OrthoeopyTestId && ot.TeacherId == currentUser.Id);

            if (test == null)
            {
                _logger.LogWarning("Учитель {TeacherId} попытался импортировать вопросы к несуществующему или чужому тесту орфоэпии {TestId}", currentUser.Id, model.OrthoeopyTestId);
                TempData["ErrorMessage"] = "Тест не найден";
                return RedirectToAction(nameof(Index));
            }

            ViewBag.Test = test;

            if (!ModelState.IsValid)
            {
                _logger.LogWarning("Учитель {TeacherId} отправил невалидную форму импорта вопросов для теста орфоэпии {TestId}", currentUser.Id, model.OrthoeopyTestId);
                return View(model);
            }

            try
            {
                if (model.ExcelFile == null || model.ExcelFile.Length == 0)
                {
                    ModelState.AddModelError("ExcelFile", "Выберите файл для импорта");
                    return View(model);
                }

                if (model.ExcelFile.Length > 10 * 1024 * 1024)
                {
                    _logger.LogWarning("Учитель {TeacherId} попытался импортировать слишком большой файл ({FileSize} байт) для теста орфоэпии {TestId}",
                        currentUser.Id, model.ExcelFile.Length, model.OrthoeopyTestId);
                    ModelState.AddModelError("ExcelFile", "Размер файла не должен превышать 10 МБ");
                    return View(model);
                }

                var allowedExtensions = new[] { ".xlsx", ".xls" };
                var fileExtension = Path.GetExtension(model.ExcelFile.FileName).ToLowerInvariant();
                if (!allowedExtensions.Contains(fileExtension))
                {
                    ModelState.AddModelError("ExcelFile", "Поддерживаются только файлы Excel (.xlsx, .xls)");
                    return View(model);
                }

                var questions = await _questionImportService.ParseExcelFileAsync(model.ExcelFile);

                if (questions == null || !questions.Any())
                {
                    _logger.LogWarning("Учитель {TeacherId} импортировал пустой файл для теста орфоэпии {TestId}", currentUser.Id, model.OrthoeopyTestId);
                    TempData["ErrorMessage"] = "Файл не содержит данных для импорта";
                    return View(model);
                }

                var importData = new
                {
                    TestId = model.OrthoeopyTestId,
                    Questions = questions,
                    PointsPerQuestion = model.PointsPerQuestion
                };

                var sessionKey = $"ImportOrthoeopyQuestions_{model.OrthoeopyTestId}_{DateTime.Now.Ticks}";
                HttpContext.Session.SetString(sessionKey, JsonSerializer.Serialize(importData));
                TempData["ImportSessionKey"] = sessionKey;

                _logger.LogInformation("Учитель {TeacherId} инициировал импорт {QuestionsCount} вопросов для теста орфоэпии {TestId}",
                    currentUser.Id, questions.Count, model.OrthoeopyTestId);

                return RedirectToAction(nameof(PreviewQuestions));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Ошибка импорта вопросов для теста орфоэпии {TestId} учителем {TeacherId}", model.OrthoeopyTestId, currentUser.Id);
                TempData["ErrorMessage"] = $"Произошла ошибка: {ex.Message}";
                return View(model);
            }
        }

        // GET: OrthoeopyTest/PreviewQuestions
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
                var test = await _context.OrthoeopyTests
                    .FirstOrDefaultAsync(ot => ot.Id == testId && ot.TeacherId == currentUser.Id);

                if (test == null)
                {
                    TempData["ErrorMessage"] = "Тест не найден";
                    return RedirectToAction(nameof(Index));
                }

                var questionsArray = importData.GetProperty("Questions");
                var questions = new List<ImportOrthoeopyQuestionRow>();

                foreach (var questionElement in questionsArray.EnumerateArray())
                {
                    try
                    {
                        var question = JsonSerializer.Deserialize<ImportOrthoeopyQuestionRow>(questionElement.GetRawText());
                        if (question != null)
                        {
                            questions.Add(question);
                        }
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"Error deserializing question: {ex.Message}");
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

        // POST: OrthoeopyTest/ConfirmImport
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
                var test = await _context.OrthoeopyTests
                    .Include(ot => ot.Questions)
                    .FirstOrDefaultAsync(ot => ot.Id == testId && ot.TeacherId == currentUser.Id);

                if (test == null) return NotFound();

                var questionsArray = importData.GetProperty("Questions");
                var validQuestions = new List<ImportOrthoeopyQuestionRow>();
                var orderIndex = test.Questions.Count;

                foreach (var questionElement in questionsArray.EnumerateArray())
                {
                    var questionData = JsonSerializer.Deserialize<ImportOrthoeopyQuestionRow>(questionElement.GetRawText());
                    if (questionData != null && questionData.IsValid)
                    {
                        validQuestions.Add(questionData);
                    }
                }

                foreach (var questionData in validQuestions)
                {
                    var question = new OrthoeopyQuestion
                    {
                        OrthoeopyTestId = testId,
                        Word = questionData.Word,
                        StressPosition = questionData.StressPosition,
                        WordWithStress = questionData.WordWithStress,
                        Hint = questionData.Hint,
                        WrongStressPositions = questionData.WrongStressPositions,
                        Points = pointsPerQuestion,
                        OrderIndex = ++orderIndex
                    };

                    _context.OrthoeopyQuestions.Add(question);
                }

                await _context.SaveChangesAsync();
                HttpContext.Session.Remove(sessionKey);

                _logger.LogInformation("Учитель {TeacherId} подтвердил импорт {QuestionsCount} вопросов для теста орфоэпии {TestId}",
                    currentUser.Id, validQuestions.Count, testId);

                TempData["SuccessMessage"] = $"Успешно импортировано {validQuestions.Count} вопросов!";
                return RedirectToAction(nameof(Details), new { id = testId });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Ошибка подтверждения импорта вопросов орфоэпии");
                TempData["ErrorMessage"] = $"Ошибка при импорте: {ex.Message}";
                return RedirectToAction(nameof(Index));
            }
        }

        // GET: OrthoeopyTest/DownloadQuestionTemplate
        public async Task<IActionResult> DownloadQuestionTemplate()
        {
            var currentUser = _userManager.GetUserAsync(User);

            try
            {
                var templateBytes = await _questionImportService.GenerateTemplateAsync();
                return File(templateBytes,
                    "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                    "Шаблон_вопросов_орфоэпия.xlsx");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Ошибка генерации шаблона вопросов орфоэпии для учителя {TeacherId}", currentUser.Id);
                TempData["ErrorMessage"] = $"Ошибка генерации шаблона: {ex.Message}";
                return RedirectToAction(nameof(Index));
            }
        }

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
            var questions = await _context.OrthoeopyQuestions
                .Where(q => q.OrthoeopyTestId == testId)
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
