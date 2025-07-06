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
    public class PunctuationTestController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly IPunctuationQuestionImportService _questionImportService;

        public PunctuationTestController(
            ApplicationDbContext context,
            UserManager<ApplicationUser> userManager,
            IPunctuationQuestionImportService questionImportService)
        {
            _context = context;
            _userManager = userManager;
            _questionImportService = questionImportService;
        }

        // GET: PunctuationTest
        public async Task<IActionResult> Index()
        {
            var currentUser = await _userManager.GetUserAsync(User);
            var tests = await _context.PunctuationTests
                .Where(pt => pt.TeacherId == currentUser.Id)
                .Include(pt => pt.Class)
                .Include(pt => pt.Questions)
                .OrderByDescending(pt => pt.CreatedAt)
                .ToListAsync();

            return View(tests);
        }

        // GET: PunctuationTest/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null) return NotFound();

            var currentUser = await _userManager.GetUserAsync(User);
            var test = await _context.PunctuationTests
                .Include(pt => pt.Teacher)
                .Include(pt => pt.Class)
                .Include(pt => pt.Questions.OrderBy(q => q.OrderIndex))
                .Include(pt => pt.TestResults)
                    .ThenInclude(tr => tr.Student)
                        .ThenInclude(s => s.User)
                .Include(pt => pt.TestResults)
                    .ThenInclude(tr => tr.Answers)
                .FirstOrDefaultAsync(pt => pt.Id == id && pt.TeacherId == currentUser.Id);

            if (test == null) return NotFound();

            return View(test);
        }

        // GET: PunctuationTest/Create
        public async Task<IActionResult> Create()
        {
            await LoadClasses();
            return View();
        }

        // POST: PunctuationTest/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(CreatePunctuationTestViewModel model)
        {
            if (ModelState.IsValid)
            {
                var currentUser = await _userManager.GetUserAsync(User);

                var test = new PunctuationTest
                {
                    Title = model.Title,
                    Description = model.Description,
                    TeacherId = currentUser.Id,
                    TestCategoryId = 5, // id=5 для тестов пунктуации
                    ClassId = model.ClassId,
                    TimeLimit = model.TimeLimit,
                    MaxAttempts = model.MaxAttempts,
                    StartDate = model.StartDate,
                    EndDate = model.EndDate,
                    ShowHints = model.ShowHints,
                    ShowCorrectAnswers = model.ShowCorrectAnswers,
                    IsActive = model.IsActive
                };

                _context.PunctuationTests.Add(test);
                await _context.SaveChangesAsync();

                TempData["SuccessMessage"] = $"Тест \"{test.Title}\" успешно создан! Теперь добавьте вопросы.";
                return RedirectToAction(nameof(Details), new { id = test.Id });
            }

            await LoadClasses();
            return View(model);
        }

        // GET: PunctuationTest/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null) return NotFound();

            var currentUser = await _userManager.GetUserAsync(User);
            var test = await _context.PunctuationTests
                .FirstOrDefaultAsync(pt => pt.Id == id && pt.TeacherId == currentUser.Id);

            if (test == null) return NotFound();

            var model = new CreatePunctuationTestViewModel
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

        // POST: PunctuationTest/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, CreatePunctuationTestViewModel model)
        {
            if (ModelState.IsValid)
            {
                try
                {
                    var currentUser = await _userManager.GetUserAsync(User);
                    var test = await _context.PunctuationTests
                        .FirstOrDefaultAsync(pt => pt.Id == id && pt.TeacherId == currentUser.Id);

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

        // GET: PunctuationTest/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null) return NotFound();

            var currentUser = await _userManager.GetUserAsync(User);
            var test = await _context.PunctuationTests
                .Include(pt => pt.Class)
                .Include(pt => pt.Questions)
                .Include(pt => pt.TestResults)
                    .ThenInclude(tr => tr.Student)
                        .ThenInclude(s => s.User)
                .Include(pt => pt.TestResults)
                    .ThenInclude(tr => tr.Answers)
                .FirstOrDefaultAsync(pt => pt.Id == id && pt.TeacherId == currentUser.Id);

            if (test == null) return NotFound();

            return View(test);
        }

        // POST: PunctuationTest/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var currentUser = await _userManager.GetUserAsync(User);
            var test = await _context.PunctuationTests
                .Include(pt => pt.TestResults)
                .FirstOrDefaultAsync(pt => pt.Id == id && pt.TeacherId == currentUser.Id);

            if (test == null) return NotFound();

            if (test.TestResults.Any())
            {
                TempData["ErrorMessage"] = "Нельзя удалить тест, который уже проходили ученики.";
                return RedirectToAction(nameof(Delete), new { id });
            }

            _context.PunctuationTests.Remove(test);
            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = $"Тест \"{test.Title}\" успешно удален!";
            return RedirectToAction(nameof(Index));
        }

        // GET: PunctuationTest/ImportQuestions/5
        public async Task<IActionResult> ImportQuestions(int id)
        {
            var currentUser = await _userManager.GetUserAsync(User);
            var test = await _context.PunctuationTests
                .FirstOrDefaultAsync(pt => pt.Id == id && pt.TeacherId == currentUser.Id);

            if (test == null) return NotFound();

            ViewBag.Test = test;
            return View(new PunctuationQuestionImportViewModel { PunctuationTestId = id });
        }

        // POST: PunctuationTest/ImportQuestions
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ImportQuestions(PunctuationQuestionImportViewModel model)
        {
            var currentUser = await _userManager.GetUserAsync(User);
            var test = await _context.PunctuationTests
                .FirstOrDefaultAsync(pt => pt.Id == model.PunctuationTestId && pt.TeacherId == currentUser.Id);

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
                if (model.ExcelFile == null || model.ExcelFile.Length == 0)
                {
                    ModelState.AddModelError("ExcelFile", "Выберите файл для импорта");
                    return View(model);
                }

                if (model.ExcelFile.Length > 10 * 1024 * 1024)
                {
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
                    TempData["ErrorMessage"] = "Файл не содержит данных для импорта";
                    return View(model);
                }

                var importData = new
                {
                    TestId = model.PunctuationTestId,
                    Questions = questions,
                    PointsPerQuestion = model.PointsPerQuestion
                };

                var sessionKey = $"ImportPunctuationQuestions_{model.PunctuationTestId}_{DateTime.Now.Ticks}";
                HttpContext.Session.SetString(sessionKey, JsonSerializer.Serialize(importData));
                TempData["ImportSessionKey"] = sessionKey;

                return RedirectToAction(nameof(PreviewQuestions));
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = $"Произошла ошибка: {ex.Message}";
                return View(model);
            }
        }

        // GET: PunctuationTest/PreviewQuestions
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
                var test = await _context.PunctuationTests
                    .FirstOrDefaultAsync(pt => pt.Id == testId && pt.TeacherId == currentUser.Id);

                if (test == null)
                {
                    TempData["ErrorMessage"] = "Тест не найден";
                    return RedirectToAction(nameof(Index));
                }

                var questionsArray = importData.GetProperty("Questions");
                var questions = new List<ImportPunctuationQuestionRow>();

                foreach (var questionElement in questionsArray.EnumerateArray())
                {
                    try
                    {
                        var question = JsonSerializer.Deserialize<ImportPunctuationQuestionRow>(questionElement.GetRawText());
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
                TempData["ErrorMessage"] = $"Ошибка обработки данных: {ex.Message}";
                return RedirectToAction(nameof(Index));
            }
        }

        // POST: PunctuationTest/ConfirmImport
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
                var test = await _context.PunctuationTests
                    .Include(pt => pt.Questions)
                    .FirstOrDefaultAsync(pt => pt.Id == testId && pt.TeacherId == currentUser.Id);

                if (test == null) return NotFound();

                var questionsArray = importData.GetProperty("Questions");
                var validQuestions = new List<ImportPunctuationQuestionRow>();
                var orderIndex = test.Questions.Count;

                foreach (var questionElement in questionsArray.EnumerateArray())
                {
                    var questionData = JsonSerializer.Deserialize<ImportPunctuationQuestionRow>(questionElement.GetRawText());
                    if (questionData.IsValid)
                    {
                        validQuestions.Add(questionData);
                    }
                }

                // Создаем вопросы в базе данных
                foreach (var questionData in validQuestions)
                {
                    var question = new PunctuationQuestion
                    {
                        PunctuationTestId = testId,
                        SentenceWithNumbers = questionData.SentenceWithNumbers,
                        CorrectPositions = questionData.CorrectPositions,
                        PlainSentence = questionData.PlainSentence,
                        Hint = questionData.Hint,
                        Points = pointsPerQuestion,
                        OrderIndex = ++orderIndex
                    };

                    _context.PunctuationQuestions.Add(question);
                }

                await _context.SaveChangesAsync();
                HttpContext.Session.Remove(sessionKey);

                TempData["SuccessMessage"] = $"Успешно импортировано {validQuestions.Count} вопросов!";
                return RedirectToAction(nameof(Details), new { id = testId });
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = $"Ошибка при импорте: {ex.Message}";
                return RedirectToAction(nameof(Index));
            }
        }

        // GET: PunctuationTest/DownloadQuestionTemplate
        public async Task<IActionResult> DownloadQuestionTemplate()
        {
            try
            {
                var templateBytes = await _questionImportService.GenerateTemplateAsync();
                return File(templateBytes,
                    "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                    "Шаблон_вопросов_пунктуация.xlsx");
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = $"Ошибка генерации шаблона: {ex.Message}";
                return RedirectToAction(nameof(Index));
            }
        }

        // GET: PunctuationTest/AddQuestion/5
        public async Task<IActionResult> AddQuestion(int id)
        {
            var currentUser = await _userManager.GetUserAsync(User);
            var test = await _context.PunctuationTests
                .Include(pt => pt.Questions)
                .FirstOrDefaultAsync(pt => pt.Id == id && pt.TeacherId == currentUser.Id);

            if (test == null) return NotFound();

            var model = new PunctuationQuestion
            {
                PunctuationTestId = test.Id,
                OrderIndex = test.Questions.Count + 1,
                Points = 1
            };

            ViewBag.Test = test;
            ViewBag.NextOrderIndex = test.Questions.Count + 1;

            return View(model);
        }

        // POST: PunctuationTest/AddQuestion
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AddQuestion(PunctuationQuestion model)
        {
            var currentUser = await _userManager.GetUserAsync(User);
            var test = await _context.PunctuationTests
                .Include(pt => pt.Questions)
                .FirstOrDefaultAsync(pt => pt.Id == model.PunctuationTestId && pt.TeacherId == currentUser.Id);

            if (test == null) return NotFound();

            ModelState.Remove("PunctuationTest");

            if (ModelState.IsValid)
            {
                var question = new PunctuationQuestion
                {
                    PunctuationTestId = model.PunctuationTestId,
                    SentenceWithNumbers = model.SentenceWithNumbers,
                    CorrectPositions = model.CorrectPositions,
                    PlainSentence = model.PlainSentence,
                    Hint = model.Hint,
                    Points = model.Points > 0 ? model.Points : 1,
                    OrderIndex = model.OrderIndex > 0 ? model.OrderIndex : test.Questions.Count + 1
                };

                _context.PunctuationQuestions.Add(question);
                await _context.SaveChangesAsync();

                TempData["SuccessMessage"] = "Вопрос успешно добавлен!";
                return RedirectToAction(nameof(Details), new { id = model.PunctuationTestId });
            }

            ViewBag.Test = test;
            ViewBag.NextOrderIndex = test.Questions.Count + 1;
            return View(model);
        }

        // POST: PunctuationTest/ToggleStatus/5
        [HttpPost]
        public async Task<IActionResult> ToggleStatus(int id)
        {
            var currentUser = await _userManager.GetUserAsync(User);
            var test = await _context.PunctuationTests
                .FirstOrDefaultAsync(pt => pt.Id == id && pt.TeacherId == currentUser.Id);

            if (test == null) return NotFound();

            test.IsActive = !test.IsActive;
            await _context.SaveChangesAsync();

            var status = test.IsActive ? "активирован" : "деактивирован";
            TempData["InfoMessage"] = $"Тест \"{test.Title}\" {status}.";

            return RedirectToAction(nameof(Details), new { id });
        }

        // GET: PunctuationTest/EditQuestion/5
        public async Task<IActionResult> EditQuestion(int? id)
        {
            if (id == null) return NotFound();

            var currentUser = await _userManager.GetUserAsync(User);
            var question = await _context.PunctuationQuestions
                .Include(pq => pq.PunctuationTest)
                .FirstOrDefaultAsync(pq => pq.Id == id && pq.PunctuationTest.TeacherId == currentUser.Id);

            if (question == null) return NotFound();

            ViewBag.Test = question.PunctuationTest;
            return View(question);
        }

        // POST: PunctuationTest/EditQuestion/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditQuestion(int id, PunctuationQuestion model)
        {
            if (id != model.Id) return NotFound();

            var currentUser = await _userManager.GetUserAsync(User);
            var existingQuestion = await _context.PunctuationQuestions
                .Include(pq => pq.PunctuationTest)
                .FirstOrDefaultAsync(pq => pq.Id == id && pq.PunctuationTest.TeacherId == currentUser.Id);

            if (existingQuestion == null) return NotFound();

            ModelState.Remove("PunctuationTest");

            if (ModelState.IsValid)
            {
                try
                {
                    existingQuestion.SentenceWithNumbers = model.SentenceWithNumbers;
                    existingQuestion.CorrectPositions = model.CorrectPositions;
                    existingQuestion.PlainSentence = model.PlainSentence;
                    existingQuestion.Hint = model.Hint;
                    existingQuestion.Points = model.Points > 0 ? model.Points : 1;
                    existingQuestion.OrderIndex = model.OrderIndex > 0 ? model.OrderIndex : existingQuestion.OrderIndex;

                    _context.Update(existingQuestion);
                    await _context.SaveChangesAsync();

                    TempData["SuccessMessage"] = "Вопрос успешно обновлен!";
                    return RedirectToAction(nameof(Details), new { id = existingQuestion.PunctuationTestId });
                }
                catch (DbUpdateConcurrencyException)
                {
                    ModelState.AddModelError("", "Произошла ошибка при сохранении. Попробуйте еще раз.");
                }
            }

            ViewBag.Test = existingQuestion.PunctuationTest;
            return View(model);
        }

        // POST: PunctuationTest/DeleteQuestion/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteQuestion(int id)
        {
            var currentUser = await _userManager.GetUserAsync(User);
            var question = await _context.PunctuationQuestions
                .Include(pq => pq.PunctuationTest)
                .Include(pq => pq.StudentAnswers)
                .FirstOrDefaultAsync(pq => pq.Id == id && pq.PunctuationTest.TeacherId == currentUser.Id);

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
                var testId = question.PunctuationTestId;
                _context.PunctuationQuestions.Remove(question);
                await _context.SaveChangesAsync();

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
            var questions = await _context.PunctuationQuestions
                .Where(q => q.PunctuationTestId == testId)
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
