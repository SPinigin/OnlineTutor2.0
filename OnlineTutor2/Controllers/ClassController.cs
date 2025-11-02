using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using OnlineTutor2.Data;
using OnlineTutor2.Models;
using OnlineTutor2.ViewModels;

namespace OnlineTutor2.Controllers
{
    [Authorize(Roles = ApplicationRoles.Teacher)]
    public class ClassController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly ILogger<ClassController> _logger;


        public ClassController(ApplicationDbContext context, UserManager<ApplicationUser> userManager, ILogger<ClassController> logger)
        {
            _context = context;
            _userManager = userManager;
            _logger = logger;
        }

        // GET: Class
        public async Task<IActionResult> Index()
        {
            var currentUser = await _userManager.GetUserAsync(User);

            var classes = await _context.Classes
                .Where(c => c.TeacherId == currentUser.Id)
                .Include(c => c.Students)
                .Include(c => c.SpellingTests)
                .Include(c => c.PunctuationTests)
                .Include(c => c.RegularTests)
                .Include(c => c.OrthoeopyTests)
                .Include(c => c.Materials)
                .OrderBy(c => c.Name)
                .ToListAsync();

            // Получаем ID всех классов
            var classIds = classes.Select(c => c.Id).ToList();

            // Получаем количество тестов по орфографии для каждого класса
            var spellingTestsCounts = await _context.SpellingTests
                .Where(st => classIds.Contains(st.ClassId.Value))
                .GroupBy(st => st.ClassId)
                .Select(g => new { ClassId = g.Key, Count = g.Count() })
                .ToDictionaryAsync(x => x.ClassId.Value, x => x.Count);

            // Получаем количество тестов по пунктуации для каждого класса
            var punctuationTestsCounts = await _context.PunctuationTests
                .Where(pt => classIds.Contains(pt.ClassId.Value))
                .GroupBy(pt => pt.ClassId)
                .Select(g => new { ClassId = g.Key, Count = g.Count() })
                .ToDictionaryAsync(x => x.ClassId.Value, x => x.Count);

            // Получаем количество тестов классических для каждого класса
            var regularTestsCounts = await _context.RegularTests
                .Where(pt => classIds.Contains(pt.ClassId.Value))
                .GroupBy(pt => pt.ClassId)
                .Select(g => new { ClassId = g.Key, Count = g.Count() })
                .ToDictionaryAsync(x => x.ClassId.Value, x => x.Count);

            // Получаем количество тестов по орфоэпии для каждого класса
            var orthoeopyTestsCounts = await _context.PunctuationTests
                .Where(pt => classIds.Contains(pt.ClassId.Value))
                .GroupBy(pt => pt.ClassId)
                .Select(g => new { ClassId = g.Key, Count = g.Count() })
                .ToDictionaryAsync(x => x.ClassId.Value, x => x.Count);


            // Создаем словарь с общим количеством тестов для каждого класса
            var totalTestsCounts = new Dictionary<int, int>();

            foreach (var @class in classes)
            {
                var regularTestsCount = regularTestsCounts.ContainsKey(@class.Id) ? regularTestsCounts[@class.Id] : 0;
                var spellingTestsCount = spellingTestsCounts.ContainsKey(@class.Id) ? spellingTestsCounts[@class.Id] : 0;
                var punctuationTestsCount = punctuationTestsCounts.ContainsKey(@class.Id) ? punctuationTestsCounts[@class.Id] : 0;
                var orthoeopyTestsCount = orthoeopyTestsCounts.ContainsKey(@class.Id) ? orthoeopyTestsCounts[@class.Id] : 0;

                var totalCount = regularTestsCount + spellingTestsCount + punctuationTestsCount + orthoeopyTestsCount;

                totalTestsCounts[@class.Id] = totalCount;
            }

            ViewBag.TotalTestsCounts = totalTestsCounts;

            return View(classes);
        }

        // GET: Class/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null) return NotFound();

            var currentUser = await _userManager.GetUserAsync(User);
            var @class = await _context.Classes
                .Include(c => c.Teacher)
                .Include(c => c.Students)
                    .ThenInclude(s => s.User)
                .Include(c => c.SpellingTests)
                    .ThenInclude(t => t.SpellingQuestions)
                .Include(c => c.SpellingTests)
                    .ThenInclude(t => t.SpellingTestResults)
                .Include(c => c.PunctuationTests)
                    .ThenInclude(t => t.PunctuationQuestions)
                .Include(c => c.PunctuationTests)
                    .ThenInclude(t => t.PunctuationTestResults)
                .Include(c => c.RegularTests)
                    .ThenInclude(t => t.RegularQuestions)
                .Include(c => c.RegularTests)
                    .ThenInclude(t => t.RegularTestResults)
                .Include(c => c.OrthoeopyTests)
                    .ThenInclude(t => t.OrthoeopyQuestions)
                .Include(c => c.OrthoeopyTests)
                    .ThenInclude(t => t.OrthoeopyTestResults)
                .Include(c => c.Materials)
                .FirstOrDefaultAsync(c => c.Id == id && c.TeacherId == currentUser.Id);

            if (@class == null) return NotFound();

            // Получаем все тесты по орфографии для этого класса
            var spellingTests = await _context.SpellingTests
                .Include(st => st.SpellingQuestions)
                .Include(st => st.SpellingTestResults)
                .Include(st => st.TestCategory)
                .Where(st => st.ClassId == id)
                .ToListAsync();

            // Получаем все тесты по пунктуации для этого класса
            var punctuationTests = await _context.PunctuationTests
                .Include(pt => pt.PunctuationQuestions)
                .Include(pt => pt.PunctuationTestResults)
                .Include(pt => pt.TestCategory)
                .Where(pt => pt.ClassId == id)
                .ToListAsync();

            // Получаем все тесты по орфоэпии для этого класса
            var orthoeopyTests = await _context.OrthoeopyTests
                .Include(pt => pt.OrthoeopyQuestions)
                .Include(pt => pt.OrthoeopyTestResults)
                .Include(pt => pt.TestCategory)
                .Where(pt => pt.ClassId == id)
                .ToListAsync();

            // Получаем все тесты классические для этого класса
            var regularTests = await _context.RegularTests
                .Include(pt => pt.RegularQuestions)
                .Include(pt => pt.RegularTestResults)
                .Include(pt => pt.TestCategory)
                .Where(pt => pt.ClassId == id)
                .ToListAsync();

            // Создаем объединенный список всех тестов
            var allTests = new List<object>();

            // Добавляем классические тесты
            foreach (var regularTest in @class.RegularTests)
            {
                allTests.Add(new
                {
                    Id = regularTest.Id,
                    Title = regularTest.Title,
                    Description = regularTest.Description,
                    CreatedAt = regularTest.CreatedAt,
                    CreatedAtFormatted = regularTest.CreatedAt.ToString("dd.MM.yyyy"),
                    IsActive = regularTest.IsActive,
                    TestType = "Regular",
                    TypeDisplayName = "Классический тест",
                    IconClass = "fas fa-tasks",
                    ColorClass = "info",
                    ControllerName = "RegularTest",
                    QuestionsCount = regularTest.RegularQuestions?.Count ?? 0,
                    TimeLimit = regularTest.TimeLimit,
                    ResultsCount = regularTest.RegularTestResults?.Count ?? 0,
                    MaxAttempts = regularTest.MaxAttempts,
                    StartDate = regularTest.StartDate,
                    StartDateFormatted = regularTest.StartDate?.ToString("dd.MM.yyyy HH:mm"),
                    EndDate = regularTest.EndDate,
                    EndDateFormatted = regularTest.EndDate?.ToString("dd.MM.yyyy HH:mm")
                });
            }

            // Добавляем тесты по орфографии
            foreach (var spellingTest in spellingTests)
            {
                allTests.Add(new
                {
                    Id = spellingTest.Id,
                    Title = spellingTest.Title,
                    Description = spellingTest.Description,
                    CreatedAt = spellingTest.CreatedAt,
                    CreatedAtFormatted = spellingTest.CreatedAt.ToString("dd.MM.yyyy"),
                    IsActive = spellingTest.IsActive,
                    TestType = "Spelling",
                    TypeDisplayName = "Орфография",
                    IconClass = "fas fa-spell-check",
                    ColorClass = "primary",
                    ControllerName = "SpellingTest",
                    QuestionsCount = spellingTest.SpellingQuestions?.Count ?? 0,
                    TimeLimit = spellingTest.TimeLimit,
                    ResultsCount = spellingTest.SpellingTestResults?.Count ?? 0,
                    MaxAttempts = spellingTest.MaxAttempts,
                    StartDate = spellingTest.StartDate,
                    StartDateFormatted = spellingTest.StartDate?.ToString("dd.MM.yyyy HH:mm"),
                    EndDate = spellingTest.EndDate,
                    EndDateFormatted = spellingTest.EndDate?.ToString("dd.MM.yyyy HH:mm")
                });
            }

            // Добавляем тесты по пунктуации
            foreach (var punctuationTest in punctuationTests)
            {
                allTests.Add(new
                {
                    Id = punctuationTest.Id,
                    Title = punctuationTest.Title,
                    Description = punctuationTest.Description,
                    CreatedAt = punctuationTest.CreatedAt,
                    CreatedAtFormatted = punctuationTest.CreatedAt.ToString("dd.MM.yyyy"),
                    IsActive = punctuationTest.IsActive,
                    TestType = "Punctuation",
                    TypeDisplayName = "Пунктуация",
                    IconClass = "fas fa-quote-right",
                    ColorClass = "success",
                    ControllerName = "PunctuationTest",
                    QuestionsCount = punctuationTest.PunctuationQuestions?.Count ?? 0,
                    TimeLimit = punctuationTest.TimeLimit,
                    ResultsCount = punctuationTest.PunctuationTestResults?.Count ?? 0,
                    MaxAttempts = punctuationTest.MaxAttempts,
                    StartDate = punctuationTest.StartDate,
                    StartDateFormatted = punctuationTest.StartDate?.ToString("dd.MM.yyyy HH:mm"),
                    EndDate = punctuationTest.EndDate,
                    EndDateFormatted = punctuationTest.EndDate?.ToString("dd.MM.yyyy HH:mm")
                });
            }

            // Добавляем тесты по орфоэпии
            foreach (var orthoeopyTest in orthoeopyTests)
            {
                allTests.Add(new
                {
                    Id = orthoeopyTest.Id,
                    Title = orthoeopyTest.Title,
                    Description = orthoeopyTest.Description,
                    CreatedAt = orthoeopyTest.CreatedAt,
                    CreatedAtFormatted = orthoeopyTest.CreatedAt.ToString("dd.MM.yyyy"),
                    IsActive = orthoeopyTest.IsActive,
                    TestType = "Punctuation",
                    TypeDisplayName = "Орфоэпия",
                    IconClass = "fas fa-check",
                    ColorClass = "success",
                    ControllerName = "OrthoeopyTest",
                    QuestionsCount = orthoeopyTest.OrthoeopyQuestions?.Count ?? 0,
                    TimeLimit = orthoeopyTest.TimeLimit,
                    ResultsCount = orthoeopyTest.OrthoeopyTestResults?.Count ?? 0,
                    MaxAttempts = orthoeopyTest.MaxAttempts,
                    StartDate = orthoeopyTest.StartDate,
                    StartDateFormatted = orthoeopyTest.StartDate?.ToString("dd.MM.yyyy HH:mm"),
                    EndDate = orthoeopyTest.EndDate,
                    EndDateFormatted = orthoeopyTest.EndDate?.ToString("dd.MM.yyyy HH:mm")
                });
            }

            // Сортируем по дате создания (новые первыми)
            allTests = allTests.OrderByDescending(t => ((dynamic)t).CreatedAt).ToList();

            ViewBag.AllTests = allTests;
            ViewBag.AllTestsCount = allTests.Count;
            ViewBag.SpellingTestsCount = spellingTests.Count;
            ViewBag.PunctuationTestsCount = punctuationTests.Count;
            ViewBag.RegularTestsCount = regularTests.Count;
            ViewBag.OrthoeopyTestsCount = orthoeopyTests.Count;

            return View(@class);
        }

        // GET: Class/Create
        public IActionResult Create()
        {
            return View();
        }

        // POST: Class/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(CreateClassViewModel model)
        {
            var currentUser = await _userManager.GetUserAsync(User);

            if (ModelState.IsValid)
            {
                var @class = new Class
                {
                    Name = model.Name,
                    Description = model.Description,
                    TeacherId = currentUser.Id
                };

                _context.Classes.Add(@class);
                await _context.SaveChangesAsync();

                _logger.LogInformation("Учитель {TeacherId} создал класс {ClassId}: {ClassName}", currentUser.Id, @class.Id, @class.Name);

                TempData["SuccessMessage"] = $"Класс \"{@class.Name}\" успешно создан!";
                return RedirectToAction(nameof(Index));
            }

            _logger.LogWarning("Учитель {TeacherId} отправил невалидную форму создания класса", currentUser.Id);
            return View(model);
        }

        // GET: Class/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null) return NotFound();

            var currentUser = await _userManager.GetUserAsync(User);
            var @class = await _context.Classes
                .FirstOrDefaultAsync(c => c.Id == id && c.TeacherId == currentUser.Id);

            if (@class == null) return NotFound();

            var model = new EditClassViewModel
            {
                Id = @class.Id,
                Name = @class.Name,
                Description = @class.Description,
                IsActive = @class.IsActive
            };

            return View(model);
        }

        // POST: Class/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, EditClassViewModel model)
        {
            if (id != model.Id) return NotFound();

            var currentUser = await _userManager.GetUserAsync(User);

            if (ModelState.IsValid)
            {
                try
                {
                    var @class = await _context.Classes
                        .FirstOrDefaultAsync(c => c.Id == id && c.TeacherId == currentUser.Id);

                    if (@class == null) return NotFound();

                    @class.Name = model.Name;
                    @class.Description = model.Description;
                    @class.IsActive = model.IsActive;

                    _context.Update(@class);
                    await _context.SaveChangesAsync();

                    _logger.LogInformation("Учитель {TeacherId} обновил класс {ClassId}: {ClassName}, IsActive: {IsActive}",
                        currentUser.Id, id, @class.Name, @class.IsActive);

                    TempData["SuccessMessage"] = $"Класс \"{@class.Name}\" успешно обновлен!";
                    return RedirectToAction(nameof(Index));
                }
                catch (DbUpdateConcurrencyException ex)
                {
                    _logger.LogError(ex, "Ошибка конкурентности при обновлении класса {ClassId} учителем {TeacherId}", id, currentUser.Id);
                    ModelState.AddModelError("", "Произошла ошибка при сохранении. Попробуйте еще раз.");
                }
            }

            _logger.LogWarning("Учитель {TeacherId} отправил невалидную форму обновления класса {ClassId}", currentUser.Id, id);
            return View(model);
        }

        // GET: Class/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null) return NotFound();

            var currentUser = await _userManager.GetUserAsync(User);

            var @class = await _context.Classes
                .Include(c => c.Students)
                .Include(c => c.RegularTests)
                .Include(c => c.SpellingTests)
                .Include(c => c.PunctuationTests)
                .Include(c => c.OrthoeopyTests)
                .Include(c => c.Materials)
                .FirstOrDefaultAsync(c => c.Id == id && c.TeacherId == currentUser.Id);

            if (@class == null) return NotFound();

            return View(@class);
        }

        // POST: Class/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var currentUser = await _userManager.GetUserAsync(User);

            var @class = await _context.Classes
                .Include(c => c.Students)
                .FirstOrDefaultAsync(c => c.Id == id && c.TeacherId == currentUser.Id);

            if (@class == null) return NotFound();

            // Проверяем, есть ли ученики в классе
            if (@class.Students.Any())
            {
                _logger.LogWarning("Учитель {TeacherId} попытался удалить класс {ClassId} с учениками ({StudentsCount})",
                    currentUser.Id, id, @class.Students.Count);
                TempData["ErrorMessage"] = "Нельзя удалить класс, в котором есть ученики. Сначала переместите учеников в другой класс или удалите их.";
                return RedirectToAction(nameof(Delete), new { id });
            }

            var className = @class.Name;
            _context.Classes.Remove(@class);
            await _context.SaveChangesAsync();

            _logger.LogInformation("Учитель {TeacherId} удалил класс {ClassId}: {ClassName}", currentUser.Id, id, className);

            TempData["SuccessMessage"] = $"Класс \"{@class.Name}\" успешно удален!";
            return RedirectToAction(nameof(Index));
        }

        // POST: Class/ToggleStatus/5
        [HttpPost]
        public async Task<IActionResult> ToggleStatus(int id)
        {
            var currentUser = await _userManager.GetUserAsync(User);
            var @class = await _context.Classes
                .FirstOrDefaultAsync(c => c.Id == id && c.TeacherId == currentUser.Id);

            if (@class == null) return NotFound();

            var oldStatus = @class.IsActive;
            @class.IsActive = !@class.IsActive;
            await _context.SaveChangesAsync();

            _logger.LogInformation("Учитель {TeacherId} изменил статус класса {ClassId}: {ClassName} с {OldStatus} на {NewStatus}",
                currentUser.Id, id, @class.Name, oldStatus, @class.IsActive);

            var status = @class.IsActive ? "активирован" : "деактивирован";
            TempData["InfoMessage"] = $"Класс \"{@class.Name}\" {status}.";

            return RedirectToAction(nameof(Index));
        }
    }
}
