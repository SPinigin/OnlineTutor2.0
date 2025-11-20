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


        public ClassController(ApplicationDbContext context, 
            UserManager<ApplicationUser> userManager, 
            ILogger<ClassController> logger)
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

            // LINQ подсчет всех тестов одним выражением
            var totalTestsCounts = classes.ToDictionary(
                c => c.Id,
                c => (c.RegularTests?.Count ?? 0) +
                     (c.SpellingTests?.Count ?? 0) +
                     (c.PunctuationTests?.Count ?? 0) +
                     (c.OrthoeopyTests?.Count ?? 0)
            );

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
                .Include(c => c.Students).ThenInclude(s => s.User)
                .Include(c => c.SpellingTests).ThenInclude(stc => stc.SpellingTest).ThenInclude(st => st.SpellingQuestions)
                .Include(c => c.SpellingTests).ThenInclude(stc => stc.SpellingTest).ThenInclude(st => st.SpellingTestResults)
                .Include(c => c.PunctuationTests).ThenInclude(ptc => ptc.PunctuationTest).ThenInclude(pt => pt.PunctuationQuestions)
                .Include(c => c.PunctuationTests).ThenInclude(ptc => ptc.PunctuationTest).ThenInclude(pt => pt.PunctuationTestResults)
                .Include(c => c.OrthoeopyTests).ThenInclude(otc => otc.OrthoeopyTest).ThenInclude(ot => ot.OrthoeopyQuestions)
                .Include(c => c.OrthoeopyTests).ThenInclude(otc => otc.OrthoeopyTest).ThenInclude(ot => ot.OrthoeopyTestResults)
                .Include(c => c.RegularTests).ThenInclude(rtc => rtc.RegularTest).ThenInclude(rt => rt.RegularQuestions)
                .Include(c => c.RegularTests).ThenInclude(rtc => rtc.RegularTest).ThenInclude(rt => rt.RegularTestResults)
                .Include(c => c.Materials)
                .FirstOrDefaultAsync(c => c.Id == id && c.TeacherId == currentUser.Id);

            if (@class == null) return NotFound();

            // Извлекаем тесты
            var spellingTests = @class.SpellingTests.Select(x => x.SpellingTest).Where(x => x != null).ToList();
            var punctuationTests = @class.PunctuationTests.Select(x => x.PunctuationTest).Where(x => x != null).ToList();
            var orthoeopyTests = @class.OrthoeopyTests.Select(x => x.OrthoeopyTest).Where(x => x != null).ToList();
            var regularTests = @class.RegularTests.Select(x => x.RegularTest).Where(x => x != null).ToList();

            // Создаем общий список с единым анонимным типом
            var allTests = regularTests.Select(t => new
            {
                Id = t.Id,
                Title = t.Title,
                Description = t.Description,
                CreatedAt = t.CreatedAt,
                CreatedAtFormatted = t.CreatedAt.ToString("dd.MM.yyyy"),
                IsActive = t.IsActive,
                TestType = "Regular",
                TypeDisplayName = "Классический тест",
                IconClass = "fas fa-tasks",
                ColorClass = "info",
                ControllerName = "RegularTest",
                QuestionsCount = t.RegularQuestions?.Count ?? 0,
                TimeLimit = t.TimeLimit,
                ResultsCount = t.RegularTestResults?.Count ?? 0,
                MaxAttempts = t.MaxAttempts,
                StartDate = t.StartDate,
                StartDateFormatted = t.StartDate?.ToString("dd.MM.yyyy HH:mm"),
                EndDate = t.EndDate,
                EndDateFormatted = t.EndDate?.ToString("dd.MM.yyyy HH:mm")
            })
            .Concat(spellingTests.Select(t => new
            {
                Id = t.Id,
                Title = t.Title,
                Description = t.Description,
                CreatedAt = t.CreatedAt,
                CreatedAtFormatted = t.CreatedAt.ToString("dd.MM.yyyy"),
                IsActive = t.IsActive,
                TestType = "Spelling",
                TypeDisplayName = "Орфография",
                IconClass = "fas fa-spell-check",
                ColorClass = "primary",
                ControllerName = "SpellingTest",
                QuestionsCount = t.SpellingQuestions?.Count ?? 0,
                TimeLimit = t.TimeLimit,
                ResultsCount = t.SpellingTestResults?.Count ?? 0,
                MaxAttempts = t.MaxAttempts,
                StartDate = t.StartDate,
                StartDateFormatted = t.StartDate?.ToString("dd.MM.yyyy HH:mm"),
                EndDate = t.EndDate,
                EndDateFormatted = t.EndDate?.ToString("dd.MM.yyyy HH:mm")
            }))
            .Concat(punctuationTests.Select(t => new
            {
                Id = t.Id,
                Title = t.Title,
                Description = t.Description,
                CreatedAt = t.CreatedAt,
                CreatedAtFormatted = t.CreatedAt.ToString("dd.MM.yyyy"),
                IsActive = t.IsActive,
                TestType = "Punctuation",
                TypeDisplayName = "Пунктуация",
                IconClass = "fas fa-quote-right",
                ColorClass = "success",
                ControllerName = "PunctuationTest",
                QuestionsCount = t.PunctuationQuestions?.Count ?? 0,
                TimeLimit = t.TimeLimit,
                ResultsCount = t.PunctuationTestResults?.Count ?? 0,
                MaxAttempts = t.MaxAttempts,
                StartDate = t.StartDate,
                StartDateFormatted = t.StartDate?.ToString("dd.MM.yyyy HH:mm"),
                EndDate = t.EndDate,
                EndDateFormatted = t.EndDate?.ToString("dd.MM.yyyy HH:mm")
            }))
            .Concat(orthoeopyTests.Select(t => new
            {
                Id = t.Id,
                Title = t.Title,
                Description = t.Description,
                CreatedAt = t.CreatedAt,
                CreatedAtFormatted = t.CreatedAt.ToString("dd.MM.yyyy"),
                IsActive = t.IsActive,
                TestType = "Orthoeopy",
                TypeDisplayName = "Орфоэпия",
                IconClass = "fas fa-volume-up",
                ColorClass = "warning",
                ControllerName = "OrthoeopyTest",
                QuestionsCount = t.OrthoeopyQuestions?.Count ?? 0,
                TimeLimit = t.TimeLimit,
                ResultsCount = t.OrthoeopyTestResults?.Count ?? 0,
                MaxAttempts = t.MaxAttempts,
                StartDate = t.StartDate,
                StartDateFormatted = t.StartDate?.ToString("dd.MM.yyyy HH:mm"),
                EndDate = t.EndDate,
                EndDateFormatted = t.EndDate?.ToString("dd.MM.yyyy HH:mm")
            }))
            .OrderByDescending(t => t.CreatedAt)
            .ToList();

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
