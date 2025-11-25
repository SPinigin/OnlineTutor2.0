using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using OnlineTutor2.Data;
using OnlineTutor2.Data.Repositories;
using OnlineTutor2.Models;
using OnlineTutor2.ViewModels;

namespace OnlineTutor2.Controllers
{
    [Authorize(Roles = ApplicationRoles.Teacher)]
    public class ClassController : Controller
    {
        private readonly IClassRepository _classRepository;
        private readonly IStudentRepository _studentRepository;
        private readonly IMaterialRepository _materialRepository;
        private readonly IRegularTestClassRepository _regularTestClassRepository;
        private readonly ISpellingTestClassRepository _spellingTestClassRepository;
        private readonly IPunctuationTestClassRepository _punctuationTestClassRepository;
        private readonly IOrthoeopyTestClassRepository _orthoeopyTestClassRepository;
        private readonly IRegularTestRepository _regularTestRepository;
        private readonly ISpellingTestRepository _spellingTestRepository;
        private readonly IPunctuationTestRepository _punctuationTestRepository;
        private readonly IOrthoeopyTestRepository _orthoeopyTestRepository;
        private readonly IRegularQuestionRepository _regularQuestionRepository;
        private readonly ISpellingQuestionRepository _spellingQuestionRepository;
        private readonly IPunctuationQuestionRepository _punctuationQuestionRepository;
        private readonly IOrthoeopyQuestionRepository _orthoeopyQuestionRepository;
        private readonly IRegularTestResultRepository _regularTestResultRepository;
        private readonly ISpellingTestResultRepository _spellingTestResultRepository;
        private readonly IPunctuationTestResultRepository _punctuationTestResultRepository;
        private readonly IOrthoeopyTestResultRepository _orthoeopyTestResultRepository;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly ILogger<ClassController> _logger;


        public ClassController(
            IClassRepository classRepository,
            IStudentRepository studentRepository,
            IMaterialRepository materialRepository,
            IRegularTestClassRepository regularTestClassRepository,
            ISpellingTestClassRepository spellingTestClassRepository,
            IPunctuationTestClassRepository punctuationTestClassRepository,
            IOrthoeopyTestClassRepository orthoeopyTestClassRepository,
            IRegularTestRepository regularTestRepository,
            ISpellingTestRepository spellingTestRepository,
            IPunctuationTestRepository punctuationTestRepository,
            IOrthoeopyTestRepository orthoeopyTestRepository,
            IRegularQuestionRepository regularQuestionRepository,
            ISpellingQuestionRepository spellingQuestionRepository,
            IPunctuationQuestionRepository punctuationQuestionRepository,
            IOrthoeopyQuestionRepository orthoeopyQuestionRepository,
            IRegularTestResultRepository regularTestResultRepository,
            ISpellingTestResultRepository spellingTestResultRepository,
            IPunctuationTestResultRepository punctuationTestResultRepository,
            IOrthoeopyTestResultRepository orthoeopyTestResultRepository,
            UserManager<ApplicationUser> userManager,
            ILogger<ClassController> logger)
        {
            _classRepository = classRepository;
            _studentRepository = studentRepository;
            _materialRepository = materialRepository;
            _regularTestClassRepository = regularTestClassRepository;
            _spellingTestClassRepository = spellingTestClassRepository;
            _punctuationTestClassRepository = punctuationTestClassRepository;
            _orthoeopyTestClassRepository = orthoeopyTestClassRepository;
            _regularTestRepository = regularTestRepository;
            _spellingTestRepository = spellingTestRepository;
            _punctuationTestRepository = punctuationTestRepository;
            _orthoeopyTestRepository = orthoeopyTestRepository;
            _regularQuestionRepository = regularQuestionRepository;
            _spellingQuestionRepository = spellingQuestionRepository;
            _punctuationQuestionRepository = punctuationQuestionRepository;
            _orthoeopyQuestionRepository = orthoeopyQuestionRepository;
            _regularTestResultRepository = regularTestResultRepository;
            _spellingTestResultRepository = spellingTestResultRepository;
            _punctuationTestResultRepository = punctuationTestResultRepository;
            _orthoeopyTestResultRepository = orthoeopyTestResultRepository;
            _userManager = userManager;
            _logger = logger;
        }

        // GET: Class
        public async Task<IActionResult> Index()
        {
            var currentUser = await _userManager.GetUserAsync(User);

            var classes = await _classRepository.GetByTeacherIdAsync(currentUser.Id);

            foreach (var classItem in classes)
            {
                await PopulateClassRelationsAsync(classItem);
            }

            // LINQ подсчет всех тестов одним выражением
            var totalTestsCounts = classes.ToDictionary(
                c => c.Id,
                c => (c.RegularTestClasses?.Count ?? 0) +
                     (c.SpellingTestClasses?.Count ?? 0) +
                     (c.PunctuationTestClasses?.Count ?? 0) +
                     (c.OrthoeopyTestClasses?.Count ?? 0)
            );

            ViewBag.TotalTestsCounts = totalTestsCounts;

            return View(classes);
        }

        // GET: Class/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null) return NotFound();

            var currentUser = await _userManager.GetUserAsync(User);

            var @class = await LoadClassWithDetailsAsync(id.Value, currentUser.Id, includeStudentUsers: true);

            if (@class == null) return NotFound();

            var testsSummary = await BuildTestsSummaryAsync(@class);

            ViewBag.AllTests = testsSummary.Tests.Cast<object>().ToList();
            ViewBag.AllTestsCount = testsSummary.TotalCount;
            ViewBag.SpellingTestsCount = testsSummary.SpellingCount;
            ViewBag.PunctuationTestsCount = testsSummary.PunctuationCount;
            ViewBag.RegularTestsCount = testsSummary.RegularCount;
            ViewBag.OrthoeopyTestsCount = testsSummary.OrthoeopyCount;

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

                var classId = await _classRepository.CreateAsync(@class);
                @class.Id = classId;

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
            var @class = await _classRepository.GetByIdAsync(id.Value);

            if (@class == null || @class.TeacherId != currentUser.Id) return NotFound();


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
                    var @class = await _classRepository.GetByIdAsync(id);

                    if (@class == null || @class.TeacherId != currentUser.Id) return NotFound();

                    @class.Name = model.Name;
                    @class.Description = model.Description;
                    @class.IsActive = model.IsActive;

                    await _classRepository.UpdateAsync(@class);

                    _logger.LogInformation("Учитель {TeacherId} обновил класс {ClassId}: {ClassName}, IsActive: {IsActive}",
                        currentUser.Id, id, @class.Name, @class.IsActive);

                    TempData["SuccessMessage"] = $"Класс \"{@class.Name}\" успешно обновлен!";
                    return RedirectToAction(nameof(Index));
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Ошибка при обновлении класса {ClassId} учителем {TeacherId}", id, currentUser.Id);
                    ModelState.AddModelError(string.Empty, "Произошла ошибка при сохранении. Попробуйте еще раз.");
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

            var @class = await LoadClassWithDetailsAsync(id.Value, currentUser.Id);

            if (@class == null) return NotFound();

            return View(@class);
        }

        // POST: Class/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var currentUser = await _userManager.GetUserAsync(User);

            var @class = await LoadClassWithDetailsAsync(id, currentUser.Id);

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
            await _classRepository.DeleteAsync(id);

            _logger.LogInformation("Учитель {TeacherId} удалил класс {ClassId}: {ClassName}", currentUser.Id, id, className);

            TempData["SuccessMessage"] = $"Класс \"{@class.Name}\" успешно удален!";
            return RedirectToAction(nameof(Index));
        }

        // POST: Class/ToggleStatus/5
        [HttpPost]
        public async Task<IActionResult> ToggleStatus(int id)
        {
            var currentUser = await _userManager.GetUserAsync(User);
            var @class = await _classRepository.GetByIdAsync(id);

            if (@class == null || @class.TeacherId != currentUser.Id) return NotFound();

            var oldStatus = @class.IsActive;
            @class.IsActive = !@class.IsActive;
            await _classRepository.UpdateAsync(@class);

            _logger.LogInformation("Учитель {TeacherId} изменил статус класса {ClassId}: {ClassName} с {OldStatus} на {NewStatus}",
                currentUser.Id, id, @class.Name, oldStatus, @class.IsActive);

            var status = @class.IsActive ? "активирован" : "деактивирован";
            TempData["InfoMessage"] = $"Класс \"{@class.Name}\" {status}.";

            return RedirectToAction(nameof(Index));
        }

        private async Task<Class?> LoadClassWithDetailsAsync(int classId, string teacherId, bool includeStudentUsers = false)
        {
            var classItem = await _classRepository.GetByIdAsync(classId);
            if (classItem == null || !IsOwnedByTeacher(classItem.TeacherId, teacherId))
            {
                return null;
            }

            await PopulateClassRelationsAsync(classItem, includeStudentUsers);
            return classItem;
        }

        private async Task PopulateClassRelationsAsync(Class classItem, bool includeStudentUsers = false)
        {
            classItem.Students = await _studentRepository.GetByClassIdAsync(classItem.Id);
            if (includeStudentUsers)
            {
                await LoadStudentUsersAsync(classItem.Students);
            }

            classItem.Materials = await _materialRepository.GetByClassIdAsync(classItem.Id);
            classItem.RegularTestClasses = await _regularTestClassRepository.GetByClassIdAsync(classItem.Id);
            classItem.SpellingTestClasses = await _spellingTestClassRepository.GetByClassIdAsync(classItem.Id);
            classItem.PunctuationTestClasses = await _punctuationTestClassRepository.GetByClassIdAsync(classItem.Id);
            classItem.OrthoeopyTestClasses = await _orthoeopyTestClassRepository.GetByClassIdAsync(classItem.Id);
        }

        private async Task LoadStudentUsersAsync(ICollection<Student> students)
        {
            if (students == null || students.Count == 0)
            {
                return;
            }

            var userIds = students
                .Select(s => s.UserId)
                .Where(id => !string.IsNullOrEmpty(id))
                .Distinct()
                .ToList();

            var users = new Dictionary<string, ApplicationUser>();

            foreach (var userId in userIds)
            {
                var user = await _userManager.FindByIdAsync(userId);
                if (user != null)
                {
                    users[userId] = user;
                }
            }

            foreach (var student in students)
            {
                if (!string.IsNullOrEmpty(student.UserId) &&
                    users.TryGetValue(student.UserId, out var user))
                {
                    student.User = user;
                }
            }
        }

        private async Task<TestsSummary> BuildTestsSummaryAsync(Class @class)
        {
            var summary = new TestsSummary();

            var regularLinks = await _regularTestClassRepository.GetByClassIdAsync(@class.Id);
            summary.RegularCount = regularLinks.Count;
            foreach (var link in regularLinks)
            {
                var test = await _regularTestRepository.GetByIdAsync(link.RegularTestId);
                if (test == null || !IsOwnedByTeacher(test.TeacherId, @class.TeacherId)) continue;

                var questionsCount = await _regularQuestionRepository.GetCountByTestIdAsync(test.Id);
                var resultsCount = await _regularTestResultRepository.GetCountByTestIdAsync(test.Id);

                summary.Tests.Add(new TestInfo
                {
                    Id = test.Id,
                    Title = test.Title,
                    Description = test.Description,
                    CreatedAt = test.CreatedAt,
                    IsActive = test.IsActive,
                    TestType = "Regular",
                    TypeDisplayName = "Классический тест",
                    IconClass = "fas fa-tasks",
                    ColorClass = "info",
                    ControllerName = "RegularTest",
                    QuestionsCount = questionsCount,
                    TimeLimit = test.TimeLimit,
                    ResultsCount = resultsCount,
                    MaxAttempts = test.MaxAttempts,
                    StartDate = test.StartDate,
                    EndDate = test.EndDate
                });
            }

            var spellingLinks = await _spellingTestClassRepository.GetByClassIdAsync(@class.Id);
            summary.SpellingCount = spellingLinks.Count;
            foreach (var link in spellingLinks)
            {
                var test = await _spellingTestRepository.GetByIdAsync(link.SpellingTestId);
                if (test == null || !IsOwnedByTeacher(test.TeacherId, @class.TeacherId)) continue;

                var questionsCount = await _spellingQuestionRepository.GetCountByTestIdAsync(test.Id);
                var resultsCount = await _spellingTestResultRepository.GetCountByTestIdAsync(test.Id);

                summary.Tests.Add(new TestInfo
                {
                    Id = test.Id,
                    Title = test.Title,
                    Description = test.Description,
                    CreatedAt = test.CreatedAt,
                    IsActive = test.IsActive,
                    TestType = "Spelling",
                    TypeDisplayName = "Орфография",
                    IconClass = "fas fa-spell-check",
                    ColorClass = "primary",
                    ControllerName = "SpellingTest",
                    QuestionsCount = questionsCount,
                    TimeLimit = test.TimeLimit,
                    ResultsCount = resultsCount,
                    MaxAttempts = test.MaxAttempts,
                    StartDate = test.StartDate,
                    EndDate = test.EndDate
                });
            }

            var punctuationLinks = await _punctuationTestClassRepository.GetByClassIdAsync(@class.Id);
            summary.PunctuationCount = punctuationLinks.Count;
            foreach (var link in punctuationLinks)
            {
                var test = await _punctuationTestRepository.GetByIdAsync(link.PunctuationTestId);
                if (test == null || !IsOwnedByTeacher(test.TeacherId, @class.TeacherId)) continue;

                var questionsCount = await _punctuationQuestionRepository.GetCountByTestIdAsync(test.Id);
                var resultsCount = await _punctuationTestResultRepository.GetCountByTestIdAsync(test.Id);

                summary.Tests.Add(new TestInfo
                {
                    Id = test.Id,
                    Title = test.Title,
                    Description = test.Description,
                    CreatedAt = test.CreatedAt,
                    IsActive = test.IsActive,
                    TestType = "Punctuation",
                    TypeDisplayName = "Пунктуация",
                    IconClass = "fas fa-quote-right",
                    ColorClass = "success",
                    ControllerName = "PunctuationTest",
                    QuestionsCount = questionsCount,
                    TimeLimit = test.TimeLimit,
                    ResultsCount = resultsCount,
                    MaxAttempts = test.MaxAttempts,
                    StartDate = test.StartDate,
                    EndDate = test.EndDate
                });
            }

            var orthoeopyLinks = await _orthoeopyTestClassRepository.GetByClassIdAsync(@class.Id);
            summary.OrthoeopyCount = orthoeopyLinks.Count;
            foreach (var link in orthoeopyLinks)
            {
                var test = await _orthoeopyTestRepository.GetByIdAsync(link.OrthoeopyTestId);
                if (test == null || !IsOwnedByTeacher(test.TeacherId, @class.TeacherId)) continue;

                var questionsCount = await _orthoeopyQuestionRepository.GetCountByTestIdAsync(test.Id);
                var resultsCount = await _orthoeopyTestResultRepository.GetCountByTestIdAsync(test.Id);

                summary.Tests.Add(new TestInfo
                {
                    Id = test.Id,
                    Title = test.Title,
                    Description = test.Description,
                    CreatedAt = test.CreatedAt,
                    IsActive = test.IsActive,
                    TestType = "Orthoeopy",
                    TypeDisplayName = "Орфоэпия",
                    IconClass = "fas fa-volume-up",
                    ColorClass = "warning",
                    ControllerName = "OrthoeopyTest",
                    QuestionsCount = questionsCount,
                    TimeLimit = test.TimeLimit,
                    ResultsCount = resultsCount,
                    MaxAttempts = test.MaxAttempts,
                    StartDate = test.StartDate,
                    EndDate = test.EndDate
                });
            }

            summary.Tests = summary.Tests
                .OrderByDescending(t => t.CreatedAt)
                .ToList();

            return summary;
        }

        private static bool IsOwnedByTeacher(string? ownerId, string teacherId)
        {
            return !string.IsNullOrEmpty(ownerId) &&
                   string.Equals(ownerId, teacherId, StringComparison.OrdinalIgnoreCase);
        }

        private sealed class TestsSummary
        {
            public List<TestInfo> Tests { get; set; } = new();
            public int RegularCount { get; set; }
            public int SpellingCount { get; set; }
            public int PunctuationCount { get; set; }
            public int OrthoeopyCount { get; set; }
            public int TotalCount => RegularCount + SpellingCount + PunctuationCount + OrthoeopyCount;
        }

        private sealed class TestInfo
        {
            public int Id { get; init; }
            public string Title { get; init; } = string.Empty;
            public string? Description { get; init; }
            public DateTime CreatedAt { get; init; }
            public string CreatedAtFormatted => CreatedAt.ToString("dd.MM.yyyy");
            public bool IsActive { get; init; }
            public string TestType { get; init; } = string.Empty;
            public string TypeDisplayName { get; init; } = string.Empty;
            public string IconClass { get; init; } = string.Empty;
            public string ColorClass { get; init; } = string.Empty;
            public string ControllerName { get; init; } = string.Empty;
            public int QuestionsCount { get; init; }
            public int TimeLimit { get; init; }
            public int ResultsCount { get; init; }
            public int MaxAttempts { get; init; }
            public DateTime? StartDate { get; init; }
            public string? StartDateFormatted => StartDate?.ToString("dd.MM.yyyy HH:mm");
            public DateTime? EndDate { get; init; }
            public string? EndDateFormatted => EndDate?.ToString("dd.MM.yyyy HH:mm");
        }
    }
}
