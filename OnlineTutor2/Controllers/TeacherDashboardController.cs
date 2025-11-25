using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using OnlineTutor2.Data.Repositories;
using OnlineTutor2.Models;
using OnlineTutor2.ViewModels;

namespace OnlineTutor2.Controllers
{
    [Authorize(Roles = ApplicationRoles.Teacher)]
    public class TeacherDashboardController : Controller
    {
        private readonly ISpellingTestRepository _spellingTestRepository;
        private readonly IPunctuationTestRepository _punctuationTestRepository;
        private readonly IOrthoeopyTestRepository _orthoeopyTestRepository;
        private readonly IRegularTestRepository _regularTestRepository;
        private readonly ISpellingTestResultRepository _spellingResultRepository;
        private readonly IPunctuationTestResultRepository _punctuationResultRepository;
        private readonly IOrthoeopyTestResultRepository _orthoeopyResultRepository;
        private readonly IRegularTestResultRepository _regularResultRepository;
        private readonly ISpellingQuestionRepository _spellingQuestionRepository;
        private readonly IPunctuationQuestionRepository _punctuationQuestionRepository;
        private readonly IOrthoeopyQuestionRepository _orthoeopyQuestionRepository;
        private readonly IRegularQuestionRepository _regularQuestionRepository;
        private readonly ISpellingAnswerRepository _spellingAnswerRepository;
        private readonly IPunctuationAnswerRepository _punctuationAnswerRepository;
        private readonly IOrthoeopyAnswerRepository _orthoeopyAnswerRepository;
        private readonly IRegularAnswerRepository _regularAnswerRepository;
        private readonly IRegularQuestionOptionRepository _regularOptionRepository;
        private readonly IStudentRepository _studentRepository;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly ILogger<TeacherDashboardController> _logger;

        public TeacherDashboardController(
            ISpellingTestRepository spellingTestRepository,
            IPunctuationTestRepository punctuationTestRepository,
            IOrthoeopyTestRepository orthoeopyTestRepository,
            IRegularTestRepository regularTestRepository,
            ISpellingTestResultRepository spellingResultRepository,
            IPunctuationTestResultRepository punctuationResultRepository,
            IOrthoeopyTestResultRepository orthoeopyResultRepository,
            IRegularTestResultRepository regularResultRepository,
            ISpellingQuestionRepository spellingQuestionRepository,
            IPunctuationQuestionRepository punctuationQuestionRepository,
            IOrthoeopyQuestionRepository orthoeopyQuestionRepository,
            IRegularQuestionRepository regularQuestionRepository,
            ISpellingAnswerRepository spellingAnswerRepository,
            IPunctuationAnswerRepository punctuationAnswerRepository,
            IOrthoeopyAnswerRepository orthoeopyAnswerRepository,
            IRegularAnswerRepository regularAnswerRepository,
            IRegularQuestionOptionRepository regularOptionRepository,
            IStudentRepository studentRepository,
            UserManager<ApplicationUser> userManager,
            ILogger<TeacherDashboardController> logger)
        {
            _spellingTestRepository = spellingTestRepository;
            _punctuationTestRepository = punctuationTestRepository;
            _orthoeopyTestRepository = orthoeopyTestRepository;
            _regularTestRepository = regularTestRepository;
            _spellingResultRepository = spellingResultRepository;
            _punctuationResultRepository = punctuationResultRepository;
            _orthoeopyResultRepository = orthoeopyResultRepository;
            _regularResultRepository = regularResultRepository;
            _spellingQuestionRepository = spellingQuestionRepository;
            _punctuationQuestionRepository = punctuationQuestionRepository;
            _orthoeopyQuestionRepository = orthoeopyQuestionRepository;
            _regularQuestionRepository = regularQuestionRepository;
            _spellingAnswerRepository = spellingAnswerRepository;
            _punctuationAnswerRepository = punctuationAnswerRepository;
            _orthoeopyAnswerRepository = orthoeopyAnswerRepository;
            _regularAnswerRepository = regularAnswerRepository;
            _regularOptionRepository = regularOptionRepository;
            _studentRepository = studentRepository;
            _userManager = userManager;
            _logger = logger;
        }

        public async Task<IActionResult> Index()
        {
            var currentUser = await _userManager.GetUserAsync(User);
            if (currentUser == null) return Unauthorized();

            // Получаем тесты через репозитории
            var spellingTests = await _spellingTestRepository.GetRecentActiveByTeacherIdAsync(currentUser.Id, 20);
            var punctuationTests = await _punctuationTestRepository.GetRecentActiveByTeacherIdAsync(currentUser.Id, 20);
            var orthoeopyTests = await _orthoeopyTestRepository.GetRecentActiveByTeacherIdAsync(currentUser.Id, 20);
            var regularTests = await _regularTestRepository.GetRecentActiveByTeacherIdAsync(currentUser.Id, 20);

            var viewModel = new TeacherDashboardViewModel
            {
                Teacher = currentUser,
                SpellingTests = spellingTests,
                PunctuationTests = punctuationTests,
                OrthoeopyTests = orthoeopyTests,
                RegularTests = regularTests
            };

            return View(viewModel);
        }

        [HttpGet]
        public async Task<IActionResult> GetRecentActivity()
        {
            var currentUser = await _userManager.GetUserAsync(User);
            if (currentUser == null) return Unauthorized();
            
            var activities = new List<TestActivityViewModel>();

            // Получаем результаты тестов по орфографии
            var spellingResults = await _spellingResultRepository.GetByTeacherIdWithDetailsAsync(currentUser.Id);
            var spellingTests = await _spellingTestRepository.GetByTeacherIdAsync(currentUser.Id);
            var spellingTestsDict = spellingTests.ToDictionary(t => t.Id);
            
            foreach (var result in spellingResults)
            {
                var test = spellingTestsDict.GetValueOrDefault(result.SpellingTestId);
                var student = await _studentRepository.GetWithUserAsync(result.StudentId);
                
                if (test != null && student?.User != null)
                {
                    activities.Add(new TestActivityViewModel
                    {
                        TestId = result.SpellingTestId,
                        TestTitle = test.Title,
                        TestType = "spelling",
                        StudentId = result.StudentId,
                        StudentName = student.User.FullName,
                        Status = result.IsCompleted ? "completed" : "in_progress",
                        Score = result.Score,
                        MaxScore = result.MaxScore,
                        Percentage = result.Percentage,
                        StartedAt = result.StartedAt,
                        CompletedAt = result.CompletedAt,
                        LastActivityAt = result.CompletedAt ?? result.StartedAt,
                        TestResultId = result.Id
                    });
                }
            }

            // Получаем результаты тестов по пунктуации
            var punctuationResults = await _punctuationResultRepository.GetByTeacherIdWithDetailsAsync(currentUser.Id);
            var punctuationTests = await _punctuationTestRepository.GetByTeacherIdAsync(currentUser.Id);
            var punctuationTestsDict = punctuationTests.ToDictionary(t => t.Id);
            
            foreach (var result in punctuationResults)
            {
                var test = punctuationTestsDict.GetValueOrDefault(result.PunctuationTestId);
                var student = await _studentRepository.GetWithUserAsync(result.StudentId);
                
                if (test != null && student?.User != null)
                {
                    activities.Add(new TestActivityViewModel
                    {
                        TestId = result.PunctuationTestId,
                        TestTitle = test.Title,
                        TestType = "punctuation",
                        StudentId = result.StudentId,
                        StudentName = student.User.FullName,
                        Status = result.IsCompleted ? "completed" : "in_progress",
                        Score = result.Score,
                        MaxScore = result.MaxScore,
                        Percentage = result.Percentage,
                        StartedAt = result.StartedAt,
                        CompletedAt = result.CompletedAt,
                        LastActivityAt = result.CompletedAt ?? result.StartedAt,
                        TestResultId = result.Id
                    });
                }
            }

            // Получаем результаты тестов по орфоэпии
            var orthoeopyResults = await _orthoeopyResultRepository.GetByTeacherIdWithDetailsAsync(currentUser.Id);
            var orthoeopyTests = await _orthoeopyTestRepository.GetByTeacherIdAsync(currentUser.Id);
            var orthoeopyTestsDict = orthoeopyTests.ToDictionary(t => t.Id);
            
            foreach (var result in orthoeopyResults)
            {
                var test = orthoeopyTestsDict.GetValueOrDefault(result.OrthoeopyTestId);
                var student = await _studentRepository.GetWithUserAsync(result.StudentId);
                
                if (test != null && student?.User != null)
                {
                    activities.Add(new TestActivityViewModel
                    {
                        TestId = result.OrthoeopyTestId,
                        TestTitle = test.Title,
                        TestType = "orthoepy",
                        StudentId = result.StudentId,
                        StudentName = student.User.FullName,
                        Status = result.IsCompleted ? "completed" : "in_progress",
                        Score = result.Score,
                        MaxScore = result.MaxScore,
                        Percentage = result.Percentage,
                        StartedAt = result.StartedAt,
                        CompletedAt = result.CompletedAt,
                        LastActivityAt = result.CompletedAt ?? result.StartedAt,
                        TestResultId = result.Id
                    });
                }
            }

            // Получаем результаты классических тестов
            var regularResults = await _regularResultRepository.GetByTeacherIdWithDetailsAsync(currentUser.Id);
            var regularTests = await _regularTestRepository.GetByTeacherIdAsync(currentUser.Id);
            var regularTestsDict = regularTests.ToDictionary(t => t.Id);
            
            foreach (var result in regularResults)
            {
                var test = regularTestsDict.GetValueOrDefault(result.RegularTestId);
                var student = await _studentRepository.GetWithUserAsync(result.StudentId);
                
                if (test != null && student?.User != null)
                {
                    activities.Add(new TestActivityViewModel
                    {
                        TestId = result.RegularTestId,
                        TestTitle = test.Title,
                        TestType = "regular",
                        StudentId = result.StudentId,
                        StudentName = student.User.FullName,
                        Status = result.IsCompleted ? "completed" : "in_progress",
                        Score = result.Score,
                        MaxScore = result.MaxScore,
                        Percentage = result.Percentage,
                        StartedAt = result.StartedAt,
                        CompletedAt = result.CompletedAt,
                        LastActivityAt = result.CompletedAt ?? result.StartedAt,
                        TestResultId = result.Id
                    });
                }
            }

            // Сортируем в памяти (данные уже загружены)
            activities.Sort((a, b) => b.LastActivityAt.CompareTo(a.LastActivityAt));
            var sortedActivities = activities.Take(50).ToList();

            return Json(sortedActivities);
        }

        // GET: TeacherDashboard/GetTestResult
        [HttpGet]
        public async Task<IActionResult> GetTestResult(string testType, int testResultId)
        {
            try
            {
                var currentUser = await _userManager.GetUserAsync(User);
                
                if (currentUser == null)
                {
                    _logger.LogWarning("Попытка загрузки результата теста неавторизованным пользователем");
                    return Unauthorized();
                }

                if (string.IsNullOrEmpty(testType))
                {
                    _logger.LogWarning("Не указан тип теста при загрузке результата");
                    return BadRequest("Тип теста не указан");
                }

                object? result = null;

                switch (testType.ToLower())
                {
                    case "spelling":
                        var spellingResult = await _spellingResultRepository.GetByIdWithDetailsAsync(testResultId, currentUser.Id);
                        if (spellingResult != null)
                        {
                            var spellingTest = await _spellingTestRepository.GetByIdAsync(spellingResult.SpellingTestId);
                            var student = await _studentRepository.GetWithUserAsync(spellingResult.StudentId);
                            var questions = await _spellingQuestionRepository.GetByTestIdOrderedAsync(spellingResult.SpellingTestId);
                            var answers = await _spellingAnswerRepository.GetByTestResultIdAsync(testResultId);
                            
                            // Устанавливаем навигационные свойства вручную
                            // TODO: Это временное решение, можно улучшить через DTO или специальный маппер
                            result = spellingResult;
                        }
                        break;

                    case "punctuation":
                        var punctuationResult = await _punctuationResultRepository.GetByIdWithDetailsAsync(testResultId, currentUser.Id);
                        if (punctuationResult != null)
                        {
                            var punctuationTest = await _punctuationTestRepository.GetByIdAsync(punctuationResult.PunctuationTestId);
                            var student2 = await _studentRepository.GetWithUserAsync(punctuationResult.StudentId);
                            var questions2 = await _punctuationQuestionRepository.GetByTestIdOrderedAsync(punctuationResult.PunctuationTestId);
                            var answers2 = await _punctuationAnswerRepository.GetByTestResultIdAsync(testResultId);
                            
                            result = punctuationResult;
                        }
                        break;

                    case "orthoepy":
                        var orthoeopyResult = await _orthoeopyResultRepository.GetByIdWithDetailsAsync(testResultId, currentUser.Id);
                        if (orthoeopyResult != null)
                        {
                            var orthoeopyTest = await _orthoeopyTestRepository.GetByIdAsync(orthoeopyResult.OrthoeopyTestId);
                            var student3 = await _studentRepository.GetWithUserAsync(orthoeopyResult.StudentId);
                            var questions3 = await _orthoeopyQuestionRepository.GetByTestIdOrderedAsync(orthoeopyResult.OrthoeopyTestId);
                            var answers3 = await _orthoeopyAnswerRepository.GetByTestResultIdAsync(testResultId);
                            
                            result = orthoeopyResult;
                        }
                        break;

                    case "regular":
                        var regularResult = await _regularResultRepository.GetByIdWithDetailsAsync(testResultId, currentUser.Id);
                        if (regularResult != null)
                        {
                            var regularTest = await _regularTestRepository.GetByIdAsync(regularResult.RegularTestId);
                            var student4 = await _studentRepository.GetWithUserAsync(regularResult.StudentId);
                            var questions4 = await _regularQuestionRepository.GetByTestIdOrderedAsync(regularResult.RegularTestId);
                            var answers4 = await _regularAnswerRepository.GetByTestResultIdAsync(testResultId);
                            
                            // Загружаем опции для каждого вопроса
                            foreach (var question in questions4)
                            {
                                var options = await _regularOptionRepository.GetByQuestionIdOrderedAsync(question.Id);
                                // TODO: Установить навигационное свойство Options
                            }
                            
                            result = regularResult;
                        }
                        break;

                    default:
                        _logger.LogWarning("Неизвестный тип теста: {TestType}", testType);
                        return BadRequest($"Неизвестный тип теста: {testType}");
                }

                if (result == null)
                {
                    _logger.LogWarning("Результат теста не найден: TestType={TestType}, ResultId={ResultId}, TeacherId={TeacherId}", 
                        testType, testResultId, currentUser.Id);
                    return NotFound();
                }

                // Передаем флаг, что это модальное окно, чтобы скрыть навигацию
                ViewBag.IsModal = true;
                
                // Возвращаем частичное представление
                return PartialView("~/Views/StudentTest/Result.cshtml", result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Ошибка загрузки результата теста {TestType} {ResultId}", testType, testResultId);
                return StatusCode(500, "Ошибка загрузки результата");
            }
        }
    }
}
