using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using OnlineTutor2.Data;
using OnlineTutor2.Models;
using OnlineTutor2.ViewModels;

namespace OnlineTutor2.Controllers
{
    [Authorize(Roles = ApplicationRoles.Teacher)]
    public class RegularTestController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly ILogger<RegularTestController> _logger;

        public RegularTestController(
            ApplicationDbContext context,
            UserManager<ApplicationUser> userManager,
            ILogger<RegularTestController> logger)
        {
            _context = context;
            _userManager = userManager;
            _logger = logger;
        }

        // GET: RegularTest
        public async Task<IActionResult> Index()
        {
            var currentUser = await _userManager.GetUserAsync(User);
            var tests = await _context.RegularTests
                .Where(rt => rt.TeacherId == currentUser.Id)
                .Include(st => st.TestClasses)
                .Include(rt => rt.RegularQuestions)
                .Include(rt => rt.RegularTestResults)
                .OrderByDescending(rt => rt.CreatedAt)
                .ToListAsync();

            return View(tests);
        }

        // GET: RegularTest/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null) return NotFound();

            var currentUser = await _userManager.GetUserAsync(User);
            var test = await _context.RegularTests
                .Include(rt => rt.Teacher)
                .Include(st => st.TestClasses)
                    .ThenInclude(tc => tc.Class)
                .Include(rt => rt.RegularQuestions.OrderBy(q => q.OrderIndex))
                    .ThenInclude(q => q.Options.OrderBy(o => o.OrderIndex))
                .Include(rt => rt.RegularTestResults)
                    .ThenInclude(tr => tr.Student)
                        .ThenInclude(s => s.User)
                .Include(rt => rt.RegularTestResults)
                    .ThenInclude(tr => tr.RegularAnswers)
                .FirstOrDefaultAsync(rt => rt.Id == id && rt.TeacherId == currentUser.Id);

            if (test == null) return NotFound();

            return View(test);
        }

        // GET: RegularTest/Create
        public async Task<IActionResult> Create()
        {
            await LoadClasses();
            ViewBag.TestTypes = GetTestTypesSelectList();
            return View();
        }

        // POST: RegularTest/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(CreateRegularTestViewModel model)
        {
            var currentUser = await _userManager.GetUserAsync(User);

            if (ModelState.IsValid)
            {
                var test = new RegularTest
                {
                    Title = model.Title,
                    Description = model.Description,
                    TeacherId = currentUser.Id,
                    TestCategoryId = TestCategoryConstants.Regular,
                    TimeLimit = model.TimeLimit,
                    MaxAttempts = model.MaxAttempts,
                    StartDate = model.StartDate,
                    EndDate = model.EndDate,
                    ShowHints = model.ShowHints,
                    ShowCorrectAnswers = model.ShowCorrectAnswers,
                    IsActive = model.IsActive,
                    Type = model.TestType
                };

                _context.RegularTests.Add(test);
                await _context.SaveChangesAsync();

                if (model.SelectedClassIds != null && model.SelectedClassIds.Any())
                {
                    foreach (var classId in model.SelectedClassIds)
                    {
                        var testClass = new RegularTestClass
                        {
                            RegularTestId = test.Id,
                            ClassId = classId
                        };
                        _context.RegularTestClasses.Add(testClass);
                    }
                    await _context.SaveChangesAsync();
                }

                _logger.LogInformation("Учитель {TeacherId} создал классический тест {TestId}: {Title}, Тип: {TestType}",
                    currentUser.Id, test.Id, test.Title, test.Type);

                TempData["SuccessMessage"] = $"Тест \"{test.Title}\" успешно создан! Теперь добавьте вопросы.";
                return RedirectToAction(nameof(Details), new { id = test.Id });
            }

            _logger.LogWarning("Учитель {TeacherId} отправил невалидную форму создания классического теста", currentUser.Id);

            await LoadClasses();
            ViewBag.TestTypes = GetTestTypesSelectList();
            return View(model);
        }

        // GET: RegularTest/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null) return NotFound();

            var currentUser = await _userManager.GetUserAsync(User);
            var test = await _context.RegularTests
                .Include(st => st.TestClasses)
                .FirstOrDefaultAsync(rt => rt.Id == id && rt.TeacherId == currentUser.Id);

            if (test == null) return NotFound();

            var model = new CreateRegularTestViewModel
            {
                Title = test.Title,
                Description = test.Description,
                SelectedClassIds = test.TestClasses.Select(tc => tc.ClassId).ToList(),
                TimeLimit = test.TimeLimit,
                MaxAttempts = test.MaxAttempts,
                StartDate = test.StartDate,
                EndDate = test.EndDate,
                ShowHints = test.ShowHints,
                ShowCorrectAnswers = test.ShowCorrectAnswers,
                IsActive = test.IsActive,
                TestType = test.Type
            };

            await LoadClasses();
            ViewBag.TestTypes = GetTestTypesSelectList();
            ViewBag.TestId = id;
            return View(model);
        }

        // POST: RegularTest/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, CreateRegularTestViewModel model)
        {
            var currentUser = await _userManager.GetUserAsync(User);

            if (ModelState.IsValid)
            {
                try
                {
                    var test = await _context.RegularTests
                        .Include(st => st.TestClasses)
                        .FirstOrDefaultAsync(rt => rt.Id == id && rt.TeacherId == currentUser.Id);

                    if (test == null) return NotFound();

                    test.Title = model.Title;
                    test.Description = model.Description;
                    test.TimeLimit = model.TimeLimit;
                    test.MaxAttempts = model.MaxAttempts;
                    test.StartDate = model.StartDate;
                    test.EndDate = model.EndDate;
                    test.ShowHints = model.ShowHints;
                    test.ShowCorrectAnswers = model.ShowCorrectAnswers;
                    test.IsActive = model.IsActive;
                    test.Type = model.TestType;

                    _context.RegularTestClasses.RemoveRange(test.TestClasses);

                    if (model.SelectedClassIds != null && model.SelectedClassIds.Any())
                    {
                        foreach (var classId in model.SelectedClassIds)
                        {
                            test.TestClasses.Add(new RegularTestClass
                            {
                                RegularTestId = test.Id,
                                ClassId = classId
                            });
                        }
                    }

                    _context.Update(test);
                    await _context.SaveChangesAsync();

                    _logger.LogInformation("Учитель {TeacherId} обновил классический тест {TestId}: {Title}",
                        currentUser.Id, id, test.Title);

                    TempData["SuccessMessage"] = $"Тест \"{test.Title}\" успешно обновлен!";
                    return RedirectToAction(nameof(Details), new { id });
                }
                catch (DbUpdateConcurrencyException ex)
                {
                    _logger.LogError(ex, "Ошибка конкурентности при обновлении классического теста {TestId}", id);
                    ModelState.AddModelError("", "Произошла ошибка при сохранении. Попробуйте еще раз.");
                }
            }

            await LoadClasses();
            ViewBag.TestTypes = GetTestTypesSelectList();
            ViewBag.TestId = id;
            return View(model);
        }

        // GET: RegularTest/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null) return NotFound();

            var currentUser = await _userManager.GetUserAsync(User);
            var test = await _context.RegularTests
                .Include(rt => rt.TestClasses)
                    .ThenInclude(rtc => rtc.Class)
                .Include(rt => rt.RegularQuestions)
                .Include(rt => rt.RegularTestResults)
                    .ThenInclude(tr => tr.Student)
                        .ThenInclude(s => s.User)
                .FirstOrDefaultAsync(rt => rt.Id == id && rt.TeacherId == currentUser.Id);

            if (test == null) return NotFound();

            return View(test);
        }

        // POST: RegularTest/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var currentUser = await _userManager.GetUserAsync(User);
            var test = await _context.RegularTests
                .Include(rt => rt.TestClasses)
                .Include(rt => rt.RegularTestResults)
                .FirstOrDefaultAsync(rt => rt.Id == id && rt.TeacherId == currentUser.Id);

            if (test == null) return NotFound();

            if (test.RegularTestResults.Any())
            {
                _logger.LogWarning("Учитель {TeacherId} попытался удалить классический тест {TestId} с результатами ({ResultsCount})",
                    currentUser.Id, id, test.RegularTestResults.Count);
                TempData["ErrorMessage"] = "Нельзя удалить тест, который уже проходили ученики.";
                return RedirectToAction(nameof(Delete), new { id });
            }

            var testTitle = test.Title;
            _context.RegularTests.Remove(test);
            await _context.SaveChangesAsync();

            _logger.LogInformation("Учитель {TeacherId} удалил классический тест {TestId}: {Title}", currentUser.Id, id, testTitle);

            TempData["SuccessMessage"] = $"Тест \"{testTitle}\" успешно удален!";
            return RedirectToAction("Category", "Test", new { id = TestCategoryConstants.Regular });
        }

        // POST: RegularTest/ToggleStatus/5
        [HttpPost]
        public async Task<IActionResult> ToggleStatus(int id)
        {
            var currentUser = await _userManager.GetUserAsync(User);
            var test = await _context.RegularTests
                .FirstOrDefaultAsync(rt => rt.Id == id && rt.TeacherId == currentUser.Id);

            if (test == null) return NotFound();

            var oldStatus = test.IsActive;
            test.IsActive = !test.IsActive;
            await _context.SaveChangesAsync();

            _logger.LogInformation("Учитель {TeacherId} изменил статус классического теста {TestId}: {Title} с {OldStatus} на {NewStatus}",
                currentUser.Id, id, test.Title, oldStatus, test.IsActive);

            var status = test.IsActive ? "активирован" : "деактивирован";
            TempData["InfoMessage"] = $"Тест \"{test.Title}\" {status}.";

            return RedirectToAction(nameof(Details), new { id });
        }

        #region Questions Management

        // GET: RegularTest/AddQuestion/5
        public async Task<IActionResult> AddQuestion(int id)
        {
            var currentUser = await _userManager.GetUserAsync(User);
            var test = await _context.RegularTests
                .Include(rt => rt.RegularQuestions)
                .FirstOrDefaultAsync(rt => rt.Id == id && rt.TeacherId == currentUser.Id);

            if (test == null) return NotFound();

            var model = new CreateRegularQuestionViewModel
            {
                TestId = test.Id,
                OrderIndex = test.RegularQuestions.Count + 1,
                Points = 1,
                Type = QuestionType.SingleChoice,
                Options = new List<RegularQuestionOptionViewModel>()
            };

            ViewBag.Test = test;
            ViewBag.QuestionTypes = GetQuestionTypesSelectList();
            return View(model);
        }

        // POST: RegularTest/AddQuestion
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AddQuestion(CreateRegularQuestionViewModel model)
        {
            var currentUser = await _userManager.GetUserAsync(User);
            var test = await _context.RegularTests
                .Include(rt => rt.RegularQuestions)
                .FirstOrDefaultAsync(rt => rt.Id == model.TestId && rt.TeacherId == currentUser.Id);

            if (test == null) return NotFound();

            // Валидация в зависимости от типа вопроса
            ValidateQuestionModel(model, ModelState);

            if (ModelState.IsValid)
            {
                var question = new RegularQuestion
                {
                    TestId = model.TestId,
                    Text = model.Text,
                    Type = model.Type,
                    Points = model.Points,
                    Hint = model.Hint,
                    Explanation = model.Explanation,
                    OrderIndex = model.OrderIndex > 0 ? model.OrderIndex : test.RegularQuestions.Count + 1
                };

                _context.RegularQuestions.Add(question);
                await _context.SaveChangesAsync();

                // Добавляем варианты ответов
                if (model.Options != null && model.Options.Any())
                {
                    int optionIndex = 1;
                    foreach (var optionVm in model.Options.Where(o => !string.IsNullOrWhiteSpace(o.Text)))
                    {
                        var option = new RegularQuestionOption
                        {
                            QuestionId = question.Id,
                            Text = optionVm.Text.Trim(),
                            IsCorrect = optionVm.IsCorrect,
                            OrderIndex = optionIndex++,
                            Explanation = optionVm.Explanation
                        };

                        _context.RegularQuestionOptions.Add(option);
                    }

                    await _context.SaveChangesAsync();
                }

                _logger.LogInformation("Учитель {TeacherId} добавил вопрос {QuestionId} к классическому тесту {TestId}: Тип: {Type}, Вопрос: {Text}",
                    currentUser.Id, question.Id, model.TestId, model.Type, model.Text);

                TempData["SuccessMessage"] = "Вопрос успешно добавлен!";
                return RedirectToAction(nameof(Details), new { id = model.TestId });
            }

            _logger.LogWarning("Учитель {TeacherId} отправил невалидную форму добавления вопроса к классическому тесту {TestId}",
                currentUser.Id, model.TestId);

            ViewBag.Test = test;
            ViewBag.QuestionTypes = GetQuestionTypesSelectList();
            return View(model);
        }

        // GET: RegularTest/EditQuestion/5
        public async Task<IActionResult> EditQuestion(int? id)
        {
            if (id == null) return NotFound();

            var currentUser = await _userManager.GetUserAsync(User);
            var question = await _context.RegularQuestions
                .Include(rq => rq.RegularTest)
                .Include(rq => rq.Options.OrderBy(o => o.OrderIndex))
                .FirstOrDefaultAsync(rq => rq.Id == id && rq.RegularTest.TeacherId == currentUser.Id);

            if (question == null) return NotFound();

            var model = new CreateRegularQuestionViewModel
            {
                Id = question.Id,
                TestId = question.TestId,
                Text = question.Text,
                Type = question.Type,
                Points = question.Points,
                Hint = question.Hint,
                Explanation = question.Explanation,
                OrderIndex = question.OrderIndex,
                Options = question.Options.Select(o => new RegularQuestionOptionViewModel
                {
                    Id = o.Id,
                    Text = o.Text,
                    IsCorrect = o.IsCorrect,
                    OrderIndex = o.OrderIndex,
                    Explanation = o.Explanation
                }).ToList()
            };

            ViewBag.Test = question.RegularTest;
            ViewBag.QuestionTypes = GetQuestionTypesSelectList();
            return View(model);
        }

        // POST: RegularTest/EditQuestion/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditQuestion(int id, CreateRegularQuestionViewModel model)
        {
            if (id != model.Id) return NotFound();

            var currentUser = await _userManager.GetUserAsync(User);
            var existingQuestion = await _context.RegularQuestions
                .Include(rq => rq.RegularTest)
                .Include(rq => rq.Options)
                .FirstOrDefaultAsync(rq => rq.Id == id && rq.RegularTest.TeacherId == currentUser.Id);

            if (existingQuestion == null) return NotFound();

            ValidateQuestionModel(model, ModelState);

            if (ModelState.IsValid)
            {
                try
                {
                    existingQuestion.Text = model.Text;
                    existingQuestion.Type = model.Type;
                    existingQuestion.Points = model.Points;
                    existingQuestion.Hint = model.Hint;
                    existingQuestion.Explanation = model.Explanation;
                    existingQuestion.OrderIndex = model.OrderIndex;

                    // Обновляем варианты ответов
                    // Удаляем старые
                    _context.RegularQuestionOptions.RemoveRange(existingQuestion.Options);

                    // Добавляем новые
                    if (model.Options != null && model.Options.Any())
                    {
                        int optionIndex = 1;
                        foreach (var optionVm in model.Options.Where(o => !string.IsNullOrWhiteSpace(o.Text)))
                        {
                            var option = new RegularQuestionOption
                            {
                                QuestionId = existingQuestion.Id,
                                Text = optionVm.Text.Trim(),
                                IsCorrect = optionVm.IsCorrect,
                                OrderIndex = optionIndex++,
                                Explanation = optionVm.Explanation
                            };

                            _context.RegularQuestionOptions.Add(option);
                        }
                    }

                    _context.Update(existingQuestion);
                    await _context.SaveChangesAsync();

                    _logger.LogInformation("Учитель {TeacherId} обновил вопрос {QuestionId} классического теста: {Text}",
                        currentUser.Id, id, model.Text);

                    TempData["SuccessMessage"] = "Вопрос успешно обновлен!";
                    return RedirectToAction(nameof(Details), new { id = existingQuestion.TestId });
                }
                catch (DbUpdateConcurrencyException ex)
                {
                    _logger.LogError(ex, "Ошибка конкурентности при обновлении вопроса {QuestionId}", id);
                    ModelState.AddModelError("", "Произошла ошибка при сохранении. Попробуйте еще раз.");
                }
            }

            ViewBag.Test = existingQuestion.RegularTest;
            ViewBag.QuestionTypes = GetQuestionTypesSelectList();
            return View(model);
        }

        // POST: RegularTest/DeleteQuestion/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteQuestion(int id)
        {
            var currentUser = await _userManager.GetUserAsync(User);
            var question = await _context.RegularQuestions
                .Include(rq => rq.RegularTest)
                .Include(rq => rq.RegularAnswers)
                .FirstOrDefaultAsync(rq => rq.Id == id && rq.RegularTest.TeacherId == currentUser.Id);

            if (question == null)
                return Json(new { success = false, message = "Вопрос не найден" });

            if (question.RegularAnswers.Any())
            {
                _logger.LogWarning("Учитель {TeacherId} попытался удалить вопрос {QuestionId} с ответами студентов ({AnswersCount})",
                    currentUser.Id, id, question.RegularAnswers.Count);

                return Json(new
                {
                    success = false,
                    message = "Нельзя удалить вопрос, на который уже отвечали ученики"
                });
            }

            try
            {
                var testId = question.TestId;
                var questionText = question.Text;

                _context.RegularQuestions.Remove(question);
                await _context.SaveChangesAsync();

                await ReorderQuestions(testId);

                _logger.LogInformation("Учитель {TeacherId} удалил вопрос {QuestionId}: {Text} из классического теста {TestId}",
                    currentUser.Id, id, questionText, testId);

                return Json(new
                {
                    success = true,
                    message = "Вопрос успешно удален",
                    testId = testId
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Ошибка удаления вопроса {QuestionId}", id);
                return Json(new
                {
                    success = false,
                    message = "Ошибка при удалении вопроса: " + ex.Message
                });
            }
        }

        // POST: RegularTest/ReorderQuestions
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ReorderQuestions(int testId, List<int> questionIds)
        {
            var currentUser = await _userManager.GetUserAsync(User);
            var test = await _context.RegularTests
                .Include(rt => rt.RegularQuestions)
                .FirstOrDefaultAsync(rt => rt.Id == testId && rt.TeacherId == currentUser.Id);

            if (test == null)
                return Json(new { success = false, message = "Тест не найден" });

            try
            {
                for (int i = 0; i < questionIds.Count; i++)
                {
                    var question = test.RegularQuestions.FirstOrDefault(q => q.Id == questionIds[i]);
                    if (question != null)
                    {
                        question.OrderIndex = i + 1;
                    }
                }

                await _context.SaveChangesAsync();

                _logger.LogInformation("Учитель {TeacherId} изменил порядок {QuestionsCount} вопросов в классическом тесте {TestId}",
                    currentUser.Id, questionIds.Count, testId);

                return Json(new { success = true, message = "Порядок вопросов обновлен" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Ошибка изменения порядка вопросов в классическом тесте {TestId}", testId);
                return Json(new { success = false, message = "Ошибка: " + ex.Message });
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

        private SelectList GetQuestionTypesSelectList()
        {
            var types = new[]
            {
                new { Value = (int)QuestionType.SingleChoice, Text = "Одиночный выбор" },
                new { Value = (int)QuestionType.MultipleChoice, Text = "Множественный выбор" },
                new { Value = (int)QuestionType.TrueFalse, Text = "Верно/Неверно" }
            };

            return new SelectList(types, "Value", "Text");
        }

        private SelectList GetTestTypesSelectList()
        {
            var types = new[]
            {
                new { Value = (int)TestType.Practice, Text = "Практика" },
                new { Value = (int)TestType.Quiz, Text = "Викторина" },
                new { Value = (int)TestType.Exam, Text = "Экзамен" },
                new { Value = (int)TestType.Homework, Text = "Домашнее задание" }
            };

            return new SelectList(types, "Value", "Text");
        }

        private void ValidateQuestionModel(CreateRegularQuestionViewModel model, ModelStateDictionary modelState)
        {
            if (model.Type == QuestionType.TrueFalse)
            {
                // Для Верно/Неверно должно быть ровно 2 варианта
                var validOptions = model.Options?.Where(o => !string.IsNullOrWhiteSpace(o.Text)).ToList() ?? new List<RegularQuestionOptionViewModel>();

                if (validOptions.Count != 2)
                {
                    modelState.AddModelError("Options", "Для вопроса Верно/Неверно должно быть ровно 2 варианта ответа");
                }
                else
                {
                    var correctCount = validOptions.Count(o => o.IsCorrect);
                    if (correctCount != 1)
                    {
                        modelState.AddModelError("Options", "Для вопроса Верно/Неверно должен быть отмечен ровно 1 правильный ответ");
                    }
                }
            }
            else if (model.Type == QuestionType.SingleChoice)
            {
                // Для одиночного выбора должен быть хотя бы 2 варианта и ровно 1 правильный
                var validOptions = model.Options?.Where(o => !string.IsNullOrWhiteSpace(o.Text)).ToList() ?? new List<RegularQuestionOptionViewModel>();

                if (validOptions.Count < 2)
                {
                    modelState.AddModelError("Options", "Для одиночного выбора нужно минимум 2 варианта ответа");
                }
                else
                {
                    var correctCount = validOptions.Count(o => o.IsCorrect);
                    if (correctCount != 1)
                    {
                        modelState.AddModelError("Options", "Для одиночного выбора должен быть отмечен ровно 1 правильный ответ");
                    }
                }
            }
            else if (model.Type == QuestionType.MultipleChoice)
            {
                // Для множественного выбора должно быть минимум 3 варианта и минимум 2 правильных
                var validOptions = model.Options?.Where(o => !string.IsNullOrWhiteSpace(o.Text)).ToList() ?? new List<RegularQuestionOptionViewModel>();

                if (validOptions.Count < 3)
                {
                    modelState.AddModelError("Options", "Для множественного выбора нужно минимум 3 варианта ответа");
                }
                else
                {
                    var correctCount = validOptions.Count(o => o.IsCorrect);
                    if (correctCount < 2)
                    {
                        modelState.AddModelError("Options", "Для множественного выбора должно быть минимум 2 правильных ответа");
                    }
                }
            }
        }

        private async Task ReorderQuestions(int testId)
        {
            var questions = await _context.RegularQuestions
                .Where(q => q.TestId == testId)
                .OrderBy(q => q.OrderIndex)
                .ToListAsync();

            for (int i = 0; i < questions.Count; i++)
            {
                questions[i].OrderIndex = i + 1;
            }

            await _context.SaveChangesAsync();
        }

        #endregion
    }
}
