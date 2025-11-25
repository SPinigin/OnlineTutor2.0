using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using OnlineTutor2.Data;
using OnlineTutor2.Data.Repositories;
using OnlineTutor2.Models;
using OnlineTutor2.Services;
using OnlineTutor2.ViewModels;

namespace OnlineTutor2.Controllers.Admin
{
    /// <summary>
    /// Контроллер для управления тестами администратором
    /// </summary>
    public class AdminTestsController : AdminBaseController
    {
        private readonly ISpellingTestRepository _spellingTestRepository;
        private readonly IRegularTestRepository _regularTestRepository;
        private readonly IPunctuationTestRepository _punctuationTestRepository;
        private readonly IOrthoeopyTestRepository _orthoeopyTestRepository;
        private readonly ISpellingQuestionRepository _spellingQuestionRepository;
        private readonly IRegularQuestionRepository _regularQuestionRepository;
        private readonly IPunctuationQuestionRepository _punctuationQuestionRepository;
        private readonly IOrthoeopyQuestionRepository _orthoeopyQuestionRepository;
        private readonly ISpellingTestResultRepository _spellingTestResultRepository;
        private readonly IRegularTestResultRepository _regularTestResultRepository;
        private readonly IPunctuationTestResultRepository _punctuationTestResultRepository;
        private readonly IOrthoeopyTestResultRepository _orthoeopyTestResultRepository;
        private readonly ISpellingTestClassRepository _spellingTestClassRepository;
        private readonly IRegularTestClassRepository _regularTestClassRepository;
        private readonly IPunctuationTestClassRepository _punctuationTestClassRepository;
        private readonly IOrthoeopyTestClassRepository _orthoeopyTestClassRepository;
        private readonly ITeacherRepository _teacherRepository;
        private readonly IClassRepository _classRepository;

        public AdminTestsController(
            IDatabaseConnection db,
            UserManager<ApplicationUser> userManager,
            RoleManager<IdentityRole> roleManager,
            ILogger<AdminTestsController> logger,
            IAuditLogService auditLogService,
            IHttpContextAccessor httpContextAccessor,
            ISpellingTestRepository spellingTestRepository,
            IRegularTestRepository regularTestRepository,
            IPunctuationTestRepository punctuationTestRepository,
            IOrthoeopyTestRepository orthoeopyTestRepository,
            ISpellingQuestionRepository spellingQuestionRepository,
            IRegularQuestionRepository regularQuestionRepository,
            IPunctuationQuestionRepository punctuationQuestionRepository,
            IOrthoeopyQuestionRepository orthoeopyQuestionRepository,
            ISpellingTestResultRepository spellingTestResultRepository,
            IRegularTestResultRepository regularTestResultRepository,
            IPunctuationTestResultRepository punctuationTestResultRepository,
            IOrthoeopyTestResultRepository orthoeopyTestResultRepository,
            ISpellingTestClassRepository spellingTestClassRepository,
            IRegularTestClassRepository regularTestClassRepository,
            IPunctuationTestClassRepository punctuationTestClassRepository,
            IOrthoeopyTestClassRepository orthoeopyTestClassRepository,
            ITeacherRepository teacherRepository,
            IClassRepository classRepository,
            IStatisticsRepository statisticsRepository)
            : base(db, userManager, roleManager, logger, auditLogService, httpContextAccessor, statisticsRepository)
        {
            _spellingTestRepository = spellingTestRepository;
            _regularTestRepository = regularTestRepository;
            _punctuationTestRepository = punctuationTestRepository;
            _orthoeopyTestRepository = orthoeopyTestRepository;
            _spellingQuestionRepository = spellingQuestionRepository;
            _regularQuestionRepository = regularQuestionRepository;
            _punctuationQuestionRepository = punctuationQuestionRepository;
            _orthoeopyQuestionRepository = orthoeopyQuestionRepository;
            _spellingTestResultRepository = spellingTestResultRepository;
            _regularTestResultRepository = regularTestResultRepository;
            _punctuationTestResultRepository = punctuationTestResultRepository;
            _orthoeopyTestResultRepository = orthoeopyTestResultRepository;
            _spellingTestClassRepository = spellingTestClassRepository;
            _regularTestClassRepository = regularTestClassRepository;
            _punctuationTestClassRepository = punctuationTestClassRepository;
            _orthoeopyTestClassRepository = orthoeopyTestClassRepository;
            _teacherRepository = teacherRepository;
            _classRepository = classRepository;
        }

        // GET: Admin/Tests
        public async Task<IActionResult> Index()
        {
            var allTests = new List<AdminTestViewModel>();

            // Получаем тесты по орфографии
            var spellingTestsList = await _spellingTestRepository.GetAllAsync();
            foreach (var st in spellingTestsList)
            {
                var teacher = await _teacherRepository.GetByUserIdAsync(st.TeacherId);
                var teacherName = teacher != null ? await UserManager.FindByIdAsync(st.TeacherId) : null;
                var testClasses = await _spellingTestClassRepository.GetByTestIdAsync(st.Id);
                var questions = await _spellingQuestionRepository.GetByTestIdAsync(st.Id);
                var results = await _spellingTestResultRepository.GetByTestIdAsync(st.Id);
                
                var classNames = new List<string>();
                foreach (var tc in testClasses)
                {
                    var @class = await _classRepository.GetByIdAsync(tc.ClassId);
                    if (@class != null) classNames.Add(@class.Name);
                }

                allTests.Add(new AdminTestViewModel
                {
                    Id = st.Id,
                    Title = st.Title,
                    Type = "Орфография",
                    TeacherName = teacherName != null ? $"{teacherName.FirstName} {teacherName.LastName}" : "Unknown",
                    ClassName = classNames.Any() ? string.Join(", ", classNames) : "Все ученики",
                    QuestionsCount = questions.Count,
                    ResultsCount = results.Count,
                    CreatedAt = st.CreatedAt,
                    IsActive = st.IsActive,
                    ControllerName = "SpellingTest"
                });
            }

            // Получаем классические тесты
            var regularTestsList = await _regularTestRepository.GetAllAsync();
            foreach (var rt in regularTestsList)
            {
                var teacher = await _teacherRepository.GetByUserIdAsync(rt.TeacherId);
                var teacherName = teacher != null ? await UserManager.FindByIdAsync(rt.TeacherId) : null;
                var testClasses = await _regularTestClassRepository.GetByTestIdAsync(rt.Id);
                var questions = await _regularQuestionRepository.GetByTestIdAsync(rt.Id);
                var results = await _regularTestResultRepository.GetByTestIdAsync(rt.Id);
                
                var classNames = new List<string>();
                foreach (var tc in testClasses)
                {
                    var @class = await _classRepository.GetByIdAsync(tc.ClassId);
                    if (@class != null) classNames.Add(@class.Name);
                }

                allTests.Add(new AdminTestViewModel
                {
                    Id = rt.Id,
                    Title = rt.Title,
                    Type = "Классический",
                    TeacherName = teacherName != null ? $"{teacherName.FirstName} {teacherName.LastName}" : "Unknown",
                    ClassName = classNames.Any() ? string.Join(", ", classNames) : "Все ученики",
                    QuestionsCount = questions.Count,
                    ResultsCount = results.Count,
                    CreatedAt = rt.CreatedAt,
                    IsActive = rt.IsActive,
                    ControllerName = "RegularTest"
                });
            }

            // Получаем тесты по пунктуации
            var punctuationTestsList = await _punctuationTestRepository.GetAllAsync();
            foreach (var pt in punctuationTestsList)
            {
                var teacher = await _teacherRepository.GetByUserIdAsync(pt.TeacherId);
                var teacherName = teacher != null ? await UserManager.FindByIdAsync(pt.TeacherId) : null;
                var testClasses = await _punctuationTestClassRepository.GetByTestIdAsync(pt.Id);
                var questions = await _punctuationQuestionRepository.GetByTestIdAsync(pt.Id);
                var results = await _punctuationTestResultRepository.GetByTestIdAsync(pt.Id);
                
                var classNames = new List<string>();
                foreach (var tc in testClasses)
                {
                    var @class = await _classRepository.GetByIdAsync(tc.ClassId);
                    if (@class != null) classNames.Add(@class.Name);
                }

                allTests.Add(new AdminTestViewModel
                {
                    Id = pt.Id,
                    Title = pt.Title,
                    Type = "Пунктуация",
                    TeacherName = teacherName != null ? $"{teacherName.FirstName} {teacherName.LastName}" : "Unknown",
                    ClassName = classNames.Any() ? string.Join(", ", classNames) : "Все ученики",
                    QuestionsCount = questions.Count,
                    ResultsCount = results.Count,
                    CreatedAt = pt.CreatedAt,
                    IsActive = pt.IsActive,
                    ControllerName = "PunctuationTest"
                });
            }

            // Получаем тесты по орфоэпии
            var orthoeopyTestsList = await _orthoeopyTestRepository.GetAllAsync();
            foreach (var ot in orthoeopyTestsList)
            {
                var teacher = await _teacherRepository.GetByUserIdAsync(ot.TeacherId);
                var teacherName = teacher != null ? await UserManager.FindByIdAsync(ot.TeacherId) : null;
                var testClasses = await _orthoeopyTestClassRepository.GetByTestIdAsync(ot.Id);
                var questions = await _orthoeopyQuestionRepository.GetByTestIdAsync(ot.Id);
                var results = await _orthoeopyTestResultRepository.GetByTestIdAsync(ot.Id);
                
                var classNames = new List<string>();
                foreach (var tc in testClasses)
                {
                    var @class = await _classRepository.GetByIdAsync(tc.ClassId);
                    if (@class != null) classNames.Add(@class.Name);
                }

                allTests.Add(new AdminTestViewModel
                {
                    Id = ot.Id,
                    Title = ot.Title,
                    Type = "Орфоэпия",
                    TeacherName = teacherName != null ? $"{teacherName.FirstName} {teacherName.LastName}" : "Unknown",
                    ClassName = classNames.Any() ? string.Join(", ", classNames) : "Все ученики",
                    QuestionsCount = questions.Count,
                    ResultsCount = results.Count,
                    CreatedAt = ot.CreatedAt,
                    IsActive = ot.IsActive,
                    ControllerName = "OrthoeopyTest"
                });
            }

            allTests = allTests.OrderByDescending(t => t.CreatedAt).ToList();
            return View(allTests);
        }

        // POST: Admin/Tests/DeleteSpellingTest/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteSpellingTest(int id)
        {
            var adminId = GetAdminId();
            var test = await _spellingTestRepository.GetByIdAsync(id);

            if (test == null)
            {
                Logger.LogWarning("Администратор {AdminId} попытался удалить несуществующий тест орфографии {TestId}", adminId, id);
                return NotFound();
            }

            var testTitle = test.Title;
            var resultsCount = await _spellingTestResultRepository.GetCountByTestIdAsync(id);

            await _spellingTestRepository.DeleteAsync(id);

            Logger.LogInformation("Администратор {AdminId} удалил тест орфографии {TestId}, Название: {TestTitle}, Результатов: {ResultsCount}",
                adminId, id, testTitle, resultsCount);

            SetSuccessMessage($"Тест \"{test.Title}\" удален!");
            return RedirectToAction(nameof(Index));
        }

        // POST: Admin/Tests/DeleteRegularTest/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteRegularTest(int id)
        {
            var test = await _regularTestRepository.GetByIdAsync(id);

            if (test == null) return NotFound();

            await _regularTestRepository.DeleteAsync(id);

            SetSuccessMessage($"Тест \"{test.Title}\" удален!");
            return RedirectToAction(nameof(Index));
        }

        // POST: Admin/Tests/DeletePunctuationTest/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeletePunctuationTest(int id)
        {
            var adminId = GetAdminId();
            var test = await _punctuationTestRepository.GetByIdAsync(id);

            if (test == null)
            {
                Logger.LogWarning("Администратор {AdminId} попытался удалить несуществующий тест пунктуации {TestId}", adminId, id);
                return NotFound();
            }

            var testTitle = test.Title;
            var resultsCount = await _punctuationTestResultRepository.GetCountByTestIdAsync(id);

            await _punctuationTestRepository.DeleteAsync(id);

            Logger.LogInformation("Администратор {AdminId} удалил тест пунктуации {TestId}, Название: {TestTitle}, Результатов: {ResultsCount}",
                adminId, id, testTitle, resultsCount);

            SetSuccessMessage($"Тест \"{testTitle}\" удален!");
            return RedirectToAction(nameof(Index));
        }

        // POST: Admin/Tests/DeleteOrthoeopyTest/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteOrthoeopyTest(int id)
        {
            var adminId = GetAdminId();
            var test = await _orthoeopyTestRepository.GetByIdAsync(id);

            if (test == null)
            {
                Logger.LogWarning("Администратор {AdminId} попытался удалить несуществующий тест орфоэпии {TestId}", adminId, id);
                return NotFound();
            }

            var testTitle = test.Title;
            var resultsCount = await _orthoeopyTestResultRepository.GetCountByTestIdAsync(id);

            await _orthoeopyTestRepository.DeleteAsync(id);

            Logger.LogInformation("Администратор {AdminId} удалил тест орфоэпии {TestId}, Название: {TestTitle}, Результатов: {ResultsCount}",
                adminId, id, testTitle, resultsCount);

            SetSuccessMessage($"Тест \"{testTitle}\" удален!");
            return RedirectToAction(nameof(Index));
        }
    }
}

