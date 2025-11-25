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
    /// Контроллер для управления результатами тестов администратором
    /// </summary>
    public class AdminTestResultsController : AdminBaseController
    {
        private readonly ISpellingTestResultRepository _spellingTestResultRepository;
        private readonly IRegularTestResultRepository _regularTestResultRepository;
        private readonly IPunctuationTestResultRepository _punctuationTestResultRepository;
        private readonly IOrthoeopyTestResultRepository _orthoeopyTestResultRepository;
        private readonly ISpellingTestRepository _spellingTestRepository;
        private readonly IRegularTestRepository _regularTestRepository;
        private readonly IPunctuationTestRepository _punctuationTestRepository;
        private readonly IOrthoeopyTestRepository _orthoeopyTestRepository;
        private readonly IStudentRepository _studentRepository;

        public AdminTestResultsController(
            IDatabaseConnection db,
            UserManager<ApplicationUser> userManager,
            RoleManager<IdentityRole> roleManager,
            ILogger<AdminTestResultsController> logger,
            IAuditLogService auditLogService,
            IHttpContextAccessor httpContextAccessor,
            ISpellingTestResultRepository spellingTestResultRepository,
            IRegularTestResultRepository regularTestResultRepository,
            IPunctuationTestResultRepository punctuationTestResultRepository,
            IOrthoeopyTestResultRepository orthoeopyTestResultRepository,
            ISpellingTestRepository spellingTestRepository,
            IRegularTestRepository regularTestRepository,
            IPunctuationTestRepository punctuationTestRepository,
            IOrthoeopyTestRepository orthoeopyTestRepository,
            IStudentRepository studentRepository,
            IStatisticsRepository statisticsRepository)
            : base(db, userManager, roleManager, logger, auditLogService, httpContextAccessor, statisticsRepository)
        {
            _spellingTestResultRepository = spellingTestResultRepository;
            _regularTestResultRepository = regularTestResultRepository;
            _punctuationTestResultRepository = punctuationTestResultRepository;
            _orthoeopyTestResultRepository = orthoeopyTestResultRepository;
            _spellingTestRepository = spellingTestRepository;
            _regularTestRepository = regularTestRepository;
            _punctuationTestRepository = punctuationTestRepository;
            _orthoeopyTestRepository = orthoeopyTestRepository;
            _studentRepository = studentRepository;
        }

        // GET: Admin/TestResults
        public async Task<IActionResult> Index()
        {
            var allResults = new List<AdminTestResultViewModel>();

            // Получаем результаты тестов по орфографии
            var spellingResults = await _spellingTestResultRepository.GetAllAsync();
            foreach (var str in spellingResults)
            {
                var test = await _spellingTestRepository.GetByIdAsync(str.SpellingTestId);
                var student = await _studentRepository.GetWithUserAsync(str.StudentId);
                var user = student != null ? await UserManager.FindByIdAsync(student.UserId) : null;

                allResults.Add(new AdminTestResultViewModel
                {
                    Id = str.Id,
                    TestTitle = test?.Title ?? "Unknown",
                    TestType = "Орфография",
                    StudentName = user != null ? $"{user.FirstName} {user.LastName}" : "Unknown",
                    Score = str.Score,
                    MaxScore = str.MaxScore,
                    Percentage = str.Percentage,
                    StartedAt = str.StartedAt,
                    CompletedAt = str.CompletedAt,
                    IsCompleted = str.IsCompleted,
                    ResultType = "Spelling"
                });
            }

            // Получаем результаты классических тестов
            var regularResults = await _regularTestResultRepository.GetAllAsync();
            foreach (var tr in regularResults)
            {
                var test = await _regularTestRepository.GetByIdAsync(tr.RegularTestId);
                var student = await _studentRepository.GetWithUserAsync(tr.StudentId);
                var user = student != null ? await UserManager.FindByIdAsync(student.UserId) : null;

                allResults.Add(new AdminTestResultViewModel
                {
                    Id = tr.Id,
                    TestTitle = test?.Title ?? "Unknown",
                    TestType = "Классический",
                    StudentName = user != null ? $"{user.FirstName} {user.LastName}" : "Unknown",
                    Score = tr.Score,
                    MaxScore = tr.MaxScore,
                    Percentage = tr.Percentage,
                    StartedAt = tr.StartedAt,
                    CompletedAt = tr.CompletedAt,
                    IsCompleted = tr.IsCompleted,
                    ResultType = "Regular"
                });
            }

            // Получаем результаты тестов по пунктуации
            var punctuationResults = await _punctuationTestResultRepository.GetAllAsync();
            foreach (var tr in punctuationResults)
            {
                var test = await _punctuationTestRepository.GetByIdAsync(tr.PunctuationTestId);
                var student = await _studentRepository.GetWithUserAsync(tr.StudentId);
                var user = student != null ? await UserManager.FindByIdAsync(student.UserId) : null;

                allResults.Add(new AdminTestResultViewModel
                {
                    Id = tr.Id,
                    TestTitle = test?.Title ?? "Unknown",
                    TestType = "Пунктуация",
                    StudentName = user != null ? $"{user.FirstName} {user.LastName}" : "Unknown",
                    Score = tr.Score,
                    MaxScore = tr.MaxScore,
                    Percentage = tr.Percentage,
                    StartedAt = tr.StartedAt,
                    CompletedAt = tr.CompletedAt,
                    IsCompleted = tr.IsCompleted,
                    ResultType = "Punctuation"
                });
            }

            // Получаем результаты тестов по орфоэпии
            var orthoeopyResults = await _orthoeopyTestResultRepository.GetAllAsync();
            foreach (var tr in orthoeopyResults)
            {
                var test = await _orthoeopyTestRepository.GetByIdAsync(tr.OrthoeopyTestId);
                var student = await _studentRepository.GetWithUserAsync(tr.StudentId);
                var user = student != null ? await UserManager.FindByIdAsync(student.UserId) : null;

                allResults.Add(new AdminTestResultViewModel
                {
                    Id = tr.Id,
                    TestTitle = test?.Title ?? "Unknown",
                    TestType = "Орфоэпия",
                    StudentName = user != null ? $"{user.FirstName} {user.LastName}" : "Unknown",
                    Score = tr.Score,
                    MaxScore = tr.MaxScore,
                    Percentage = tr.Percentage,
                    StartedAt = tr.StartedAt,
                    CompletedAt = tr.CompletedAt,
                    IsCompleted = tr.IsCompleted,
                    ResultType = "Orthoeopy"
                });
            }

            allResults = allResults.OrderByDescending(r => r.StartedAt).ToList();
            return View(allResults);
        }

        // POST: Admin/TestResults/DeleteSpellingResult/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteSpellingResult(int id)
        {
            var adminId = GetAdminId();
            var result = await _spellingTestResultRepository.GetByIdAsync(id);
            if (result == null)
            {
                Logger.LogWarning("Администратор {AdminId} попытался удалить несуществующий результат теста по орфографии {ResultId}", adminId, id);
                return NotFound();
            }

            await _spellingTestResultRepository.DeleteAsync(id);

            Logger.LogInformation("Администратор {AdminId} удалил результат теста по орфографии {ResultId}, TestId: {TestId}, StudentId: {StudentId}",
                adminId, id, result.SpellingTestId, result.StudentId);

            SetSuccessMessage("Результат теста удален!");
            return RedirectToAction(nameof(Index));
        }

        // POST: Admin/TestResults/DeleteRegularResult/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteRegularResult(int id)
        {
            var adminId = GetAdminId();
            var result = await _regularTestResultRepository.GetByIdAsync(id);
            if (result == null)
            {
                Logger.LogWarning("Администратор {AdminId} попытался удалить несуществующий результат классического теста {ResultId}", adminId, id);
                return NotFound();
            }

            await _regularTestResultRepository.DeleteAsync(id);

            Logger.LogInformation("Администратор {AdminId} удалил результат классического теста {ResultId}", adminId, id);

            SetSuccessMessage("Результат теста удален!");
            return RedirectToAction(nameof(Index));
        }

        // POST: Admin/TestResults/DeletePunctuationResult/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeletePunctuationResult(int id)
        {
            var adminId = GetAdminId();
            var result = await _punctuationTestResultRepository.GetByIdAsync(id);
            if (result == null)
            {
                Logger.LogWarning("Администратор {AdminId} попытался удалить несуществующий результат теста по пунктуации {ResultId}", adminId, id);
                return NotFound();
            }

            await _punctuationTestResultRepository.DeleteAsync(id);

            Logger.LogInformation("Администратор {AdminId} удалил результат теста по пунктуации {ResultId}", adminId, id);

            SetSuccessMessage("Результат теста удален!");
            return RedirectToAction(nameof(Index));
        }

        // POST: Admin/TestResults/DeleteOrthoeopyResult/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteOrthoeopyResult(int id)
        {
            var adminId = GetAdminId();
            var result = await _orthoeopyTestResultRepository.GetByIdAsync(id);
            if (result == null)
            {
                Logger.LogWarning("Администратор {AdminId} попытался удалить несуществующий результат теста по орфоэпии {ResultId}", adminId, id);
                return NotFound();
            }

            await _orthoeopyTestResultRepository.DeleteAsync(id);

            Logger.LogInformation("Администратор {AdminId} удалил результат теста по орфоэпии {ResultId}", adminId, id);

            SetSuccessMessage("Результат теста удален!");
            return RedirectToAction(nameof(Index));
        }

        // POST: Admin/TestResults/ClearAll
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ClearAll()
        {
            var adminId = GetAdminId();
            var adminName = GetAdminName();
            Logger.LogWarning("Администратор {AdminId} начал очистку всех результатов тестов", adminId);

            var spellingResults = await _spellingTestResultRepository.GetAllAsync();
            var regularResults = await _regularTestResultRepository.GetAllAsync();
            var punctuationResults = await _punctuationTestResultRepository.GetAllAsync();
            var orthoeopyResults = await _orthoeopyTestResultRepository.GetAllAsync();

            var totalCount = spellingResults.Count + regularResults.Count + punctuationResults.Count + orthoeopyResults.Count;

            // Удаляем все результаты через SQL
            await Db.ExecuteAsync("DELETE FROM SpellingTestResults");
            await Db.ExecuteAsync("DELETE FROM RegularTestResults");
            await Db.ExecuteAsync("DELETE FROM PunctuationTestResults");
            await Db.ExecuteAsync("DELETE FROM OrthoeopyTestResults");

            await LogAdminActionAsync(
                AuditActions.AllResultsCleared,
                AuditEntityTypes.System,
                null,
                $"Удалены все результаты тестов (Всего: {totalCount})"
            );

            Logger.LogWarning("Администратор {AdminId} удалил все результаты тестов. Всего удалено: {TotalCount} (Орфография: {SpellingCount}, " +
                "Классические: {RegularCount}, Пунктуация: {PunctuationCount}, Орфоэпия: {OrthoeopyCount})",
                adminId, totalCount, spellingResults.Count, regularResults.Count, punctuationResults.Count, orthoeopyResults.Count);

            SetSuccessMessage($"Удалено {totalCount} результатов тестов!");

            return RedirectToAction(nameof(Index));
        }
    }
}

