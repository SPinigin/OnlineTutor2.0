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
            if (ModelState.IsValid)
            {
                try
                {
                    var currentUser = await _userManager.GetUserAsync(User);
                    var test = await _context.SpellingTests
                        .FirstOrDefaultAsync(st => st.Id == model.SpellingTestId && st.TeacherId == currentUser.Id);

                    if (test == null) return NotFound();

                    // Проверка файла
                    if (model.ExcelFile == null || model.ExcelFile.Length == 0)
                    {
                        ModelState.AddModelError("ExcelFile", "Выберите файл для импорта");
                        ViewBag.Test = test;
                        return View(model);
                    }

                    var allowedExtensions = new[] { ".xlsx", ".xls" };
                    var fileExtension = Path.GetExtension(model.ExcelFile.FileName).ToLowerInvariant();
                    if (!allowedExtensions.Contains(fileExtension))
                    {
                        ModelState.AddModelError("ExcelFile", "Поддерживаются только файлы Excel (.xlsx, .xls)");
                        ViewBag.Test = test;
                        return View(model);
                    }

                    // Парсинг файла
                    var questions = await _questionImportService.ParseExcelFileAsync(model.ExcelFile);

                    if (!questions.Any())
                    {
                        TempData["ErrorMessage"] = "Файл не содержит данных для импорта";
                        ViewBag.Test = test;
                        return View(model);
                    }

                    // Сохраняем данные в TempData для предварительного просмотра
                    TempData["ImportQuestions"] = System.Text.Json.JsonSerializer.Serialize(new
                    {
                        TestId = model.SpellingTestId,
                        Questions = questions,
                        PointsPerQuestion = model.PointsPerQuestion
                    });

                    return RedirectToAction(nameof(PreviewQuestions));
                }
                catch (Exception ex)
                {
                    ModelState.AddModelError("", $"Ошибка при обработке файла: {ex.Message}");
                }
            }

            var currentUserForView = await _userManager.GetUserAsync(User);
            var testForView = await _context.SpellingTests
                .FirstOrDefaultAsync(st => st.Id == model.SpellingTestId && st.TeacherId == currentUserForView.Id);
            ViewBag.Test = testForView;
            return View(model);
        }

        // GET: SpellingTest/PreviewQuestions
        public async Task<IActionResult> PreviewQuestions()
        {
            var importDataJson = TempData["ImportQuestions"] as string;
            if (string.IsNullOrEmpty(importDataJson))
            {
                TempData["ErrorMessage"] = "Данные импорта не найдены. Попробуйте еще раз.";
                return RedirectToAction(nameof(Index));
            }

            try
            {
                var importData = System.Text.Json.JsonSerializer.Deserialize<JsonElement>(importDataJson);
                var testId = importData.GetProperty("TestId").GetInt32();
                var pointsPerQuestion = importData.GetProperty("PointsPerQuestion").GetInt32();

                var currentUser = await _userManager.GetUserAsync(User);
                var test = await _context.SpellingTests
                    .FirstOrDefaultAsync(st => st.Id == testId && st.TeacherId == currentUser.Id);

                if (test == null) return NotFound();

                var questionsArray = importData.GetProperty("Questions");
                var questions = new List<ImportQuestionRow>();

                foreach (var questionElement in questionsArray.EnumerateArray())
                {
                    var question = System.Text.Json.JsonSerializer.Deserialize<ImportQuestionRow>(questionElement.GetRawText());
                    questions.Add(question);
                }

                ViewBag.Test = test;
                ViewBag.PointsPerQuestion = pointsPerQuestion;
                TempData["ImportQuestions"] = importDataJson; // Сохраняем обратно

                return View(questions);
            }
            catch (Exception ex)
            {
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

            ViewBag.Test = test;
            ViewBag.NextOrderIndex = test.Questions.Count + 1;

            return View();
        }

        // POST: SpellingTest/AddQuestion
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AddQuestion(int testId, SpellingQuestion model)
        {
            var currentUser = await _userManager.GetUserAsync(User);
            var test = await _context.SpellingTests
                .Include(st => st.Questions)
                .FirstOrDefaultAsync(st => st.Id == testId && st.TeacherId == currentUser.Id);

            if (test == null) return NotFound();

            if (ModelState.IsValid)
            {
                model.SpellingTestId = testId;
                model.OrderIndex = test.Questions.Count + 1;

                _context.SpellingQuestions.Add(model);
                await _context.SaveChangesAsync();

                TempData["SuccessMessage"] = "Вопрос успешно добавлен!";
                return RedirectToAction(nameof(Details), new { id = testId });
            }

            ViewBag.Test = test;
            ViewBag.NextOrderIndex = test.Questions.Count + 1;
            return View(model);
        }
    }
}
