using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using OnlineTutor2.Data;
using OnlineTutor2.Data.Repositories;
using OnlineTutor2.Hubs;
using OnlineTutor2.Models;
using OnlineTutor2.ViewModels;

namespace OnlineTutor2.Controllers
{
    [Authorize(Roles = ApplicationRoles.Student)]
    public class StudentTestController : Controller
    {
        private readonly IStudentRepository _studentRepository;
        private readonly ISpellingTestRepository _spellingTestRepository;
        private readonly IPunctuationTestRepository _punctuationTestRepository;
        private readonly IOrthoeopyTestRepository _orthoeopyTestRepository;
        private readonly IRegularTestRepository _regularTestRepository;
        private readonly ISpellingTestClassRepository _spellingTestClassRepository;
        private readonly IPunctuationTestClassRepository _punctuationTestClassRepository;
        private readonly IOrthoeopyTestClassRepository _orthoeopyTestClassRepository;
        private readonly IRegularTestClassRepository _regularTestClassRepository;
        private readonly ISpellingQuestionRepository _spellingQuestionRepository;
        private readonly IPunctuationQuestionRepository _punctuationQuestionRepository;
        private readonly IOrthoeopyQuestionRepository _orthoeopyQuestionRepository;
        private readonly IRegularQuestionRepository _regularQuestionRepository;
        private readonly ISpellingTestResultRepository _spellingTestResultRepository;
        private readonly IPunctuationTestResultRepository _punctuationTestResultRepository;
        private readonly IOrthoeopyTestResultRepository _orthoeopyTestResultRepository;
        private readonly IRegularTestResultRepository _regularTestResultRepository;
        private readonly ISpellingAnswerRepository _spellingAnswerRepository;
        private readonly IPunctuationAnswerRepository _punctuationAnswerRepository;
        private readonly IOrthoeopyAnswerRepository _orthoeopyAnswerRepository;
        private readonly IRegularAnswerRepository _regularAnswerRepository;
        private readonly IRegularQuestionOptionRepository _regularQuestionOptionRepository;
        private readonly IClassRepository _classRepository;
        private readonly IDatabaseConnection _db;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly ILogger<StudentTestController> _logger;
        private readonly IHubContext<TestAnalyticsHub> _hubContext;

        public StudentTestController(
            IStudentRepository studentRepository,
            ISpellingTestRepository spellingTestRepository,
            IPunctuationTestRepository punctuationTestRepository,
            IOrthoeopyTestRepository orthoeopyTestRepository,
            IRegularTestRepository regularTestRepository,
            ISpellingTestClassRepository spellingTestClassRepository,
            IPunctuationTestClassRepository punctuationTestClassRepository,
            IOrthoeopyTestClassRepository orthoeopyTestClassRepository,
            IRegularTestClassRepository regularTestClassRepository,
            ISpellingQuestionRepository spellingQuestionRepository,
            IPunctuationQuestionRepository punctuationQuestionRepository,
            IOrthoeopyQuestionRepository orthoeopyQuestionRepository,
            IRegularQuestionRepository regularQuestionRepository,
            ISpellingTestResultRepository spellingTestResultRepository,
            IPunctuationTestResultRepository punctuationTestResultRepository,
            IOrthoeopyTestResultRepository orthoeopyTestResultRepository,
            IRegularTestResultRepository regularTestResultRepository,
            ISpellingAnswerRepository spellingAnswerRepository,
            IPunctuationAnswerRepository punctuationAnswerRepository,
            IOrthoeopyAnswerRepository orthoeopyAnswerRepository,
            IRegularAnswerRepository regularAnswerRepository,
            IRegularQuestionOptionRepository regularQuestionOptionRepository,
            IClassRepository classRepository,
            IDatabaseConnection db,
            UserManager<ApplicationUser> userManager,
            ILogger<StudentTestController> logger,
            IHubContext<TestAnalyticsHub> hubContext)
        {
            _studentRepository = studentRepository;
            _spellingTestRepository = spellingTestRepository;
            _punctuationTestRepository = punctuationTestRepository;
            _orthoeopyTestRepository = orthoeopyTestRepository;
            _regularTestRepository = regularTestRepository;
            _spellingTestClassRepository = spellingTestClassRepository;
            _punctuationTestClassRepository = punctuationTestClassRepository;
            _orthoeopyTestClassRepository = orthoeopyTestClassRepository;
            _regularTestClassRepository = regularTestClassRepository;
            _spellingQuestionRepository = spellingQuestionRepository;
            _punctuationQuestionRepository = punctuationQuestionRepository;
            _orthoeopyQuestionRepository = orthoeopyQuestionRepository;
            _regularQuestionRepository = regularQuestionRepository;
            _spellingTestResultRepository = spellingTestResultRepository;
            _punctuationTestResultRepository = punctuationTestResultRepository;
            _orthoeopyTestResultRepository = orthoeopyTestResultRepository;
            _regularTestResultRepository = regularTestResultRepository;
            _spellingAnswerRepository = spellingAnswerRepository;
            _punctuationAnswerRepository = punctuationAnswerRepository;
            _orthoeopyAnswerRepository = orthoeopyAnswerRepository;
            _regularAnswerRepository = regularAnswerRepository;
            _regularQuestionOptionRepository = regularQuestionOptionRepository;
            _classRepository = classRepository;
            _db = db;
            _userManager = userManager;
            _logger = logger;
            _hubContext = hubContext;
        }

        // GET: StudentTest - Список всех доступных тестов
        public async Task<IActionResult> Index(string? category = null)
        {
            var currentUser = await _userManager.GetUserAsync(User);
            var student = await _studentRepository.GetByUserIdAsync(currentUser.Id);
            if (student != null && student.ClassId.HasValue)
            {
                student.Class = await _classRepository.GetByIdAsync(student.ClassId.Value);
            }

            if (student == null)
            {
                _logger.LogWarning("Профиль студента не найден для пользователя {UserId}", currentUser.Id);
                TempData["ErrorMessage"] = "Профиль ученика не найден.";
                return RedirectToAction("Index", "Student");
            }

            var viewModel = new StudentAllTestsIndexViewModel
            {
                Student = student
            };

            // ✅ ИСПРАВЛЕНО: Получаем тесты по орфографии через SQL
            if (category == null || category == "spelling")
            {
                var now = DateTime.Now;
                var teacherId = student.Class?.TeacherId ?? "";
                
                // Получаем активные тесты с фильтрацией по датам через SQL
                var spellingTestsSql = @"
                    SELECT * FROM SpellingTests 
                    WHERE IsActive = 1 AND TeacherId = @TeacherId
                    AND (StartDate IS NULL OR StartDate <= @Now)
                    AND (EndDate IS NULL OR EndDate >= @Now)
                    ORDER BY Title";
                var allSpellingTests = await _db.QueryAsync<SpellingTest>(spellingTestsSql, new { TeacherId = teacherId, Now = now });
                
                // Загружаем связанные данные и фильтруем по классу
                var filteredSpellingTests = new List<SpellingTest>();
                foreach (var test in allSpellingTests)
                {
                    test.TestClasses = await _spellingTestClassRepository.GetByTestIdAsync(test.Id);
                    foreach (var testClass in test.TestClasses)
                    {
                        testClass.Class = await _classRepository.GetByIdAsync(testClass.ClassId);
                    }
                    
                    // Фильтруем по классу студента через SQL проверку
                    var testClassesCount = test.TestClasses.Count;
                    if (testClassesCount > 0)
                    {
                        if (!student.ClassId.HasValue)
                        {
                            continue;
                        }
                        
                        // Проверяем через SQL, есть ли связь теста с классом студента
                        var hasClassLinkSql = "SELECT COUNT(*) FROM SpellingTestClasses WHERE SpellingTestId = @TestId AND ClassId = @ClassId";
                        var hasClassLink = await _db.QueryScalarAsync<int>(hasClassLinkSql, new { TestId = test.Id, ClassId = student.ClassId.Value });
                        if (hasClassLink == 0)
                        {
                            continue;
                        }
                    }
                    
                    test.SpellingQuestions = await _spellingQuestionRepository.GetByTestIdOrderedAsync(test.Id);
                    
                    // Получаем результаты теста для студента через SQL
                    var resultsSql = "SELECT * FROM SpellingTestResults WHERE StudentId = @StudentId AND SpellingTestId = @TestId";
                    test.SpellingTestResults = await _db.QueryAsync<SpellingTestResult>(resultsSql, new { StudentId = student.Id, TestId = test.Id });
                    
                    filteredSpellingTests.Add(test);
                }
                
                // Сортируем по названию (уже отсортировано в SQL, но для надежности)
                filteredSpellingTests.Sort((a, b) => string.Compare(a.Title, b.Title, StringComparison.Ordinal));
                viewModel.SpellingTests = filteredSpellingTests;
            }

            // ✅ ИСПРАВЛЕНО: Получаем тесты по пунктуации через SQL
            if (category == null || category == "punctuation")
            {
                var now = DateTime.Now;
                var teacherId = student.Class?.TeacherId ?? "";
                
                // Получаем активные тесты с фильтрацией по датам через SQL
                var punctuationTestsSql = @"
                    SELECT * FROM PunctuationTests 
                    WHERE IsActive = 1 AND TeacherId = @TeacherId
                    AND (StartDate IS NULL OR StartDate <= @Now)
                    AND (EndDate IS NULL OR EndDate >= @Now)
                    ORDER BY Title";
                var allPunctuationTests = await _db.QueryAsync<PunctuationTest>(punctuationTestsSql, new { TeacherId = teacherId, Now = now });
                
                // Загружаем связанные данные и фильтруем по классу
                var filteredPunctuationTests = new List<PunctuationTest>();
                foreach (var test in allPunctuationTests)
                {
                    test.TestClasses = await _punctuationTestClassRepository.GetByTestIdAsync(test.Id);
                    foreach (var testClass in test.TestClasses)
                    {
                        testClass.Class = await _classRepository.GetByIdAsync(testClass.ClassId);
                    }
                    
                    // Фильтруем по классу студента через SQL проверку
                    var testClassesCount = test.TestClasses.Count;
                    if (testClassesCount > 0)
                    {
                        if (!student.ClassId.HasValue)
                        {
                            continue;
                        }
                        
                        // Проверяем через SQL, есть ли связь теста с классом студента
                        var hasClassLinkSql = "SELECT COUNT(*) FROM PunctuationTestClasses WHERE PunctuationTestId = @TestId AND ClassId = @ClassId";
                        var hasClassLink = await _db.QueryScalarAsync<int>(hasClassLinkSql, new { TestId = test.Id, ClassId = student.ClassId.Value });
                        if (hasClassLink == 0)
                        {
                            continue;
                        }
                    }
                    
                    test.PunctuationQuestions = await _punctuationQuestionRepository.GetByTestIdOrderedAsync(test.Id);
                    
                    // Получаем результаты теста для студента через SQL
                    var resultsSql = "SELECT * FROM PunctuationTestResults WHERE StudentId = @StudentId AND PunctuationTestId = @TestId";
                    test.PunctuationTestResults = await _db.QueryAsync<PunctuationTestResult>(resultsSql, new { StudentId = student.Id, TestId = test.Id });
                    
                    filteredPunctuationTests.Add(test);
                }
                
                // Сортируем по названию
                filteredPunctuationTests.Sort((a, b) => string.Compare(a.Title, b.Title, StringComparison.Ordinal));
                viewModel.PunctuationTests = filteredPunctuationTests;
            }

            // ✅ ИСПРАВЛЕНО: Получаем тесты по орфоэпии через SQL
            if (category == null || category == "orthoepy")
            {
                var now = DateTime.Now;
                var teacherId = student.Class?.TeacherId ?? "";
                
                // Получаем активные тесты с фильтрацией по датам через SQL
                var orthoeopyTestsSql = @"
                    SELECT * FROM OrthoeopyTests 
                    WHERE IsActive = 1 AND TeacherId = @TeacherId
                    AND (StartDate IS NULL OR StartDate <= @Now)
                    AND (EndDate IS NULL OR EndDate >= @Now)
                    ORDER BY Title";
                var allOrthoeopyTests = await _db.QueryAsync<OrthoeopyTest>(orthoeopyTestsSql, new { TeacherId = teacherId, Now = now });
                
                // Загружаем связанные данные и фильтруем по классу
                var filteredOrthoeopyTests = new List<OrthoeopyTest>();
                foreach (var test in allOrthoeopyTests)
                {
                    test.TestClasses = await _orthoeopyTestClassRepository.GetByTestIdAsync(test.Id);
                    foreach (var testClass in test.TestClasses)
                    {
                        testClass.Class = await _classRepository.GetByIdAsync(testClass.ClassId);
                    }
                    
                    // Фильтруем по классу студента через SQL проверку
                    var testClassesCount = test.TestClasses.Count;
                    if (testClassesCount > 0)
                    {
                        if (!student.ClassId.HasValue)
                        {
                            continue;
                        }
                        
                        // Проверяем через SQL, есть ли связь теста с классом студента
                        var hasClassLinkSql = "SELECT COUNT(*) FROM OrthoeopyTestClasses WHERE OrthoeopyTestId = @TestId AND ClassId = @ClassId";
                        var hasClassLink = await _db.QueryScalarAsync<int>(hasClassLinkSql, new { TestId = test.Id, ClassId = student.ClassId.Value });
                        if (hasClassLink == 0)
                        {
                            continue;
                        }
                    }
                    
                    test.OrthoeopyQuestions = await _orthoeopyQuestionRepository.GetByTestIdOrderedAsync(test.Id);
                    
                    // Получаем результаты теста для студента через SQL
                    var resultsSql = "SELECT * FROM OrthoeopyTestResults WHERE StudentId = @StudentId AND OrthoeopyTestId = @TestId";
                    test.OrthoeopyTestResults = await _db.QueryAsync<OrthoeopyTestResult>(resultsSql, new { StudentId = student.Id, TestId = test.Id });
                    
                    filteredOrthoeopyTests.Add(test);
                }
                
                // Сортируем по названию
                filteredOrthoeopyTests.Sort((a, b) => string.Compare(a.Title, b.Title, StringComparison.Ordinal));
                viewModel.OrthoeopyTests = filteredOrthoeopyTests;
            }

            // ✅ ИСПРАВЛЕНО: Получаем тесты классические через SQL
            if (category == null || category == "regular")
            {
                var now = DateTime.Now;
                var teacherId = student.Class?.TeacherId ?? "";
                
                // Получаем активные тесты с фильтрацией по датам через SQL
                var regularTestsSql = @"
                    SELECT * FROM RegularTests 
                    WHERE IsActive = 1 AND TeacherId = @TeacherId
                    AND (StartDate IS NULL OR StartDate <= @Now)
                    AND (EndDate IS NULL OR EndDate >= @Now)
                    ORDER BY Title";
                var allRegularTests = await _db.QueryAsync<RegularTest>(regularTestsSql, new { TeacherId = teacherId, Now = now });
                
                // Загружаем связанные данные и фильтруем по классу
                var filteredRegularTests = new List<RegularTest>();
                foreach (var test in allRegularTests)
                {
                    test.TestClasses = await _regularTestClassRepository.GetByTestIdAsync(test.Id);
                    foreach (var testClass in test.TestClasses)
                    {
                        testClass.Class = await _classRepository.GetByIdAsync(testClass.ClassId);
                    }
                    
                    // Фильтруем по классу студента через SQL проверку
                    var testClassesCount = test.TestClasses.Count;
                    if (testClassesCount > 0)
                    {
                        if (!student.ClassId.HasValue)
                        {
                            continue;
                        }
                        
                        // Проверяем через SQL, есть ли связь теста с классом студента
                        var hasClassLinkSql = "SELECT COUNT(*) FROM RegularTestClasses WHERE RegularTestId = @TestId AND ClassId = @ClassId";
                        var hasClassLink = await _db.QueryScalarAsync<int>(hasClassLinkSql, new { TestId = test.Id, ClassId = student.ClassId.Value });
                        if (hasClassLink == 0)
                        {
                            continue;
                        }
                    }
                    
                    test.RegularQuestions = await _regularQuestionRepository.GetByTestIdOrderedAsync(test.Id);
                    
                    // Получаем результаты теста для студента через SQL
                    var resultsSql = "SELECT * FROM RegularTestResults WHERE StudentId = @StudentId AND RegularTestId = @TestId";
                    test.RegularTestResults = await _db.QueryAsync<RegularTestResult>(resultsSql, new { StudentId = student.Id, TestId = test.Id });
                    
                    filteredRegularTests.Add(test);
                }
                
                // Сортируем по названию
                filteredRegularTests.Sort((a, b) => string.Compare(a.Title, b.Title, StringComparison.Ordinal));
                viewModel.RegularTests = filteredRegularTests;
            }

            ViewBag.CurrentCategory = category;
            return View(viewModel);
        }

        #region Spelling Tests

        // GET: StudentTest/StartSpelling/5 - Начало теста по орфографии
        public async Task<IActionResult> StartSpelling(int id)
        {
            var currentUser = await _userManager.GetUserAsync(User);
            var student = await _studentRepository.GetByUserIdAsync(currentUser.Id);
            if (student == null) return NotFound();

            var test = await _spellingTestRepository.GetByIdAsync(id);
            if (test == null || !test.IsActive) return NotFound();
            
            // Загружаем связанные данные
            var testClasses = await _spellingTestClassRepository.GetByTestIdAsync(test.Id);
            var questions = await _spellingQuestionRepository.GetByTestIdOrderedAsync(test.Id);
            
            // Получаем результаты теста для студента через SQL
            var studentResultsSql = "SELECT * FROM SpellingTestResults WHERE StudentId = @StudentId AND SpellingTestId = @TestId";
            var studentResults = await _db.QueryAsync<SpellingTestResult>(studentResultsSql, new { StudentId = student.Id, TestId = test.Id });

            if (test == null) return NotFound();

            if (!await IsSpellingTestAvailableAsync(test, student, testClasses))
            {
                TempData["ErrorMessage"] = "Тест недоступен для прохождения.";
                return RedirectToAction(nameof(Index));
            }

            var attemptCount = studentResults.Count;
            if (attemptCount >= test.MaxAttempts)
            {
                TempData["ErrorMessage"] = $"Превышено количество попыток ({test.MaxAttempts}).";
                return RedirectToAction(nameof(Index));
            }

            // Получаем незавершенный результат через SQL
            var ongoingResultSql = "SELECT TOP 1 * FROM SpellingTestResults WHERE StudentId = @StudentId AND SpellingTestId = @TestId AND IsCompleted = 0 ORDER BY StartedAt DESC";
            var ongoingResults = await _db.QueryAsync<SpellingTestResult>(ongoingResultSql, new { StudentId = student.Id, TestId = test.Id });
            var ongoingResult = ongoingResults.FirstOrDefault();

            if (ongoingResult != null)
            {
                _logger.LogInformation("Студент {StudentId} продолжает незавершенный тест по орфографии {TestId}", student.Id, id);

                try
                {
                    var notificationData = new
                    {
                        testId = test.Id,
                        testTitle = test.Title,
                        testType = "spelling",
                        studentId = student.Id,
                        studentName = student.User.FullName,
                        timestamp = DateTime.Now,
                        action = "continued"
                    };

                    await _hubContext.Clients.Group($"teacher_{test.TeacherId}")
                        .SendAsync("StudentTestActivity", notificationData);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Ошибка отправки SignalR уведомления");
                }

                return RedirectToAction(nameof(TakeSpelling), new { id = ongoingResult.Id });
            }

            // Вычисляем максимальный балл через SQL
            var maxScoreSql = "SELECT ISNULL(SUM(Points), 0) FROM SpellingQuestions WHERE SpellingTestId = @TestId";
            var maxScore = await _db.QueryScalarAsync<int>(maxScoreSql, new { TestId = test.Id });
            var testResult = new SpellingTestResult
            {
                SpellingTestId = test.Id,
                StudentId = student.Id,
                StartedAt = DateTime.Now,
                AttemptNumber = attemptCount + 1,
                MaxScore = maxScore,
                IsCompleted = false
            };

            await _spellingTestResultRepository.CreateAsync(testResult);

            _logger.LogInformation("Студент {StudentId} начал тест по орфографии {TestId}", student.Id, id);

            try
            {
                var notificationData = new
                {
                    testId = test.Id,
                    testTitle = test.Title,
                    testType = "spelling",
                    studentId = student.Id,
                    studentName = student.User.FullName,
                    timestamp = DateTime.Now,
                    action = "started"
                };

                await _hubContext.Clients.Group($"teacher_{test.TeacherId}")
                    .SendAsync("StudentTestActivity", notificationData);

                _logger.LogInformation("SignalR: Отправлены уведомления о начале теста по орфографии {TestId} студентом {StudentName}",
                    test.Id, student.User.FullName);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Ошибка отправки SignalR уведомления");
            }

            return RedirectToAction(nameof(TakeSpelling), new { id = testResult.Id });
        }

        // GET: StudentTest/TakeSpelling/5 - Прохождение теста по орфографии
        public async Task<IActionResult> TakeSpelling(int id)
        {
            var currentUser = await _userManager.GetUserAsync(User);
            var student = await _studentRepository.GetByUserIdAsync(currentUser.Id);
            if (student == null) return NotFound();

            var testResult = await _spellingTestResultRepository.GetByIdAsync(id);
            if (testResult == null || testResult.StudentId != student.Id) return NotFound();
            
            var test = await _spellingTestRepository.GetByIdAsync(testResult.SpellingTestId);
            if (test == null) return NotFound();
            
            var questions = await _spellingQuestionRepository.GetByTestIdOrderedAsync(test.Id);
            var answers = await _spellingAnswerRepository.GetByTestResultIdAsync(testResult.Id);

            if (testResult == null) return NotFound();

            if (testResult.IsCompleted)
            {
                return RedirectToAction(nameof(SpellingResult), new { id = testResult.Id });
            }

            var timeElapsed = DateTime.Now - testResult.StartedAt;
            var timeLimit = TimeSpan.FromMinutes(test.TimeLimit);

            if (timeElapsed >= timeLimit)
            {
                _logger.LogInformation("Время теста по орфографии {ResultId} истекло для студента {StudentId}. Прошло: {TimeElapsed}, Лимит: {TimeLimit}",
                    id, student.Id, timeElapsed, timeLimit);
                await CompleteSpellingTest(testResult, isAutoCompleted: true);
                return RedirectToAction(nameof(SpellingResult), new { id = testResult.Id });
            }

            // Создаем временный объект для ViewModel (так как TestResult не имеет навигационных свойств)
            var viewModel = new TakeSpellingTestViewModel
            {
                TestResult = testResult,
                TimeRemaining = timeLimit - timeElapsed,
                CurrentQuestionIndex = 0
            };
            // Сохраняем вопросы и ответы для использования в представлении
            ViewBag.Questions = questions;
            ViewBag.Answers = answers;

            return View(viewModel);
        }

        // POST: StudentTest/SubmitSpellingAnswer - Сохранение ответа по орфографии
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SubmitSpellingAnswer(SubmitSpellingAnswerViewModel model)
        {
            var currentUser = await _userManager.GetUserAsync(User);
            var student = await _studentRepository.GetByUserIdAsync(currentUser.Id);
            if (student == null) return Json(new { success = false, message = "Студент не найден" });

            var testResult = await _spellingTestResultRepository.GetByIdAsync(model.TestResultId);
            if (testResult == null || testResult.StudentId != student.Id || testResult.IsCompleted)
                return Json(new { success = false, message = "Тест не найден или уже завершен" });

            var question = await _spellingQuestionRepository.GetByIdAsync(model.QuestionId);
            if (question == null || question.SpellingTestId != testResult.SpellingTestId)
                return Json(new { success = false, message = "Вопрос не найден" });

            // Получаем существующий ответ через SQL
            var existingAnswerSql = "SELECT TOP 1 * FROM SpellingAnswers WHERE SpellingTestResultId = @TestResultId AND SpellingQuestionId = @QuestionId";
            var existingAnswers = await _db.QueryAsync<SpellingAnswer>(existingAnswerSql, new { TestResultId = testResult.Id, QuestionId = model.QuestionId });
            var existingAnswer = existingAnswers.FirstOrDefault();

            bool isCorrect = CheckSpellingAnswer(question.CorrectLetter, model.StudentAnswer);
            int points = isCorrect ? question.Points : 0;

            if (existingAnswer != null)
            {
                existingAnswer.StudentAnswer = model.StudentAnswer?.Trim();
                existingAnswer.IsCorrect = isCorrect;
                existingAnswer.Points = points;
                existingAnswer.AnsweredAt = DateTime.Now;
                await _spellingAnswerRepository.UpdateAsync(existingAnswer);
            }
            else
            {
                var answer = new SpellingAnswer
                {
                    SpellingTestResultId = testResult.Id,
                    SpellingQuestionId = question.Id,
                    StudentAnswer = model.StudentAnswer?.Trim(),
                    IsCorrect = isCorrect,
                    Points = points,
                    AnsweredAt = DateTime.Now
                };

                await _spellingAnswerRepository.CreateAsync(answer);
            }

            _logger.LogInformation("Студент {StudentId} ответил на вопрос {QuestionId} в тесте по орфографии {ResultId}. Правильно: {IsCorrect}, Баллы: {Points}",
                student.Id, model.QuestionId, model.TestResultId, isCorrect, points);

            return Json(new
            {
                success = true,
                isCorrect = isCorrect,
                points = points,
                correctAnswer = question.CorrectLetter
            });
        }

        // POST: StudentTest/CompleteSpelling - Завершение теста по орфографии
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CompleteSpelling(int testResultId)
        {
            var currentUser = await _userManager.GetUserAsync(User);

            try
            {
                var student = await _studentRepository.GetByUserIdAsync(currentUser.Id);
                if (student == null) return NotFound();

                var testResult = await _spellingTestResultRepository.GetByIdAsync(testResultId);
                if (testResult == null || testResult.StudentId != student.Id) return NotFound();

                if (testResult == null) return NotFound();

                if (testResult.IsCompleted)
                {
                    return RedirectToAction(nameof(SpellingResult), new { id = testResult.Id });
                }

                await CompleteSpellingTest(testResult);

                _logger.LogInformation("Студент {StudentId} завершил тест по орфографии {ResultId}. Баллы: {Score}/{MaxScore}, Процент: {Percentage}",
                    student.Id, testResultId, testResult.Score, testResult.MaxScore, testResult.Percentage);

                return RedirectToAction("Result", new { id = testResult.Id });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Ошибка завершения теста по орфографии {ResultId} студентом {StudentId}", testResultId, currentUser.Id);
                TempData["ErrorMessage"] = "Произошла ошибка при завершении теста по орфографии.";
                Console.WriteLine(ex);
                return RedirectToAction(nameof(Index));
            }
        }

        // GET: StudentTest/SpellingResult/5 - Результат теста по орфографии
        public async Task<IActionResult> SpellingResult(int id)
        {
            var currentUser = await _userManager.GetUserAsync(User);
            var student = await _studentRepository.GetByUserIdAsync(currentUser.Id);

            if (student == null) return NotFound();

            var testResult = await _spellingTestResultRepository.GetByIdAsync(id);
            if (testResult == null || testResult.StudentId != student.Id) return NotFound();
            
            // Загружаем связанные данные
            var test = await _spellingTestRepository.GetByIdAsync(testResult.SpellingTestId);
            var questions = test != null ? await _spellingQuestionRepository.GetByTestIdOrderedAsync(test.Id) : new List<SpellingQuestion>();
            var answers = await _spellingAnswerRepository.GetByTestResultIdAsync(testResult.Id);
            
            // Загружаем вопросы для ответов через SQL
            var questionIdsSql = "SELECT DISTINCT SpellingQuestionId AS Id FROM SpellingAnswers WHERE SpellingTestResultId = @TestResultId";
            var questionIdsResults = await _db.QueryAsync<IntId>(questionIdsSql, new { TestResultId = testResult.Id });
            var questionIds = questionIdsResults.Select(r => r.Id).ToList();
            var answerQuestions = new List<SpellingQuestion>();
            foreach (var qId in questionIds)
            {
                var q = await _spellingQuestionRepository.GetByIdAsync(qId);
                if (q != null) answerQuestions.Add(q);
            }
            
            ViewBag.Test = test;
            ViewBag.Questions = questions;
            ViewBag.Answers = answers;
            ViewBag.AnswerQuestions = answerQuestions;

            return View("Result", testResult);
        }

        #endregion

        #region Punctuation Tests

        // GET: StudentTest/StartPunctuation/5 - Начало теста по пунктуации
        public async Task<IActionResult> StartPunctuation(int id)
        {
            var currentUser = await _userManager.GetUserAsync(User);
            var student = await _studentRepository.GetByUserIdAsync(currentUser.Id);

            if (student == null) return NotFound();

            var test = await _punctuationTestRepository.GetByIdAsync(id);
            if (test == null || !test.IsActive) return NotFound();
            
            // Загружаем связанные данные
            var testClasses = await _punctuationTestClassRepository.GetByTestIdAsync(test.Id);
            var questions = await _punctuationQuestionRepository.GetByTestIdOrderedAsync(test.Id);
            // Получаем результаты теста для студента через SQL
            var studentResultsSql = "SELECT * FROM PunctuationTestResults WHERE StudentId = @StudentId AND PunctuationTestId = @TestId";
            var studentResults = await _db.QueryAsync<PunctuationTestResult>(studentResultsSql, new { StudentId = student.Id, TestId = test.Id });

            if (test == null) return NotFound();

            if (!await IsPunctuationTestAvailableAsync(test, student, testClasses))
            {
                TempData["ErrorMessage"] = "Тест недоступен для прохождения.";
                return RedirectToAction(nameof(Index));
            }

            var attemptCount = studentResults.Count;
            if (attemptCount >= test.MaxAttempts)
            {
                _logger.LogWarning("Студент {StudentId} превысил лимит попыток ({MaxAttempts}) для теста по пунктуации {TestId}",
                    student.Id, test.MaxAttempts, id);
                TempData["ErrorMessage"] = $"Превышено количество попыток ({test.MaxAttempts}).";
                return RedirectToAction(nameof(Index));
            }

            // Получаем незавершенный результат через SQL
            var ongoingResultSql = "SELECT TOP 1 * FROM OrthoeopyTestResults WHERE StudentId = @StudentId AND OrthoeopyTestId = @TestId AND IsCompleted = 0 ORDER BY StartedAt DESC";
            var ongoingResults = await _db.QueryAsync<OrthoeopyTestResult>(ongoingResultSql, new { StudentId = student.Id, TestId = test.Id });
            var ongoingResult = ongoingResults.FirstOrDefault();

            if (ongoingResult != null)
            {
                _logger.LogInformation("Студент {StudentId} продолжает незавершенный тест по пунктуации {TestId}", student.Id, id);

                try
                {
                    var notificationData = new
                    {
                        testId = test.Id,
                        testTitle = test.Title,
                        testType = "punctuation",
                        studentId = student.Id,
                        studentName = student.User.FullName,
                        timestamp = DateTime.Now,
                        action = "continued"
                    };

                    await _hubContext.Clients.Group($"teacher_{test.TeacherId}")
                        .SendAsync("StudentTestActivity", notificationData);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Ошибка отправки SignalR уведомления");
                }

                return RedirectToAction(nameof(TakePunctuation), new { id = ongoingResult.Id });
            }

            var testResult = new PunctuationTestResult
            {
                PunctuationTestId = test.Id,
                StudentId = student.Id,
                StartedAt = DateTime.Now,
                AttemptNumber = attemptCount + 1,
                // Вычисляем максимальный балл через SQL
                MaxScore = await _db.QueryScalarAsync<int>("SELECT ISNULL(SUM(Points), 0) FROM PunctuationQuestions WHERE PunctuationTestId = @TestId", new { TestId = test.Id }),
                IsCompleted = false
            };

            await _punctuationTestResultRepository.CreateAsync(testResult);

            _logger.LogInformation("Студент {StudentId} начал тест по пунктуации {TestId}", student.Id, id);

            try
            {
                var notificationData = new
                {
                    testId = test.Id,
                    testTitle = test.Title,
                    testType = "punctuation",
                    studentId = student.Id,
                    studentName = student.User.FullName,
                    timestamp = DateTime.Now,
                    action = "started"
                };

                await _hubContext.Clients.Group($"teacher_{test.TeacherId}")
                    .SendAsync("StudentTestActivity", notificationData);

                _logger.LogInformation("SignalR: Отправлены уведомления о начале теста по пунктуации {TestId} студентом {StudentName}",
                    test.Id, student.User.FullName);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Ошибка отправки SignalR уведомления");
            }

            return RedirectToAction(nameof(TakePunctuation), new { id = testResult.Id });
        }

        // GET: StudentTest/TakePunctuation/5 - Прохождение теста по пунктуации
        public async Task<IActionResult> TakePunctuation(int id)
        {
            var currentUser = await _userManager.GetUserAsync(User);
            var student = await _studentRepository.GetByUserIdAsync(currentUser.Id);

            if (student == null) return NotFound();

            var testResult = await _punctuationTestResultRepository.GetByIdAsync(id);
            if (testResult == null || testResult.StudentId != student.Id) return NotFound();
            
            var test = await _punctuationTestRepository.GetByIdAsync(testResult.PunctuationTestId);
            if (test == null) return NotFound();
            
            var questions = await _punctuationQuestionRepository.GetByTestIdOrderedAsync(test.Id);
            var answers = await _punctuationAnswerRepository.GetByTestResultIdAsync(testResult.Id);

            if (testResult.IsCompleted)
            {
                return RedirectToAction(nameof(PunctuationResult), new { id = testResult.Id });
            }

            var timeElapsed = DateTime.Now - testResult.StartedAt;
            var timeLimit = TimeSpan.FromMinutes(test.TimeLimit);

            if (timeElapsed >= timeLimit)
            {
                _logger.LogInformation("Время теста по пунктуации {ResultId} истекло для студента {StudentId}. Прошло: {TimeElapsed}, Лимит: {TimeLimit}",
                    id, student.Id, timeElapsed, timeLimit);
                await CompletePunctuationTest(testResult);
                return RedirectToAction(nameof(PunctuationResult), new { id = testResult.Id });
            }

            ViewBag.Test = test;
            ViewBag.Questions = questions;
            ViewBag.Answers = answers;
            
            var viewModel = new TakePunctuationTestViewModel
            {
                TestResult = testResult,
                TimeRemaining = timeLimit - timeElapsed,
                CurrentQuestionIndex = 0
            };

            return View(viewModel);
        }

        // POST: StudentTest/SubmitPunctuationAnswer - Сохранение ответа по пунктуации
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SubmitPunctuationAnswer(SubmitPunctuationAnswerViewModel model)
        {
            var currentUser = await _userManager.GetUserAsync(User);
            var student = await _studentRepository.GetByUserIdAsync(currentUser.Id);

            if (student == null) return Json(new { success = false, message = "Студент не найден" });

            var testResult = await _punctuationTestResultRepository.GetByIdAsync(model.TestResultId);
            if (testResult == null || testResult.StudentId != student.Id || testResult.IsCompleted)
                return Json(new { success = false, message = "Тест не найден или уже завершен" });

            var question = await _punctuationQuestionRepository.GetByIdAsync(model.QuestionId);
            if (question == null || question.PunctuationTestId != testResult.PunctuationTestId)
                return Json(new { success = false, message = "Вопрос не найден" });

            // Получаем существующий ответ через SQL
            var existingAnswerSql = "SELECT TOP 1 * FROM PunctuationAnswers WHERE PunctuationTestResultId = @TestResultId AND PunctuationQuestionId = @QuestionId";
            var existingAnswers = await _db.QueryAsync<PunctuationAnswer>(existingAnswerSql, new { TestResultId = testResult.Id, QuestionId = model.QuestionId });
            var existingAnswer = existingAnswers.FirstOrDefault();

            bool isCorrect = CheckPunctuationAnswer(question.CorrectPositions, model.StudentAnswer);
            int points = isCorrect ? question.Points : 0;

            if (existingAnswer != null)
            {
                existingAnswer.StudentAnswer = model.StudentAnswer?.Trim();
                existingAnswer.IsCorrect = isCorrect;
                existingAnswer.Points = points;
                existingAnswer.AnsweredAt = DateTime.Now;
                await _punctuationAnswerRepository.UpdateAsync(existingAnswer);
            }
            else
            {
                var answer = new PunctuationAnswer
                {
                    PunctuationTestResultId = testResult.Id,
                    PunctuationQuestionId = question.Id,
                    StudentAnswer = model.StudentAnswer?.Trim(),
                    IsCorrect = isCorrect,
                    Points = points,
                    AnsweredAt = DateTime.Now
                };

                await _punctuationAnswerRepository.CreateAsync(answer);
            }


            _logger.LogInformation("Студент {StudentId} ответил на вопрос {QuestionId} в тесте по пунктуации {ResultId}. Правильно: {IsCorrect}, Баллы: {Points}",
                student.Id, model.QuestionId, model.TestResultId, isCorrect, points);

            return Json(new
            {
                success = true,
                isCorrect = isCorrect,
                points = points,
                correctAnswer = question.CorrectPositions
            });
        }

        // POST: StudentTest/CompletePunctuation - Завершение теста по пунктуации
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CompletePunctuation(int testResultId)
        {
            try
            {
                var currentUser = await _userManager.GetUserAsync(User);
                var student = await _studentRepository.GetByUserIdAsync(currentUser.Id);
                if (student == null) return NotFound();

                var testResult = await _punctuationTestResultRepository.GetByIdAsync(testResultId);
                if (testResult == null || testResult.StudentId != student.Id) return NotFound();

                if (testResult == null) return NotFound();

                if (testResult.IsCompleted)
                {
                    return RedirectToAction(nameof(PunctuationResult), new { id = testResult.Id });
                }

                await CompletePunctuationTest(testResult);

                _logger.LogInformation("Студент {StudentId} завершил тест по пунктуации {ResultId}. Баллы: {Score}/{MaxScore}, Процент: {Percentage}",
                    student.Id, testResultId, testResult.Score, testResult.MaxScore, testResult.Percentage);

                return RedirectToAction("Result", new { id = testResult.Id });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Ошибка завершения теста по пунктуации {ResultId} студентом {StudentId}", testResultId, _userManager.GetUserId(User));
                TempData["ErrorMessage"] = "Произошла ошибка при завершении теста по пунктуации.";
                return RedirectToAction(nameof(Index));
            }
        }

        // GET: StudentTest/PunctuationResult/5 - Результат теста по пунктуации
        public async Task<IActionResult> PunctuationResult(int id)
        {
            var currentUser = await _userManager.GetUserAsync(User);
            var student = await _studentRepository.GetByUserIdAsync(currentUser.Id);

            if (student == null) return NotFound();

            var testResult = await _punctuationTestResultRepository.GetByIdAsync(id);
            if (testResult == null || testResult.StudentId != student.Id) return NotFound();
            
            // Загружаем связанные данные
            var test = await _punctuationTestRepository.GetByIdAsync(testResult.PunctuationTestId);
            var questions = test != null ? await _punctuationQuestionRepository.GetByTestIdOrderedAsync(test.Id) : new List<PunctuationQuestion>();
            var answers = await _punctuationAnswerRepository.GetByTestResultIdAsync(testResult.Id);
            
            // Загружаем вопросы для ответов через SQL
            var questionIdsSql = "SELECT DISTINCT PunctuationQuestionId AS Id FROM PunctuationAnswers WHERE PunctuationTestResultId = @TestResultId";
            var questionIdsResults = await _db.QueryAsync<IntId>(questionIdsSql, new { TestResultId = testResult.Id });
            var questionIds = questionIdsResults.Select(r => r.Id).ToList();
            var answerQuestions = new List<PunctuationQuestion>();
            foreach (var qId in questionIds)
            {
                var q = await _punctuationQuestionRepository.GetByIdAsync(qId);
                if (q != null) answerQuestions.Add(q);
            }
            
            ViewBag.Test = test;
            ViewBag.Questions = questions;
            ViewBag.Answers = answers;
            ViewBag.AnswerQuestions = answerQuestions;

            return View("Result", testResult);
        }

        public async Task<IActionResult> Result(int id)
        {
            var currentUser = await _userManager.GetUserAsync(User);
            var student = await _studentRepository.GetByUserIdAsync(currentUser.Id);

            if (student == null) return NotFound();

            // Пробуем найти результат spelling теста
            var spellingResult = await _spellingTestResultRepository.GetByIdAsync(id);
            if (spellingResult != null && spellingResult.StudentId == student.Id)
            {
                var spellingTest = await _spellingTestRepository.GetByIdAsync(spellingResult.SpellingTestId);
                var spellingQuestions = spellingTest != null ? await _spellingQuestionRepository.GetByTestIdOrderedAsync(spellingTest.Id) : new List<SpellingQuestion>();
                var spellingAnswers = await _spellingAnswerRepository.GetByTestResultIdAsync(spellingResult.Id);
                ViewBag.Test = spellingTest;
                ViewBag.Questions = spellingQuestions;
                ViewBag.Answers = spellingAnswers;
                return View(spellingResult);
            }

            // Пробуем найти punctuation тест
            var punctuationResult = await _punctuationTestResultRepository.GetByIdAsync(id);
            if (punctuationResult != null && punctuationResult.StudentId == student.Id)
            {
                var punctuationTest = await _punctuationTestRepository.GetByIdAsync(punctuationResult.PunctuationTestId);
                var punctuationQuestions = punctuationTest != null ? await _punctuationQuestionRepository.GetByTestIdOrderedAsync(punctuationTest.Id) : new List<PunctuationQuestion>();
                var punctuationAnswers = await _punctuationAnswerRepository.GetByTestResultIdAsync(punctuationResult.Id);
                ViewBag.Test = punctuationTest;
                ViewBag.Questions = punctuationQuestions;
                ViewBag.Answers = punctuationAnswers;
                return View(punctuationResult);
            }

            // Пробуем найти orthoepy тест
            var orthoeopyResult = await _orthoeopyTestResultRepository.GetByIdAsync(id);
            if (orthoeopyResult != null && orthoeopyResult.StudentId == student.Id)
            {
                var orthoeopyTest = await _orthoeopyTestRepository.GetByIdAsync(orthoeopyResult.OrthoeopyTestId);
                var orthoeopyQuestions = orthoeopyTest != null ? await _orthoeopyQuestionRepository.GetByTestIdOrderedAsync(orthoeopyTest.Id) : new List<OrthoeopyQuestion>();
                var orthoeopyAnswers = await _orthoeopyAnswerRepository.GetByTestResultIdAsync(orthoeopyResult.Id);
                ViewBag.Test = orthoeopyTest;
                ViewBag.Questions = orthoeopyQuestions;
                ViewBag.Answers = orthoeopyAnswers;
                return View(orthoeopyResult);
            }

            // Пробуем найти regular тест
            var regularResult = await _regularTestResultRepository.GetByIdAsync(id);
            if (regularResult != null && regularResult.StudentId == student.Id)
            {
                var regularTest = await _regularTestRepository.GetByIdAsync(regularResult.RegularTestId);
                var regularQuestions = regularTest != null ? await _regularQuestionRepository.GetByTestIdOrderedAsync(regularTest.Id) : new List<RegularQuestion>();
                var regularAnswers = await _regularAnswerRepository.GetByTestResultIdAsync(regularResult.Id);
                // Загружаем опции для вопросов через SQL
                var questionIdsSql = "SELECT Id FROM RegularQuestions WHERE RegularTestId = @TestId ORDER BY OrderIndex";
                var questionIdsResults = await _db.QueryAsync<IntId>(questionIdsSql, new { TestId = regularTest.Id });
                var questionIds = questionIdsResults.Select(r => r.Id).ToList();
                var options = new List<RegularQuestionOption>();
                foreach (var qId in questionIds)
                {
                    var qOptions = await _regularQuestionOptionRepository.GetByQuestionIdAsync(qId);
                    options.AddRange(qOptions);
                }
                ViewBag.Test = regularTest;
                ViewBag.Questions = regularQuestions;
                ViewBag.Answers = regularAnswers;
                ViewBag.Options = options;
                return View(regularResult);
            }

            return NotFound();
        }

        #endregion

        #region Orthoepy Tests

        // GET: StudentTest/StartOrthoepy/5 - Начало теста по орфоэпии
        public async Task<IActionResult> StartOrthoepy(int id)
        {
            var currentUser = await _userManager.GetUserAsync(User);
            var student = await _studentRepository.GetByUserIdAsync(currentUser.Id);

            if (student == null) return NotFound();

            var test = await _orthoeopyTestRepository.GetByIdAsync(id);
            if (test == null || !test.IsActive) return NotFound();
            
            // Загружаем связанные данные
            var testClasses = await _orthoeopyTestClassRepository.GetByTestIdAsync(test.Id);
            var questions = await _orthoeopyQuestionRepository.GetByTestIdOrderedAsync(test.Id);
            // Получаем результаты теста для студента через SQL
            var studentResultsSql = "SELECT * FROM OrthoeopyTestResults WHERE StudentId = @StudentId AND OrthoeopyTestId = @TestId";
            var studentResults = await _db.QueryAsync<OrthoeopyTestResult>(studentResultsSql, new { StudentId = student.Id, TestId = test.Id });

            if (!await IsOrthoeopyTestAvailableAsync(test, student, testClasses))
            {
                TempData["ErrorMessage"] = "Тест недоступен для прохождения.";
                return RedirectToAction(nameof(Index));
            }

            var attemptCount = studentResults.Count;
            if (attemptCount >= test.MaxAttempts)
            {
                _logger.LogWarning("Студент {StudentId} превысил лимит попыток ({MaxAttempts}) для теста по орфоэпии {TestId}",
                    student.Id, test.MaxAttempts, id);
                TempData["ErrorMessage"] = $"Превышено количество попыток ({test.MaxAttempts}).";
                return RedirectToAction(nameof(Index));
            }

            // Получаем незавершенный результат через SQL
            var ongoingResultSql = "SELECT TOP 1 * FROM OrthoeopyTestResults WHERE StudentId = @StudentId AND OrthoeopyTestId = @TestId AND IsCompleted = 0 ORDER BY StartedAt DESC";
            var ongoingResults = await _db.QueryAsync<OrthoeopyTestResult>(ongoingResultSql, new { StudentId = student.Id, TestId = test.Id });
            var ongoingResult = ongoingResults.FirstOrDefault();

            if (ongoingResult != null)
            {
                _logger.LogInformation("Студент {StudentId} продолжает незавершенный тест по орфоэпии {TestId}", student.Id, id);

                try
                {
                    var notificationData = new
                    {
                        testId = test.Id,
                        testTitle = test.Title,
                        testType = "orthoeopy",
                        studentId = student.Id,
                        studentName = student.User.FullName,
                        timestamp = DateTime.Now,
                        action = "continued"
                    };

                    await _hubContext.Clients.Group($"teacher_{test.TeacherId}")
                        .SendAsync("StudentTestActivity", notificationData);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Ошибка отправки SignalR уведомления");
                }

                return RedirectToAction(nameof(TakeOrthoeopy), new { id = ongoingResult.Id });
            }

            var testResult = new OrthoeopyTestResult
            {
                OrthoeopyTestId = test.Id,
                StudentId = student.Id,
                StartedAt = DateTime.Now,
                AttemptNumber = attemptCount + 1,
                // Вычисляем максимальный балл через SQL
                MaxScore = await _db.QueryScalarAsync<int>("SELECT ISNULL(SUM(Points), 0) FROM OrthoeopyQuestions WHERE OrthoeopyTestId = @TestId", new { TestId = test.Id }),
                IsCompleted = false
            };

            await _orthoeopyTestResultRepository.CreateAsync(testResult);

            _logger.LogInformation("Студент {StudentId} начал тест по орфоэпии {TestId}", student.Id, id);

            try
            {
                var notificationData = new
                {
                    testId = test.Id,
                    testTitle = test.Title,
                    testType = "orthoeopy",
                    studentId = student.Id,
                    studentName = student.User.FullName,
                    timestamp = DateTime.Now,
                    action = "started"
                };

                await _hubContext.Clients.Group($"teacher_{test.TeacherId}")
                    .SendAsync("StudentTestActivity", notificationData);

                _logger.LogInformation("SignalR: Отправлены уведомления о начале теста {TestId} студентом {StudentName}",
                    test.Id, student.User.FullName);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Ошибка отправки SignalR уведомления");
            }

            return RedirectToAction(nameof(TakeOrthoeopy), new { id = testResult.Id });
        }

        // GET: StudentTest/TakeOrthoepy/5 - Прохождение теста по орфоэпии
        public async Task<IActionResult> TakeOrthoeopy(int id)
        {
            var currentUser = await _userManager.GetUserAsync(User);
            var student = await _studentRepository.GetByUserIdAsync(currentUser.Id);

            if (student == null) return NotFound();

            var testResult = await _orthoeopyTestResultRepository.GetByIdAsync(id);
            if (testResult == null || testResult.StudentId != student.Id) return NotFound();
            
            var test = await _orthoeopyTestRepository.GetByIdAsync(testResult.OrthoeopyTestId);
            if (test == null) return NotFound();
            
            var questions = await _orthoeopyQuestionRepository.GetByTestIdOrderedAsync(test.Id);
            var answers = await _orthoeopyAnswerRepository.GetByTestResultIdAsync(testResult.Id);

            if (testResult.IsCompleted)
            {
                return RedirectToAction(nameof(OrthoeopyResult), new { id = testResult.Id });
            }

            var timeElapsed = DateTime.Now - testResult.StartedAt;
            var timeLimit = TimeSpan.FromMinutes(test.TimeLimit);

            if (timeElapsed >= timeLimit)
            {
                _logger.LogInformation("Время теста по орфоэпии {ResultId} истекло для студента {StudentId}. Прошло: {TimeElapsed}, Лимит: {TimeLimit}",
                    id, student.Id, timeElapsed, timeLimit);
                await CompleteOrthoeopyTest(testResult);
                return RedirectToAction(nameof(OrthoeopyResult), new { id = testResult.Id });
            }

            ViewBag.Test = test;
            ViewBag.Questions = questions;
            ViewBag.Answers = answers;
            
            var viewModel = new TakeOrthoeopyTestViewModel
            {
                TestResult = testResult,
                TimeRemaining = timeLimit - timeElapsed,
                CurrentQuestionIndex = 0
            };

            return View(viewModel);
        }

        // POST: StudentTest/SubmitOrthoeopyAnswer - Сохранение ответа по орфоэпии
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SubmitOrthoeopyAnswer(int TestResultId, int QuestionId, int SelectedStressPosition)
        {
            var currentUser = await _userManager.GetUserAsync(User);
            var student = await _studentRepository.GetByUserIdAsync(currentUser.Id);

            if (student == null) return Json(new { success = false, message = "Студент не найден" });

            var testResult = await _orthoeopyTestResultRepository.GetByIdAsync(TestResultId);
            if (testResult == null || testResult.StudentId != student.Id || testResult.IsCompleted)
                return Json(new { success = false, message = "Тест не найден или уже завершен" });

            var question = await _orthoeopyQuestionRepository.GetByIdAsync(QuestionId);
            if (question == null || question.OrthoeopyTestId != testResult.OrthoeopyTestId)
                return Json(new { success = false, message = "Вопрос не найден" });

            // Получаем существующий ответ через SQL
            var existingAnswerSql = "SELECT TOP 1 * FROM OrthoeopyAnswers WHERE OrthoeopyTestResultId = @TestResultId AND OrthoeopyQuestionId = @QuestionId";
            var existingAnswers = await _db.QueryAsync<OrthoeopyAnswer>(existingAnswerSql, new { TestResultId = testResult.Id, QuestionId = QuestionId });
            var existingAnswer = existingAnswers.FirstOrDefault();

            bool isCorrect = question.StressPosition == SelectedStressPosition;
            int points = isCorrect ? question.Points : 0;

            if (existingAnswer != null)
            {
                existingAnswer.SelectedStressPosition = SelectedStressPosition;
                existingAnswer.IsCorrect = isCorrect;
                existingAnswer.Points = points;
                existingAnswer.AnsweredAt = DateTime.Now;
                await _orthoeopyAnswerRepository.UpdateAsync(existingAnswer);
            }
            else
            {
                var answer = new OrthoeopyAnswer
                {
                    OrthoeopyTestResultId = testResult.Id,
                    OrthoeopyQuestionId = question.Id,
                    SelectedStressPosition = SelectedStressPosition,
                    IsCorrect = isCorrect,
                    Points = points,
                    AnsweredAt = DateTime.Now
                };

                await _orthoeopyAnswerRepository.CreateAsync(answer);
            }


            _logger.LogInformation("Студент {StudentId} ответил на вопрос {QuestionId} в тесте по орфоэпии {ResultId}. Выбрана позиция: {SelectedPosition}, Правильная: {CorrectPosition}, Правильно: {IsCorrect}, Баллы: {Points}",
                student.Id, QuestionId, TestResultId, SelectedStressPosition, question.StressPosition, isCorrect, points);

            return Json(new
            {
                success = true,
                isCorrect = isCorrect,
                points = points,
                correctPosition = question.StressPosition
            });
        }

        // POST: StudentTest/CompleteOrthoepy - Завершение теста по орфоэпии
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CompleteOrthoepy(int testResultId)
        {
            var currentUser = await _userManager.GetUserAsync(User);

            try
            {
                var student = await _studentRepository.GetByUserIdAsync(currentUser.Id);
                if (student == null) return NotFound();

                var testResult = await _orthoeopyTestResultRepository.GetByIdAsync(testResultId);
                if (testResult == null || testResult.StudentId != student.Id) return NotFound();

                if (testResult == null) return NotFound();

                if (testResult.IsCompleted)
                {
                    return RedirectToAction(nameof(OrthoeopyResult), new { id = testResult.Id });
                }

                await CompleteOrthoeopyTest(testResult);

                _logger.LogInformation("Студент {StudentId} завершил тест по орфоэпии {ResultId}. Баллы: {Score}/{MaxScore}, Процент: {Percentage}",
                    student.Id, testResultId, testResult.Score, testResult.MaxScore, testResult.Percentage);

                return RedirectToAction("Result", new { id = testResult.Id });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Ошибка завершения теста по орфоэпии {ResultId} студентом {StudentId}", testResultId, currentUser.Id);
                TempData["ErrorMessage"] = "Произошла ошибка при завершении теста по орфоэпии.";
                Console.WriteLine(ex);
                return RedirectToAction(nameof(Index));
            }
        }

        // GET: StudentTest/OrthoeopyResult/5 - Результат теста по орфоэпии
        public async Task<IActionResult> OrthoeopyResult(int id)
        {
            var currentUser = await _userManager.GetUserAsync(User);
            var student = await _studentRepository.GetByUserIdAsync(currentUser.Id);

            if (student == null) return NotFound();

            var testResult = await _orthoeopyTestResultRepository.GetByIdAsync(id);
            if (testResult == null || testResult.StudentId != student.Id) return NotFound();
            
            // Загружаем связанные данные
            var test = await _orthoeopyTestRepository.GetByIdAsync(testResult.OrthoeopyTestId);
            var questions = test != null ? await _orthoeopyQuestionRepository.GetByTestIdOrderedAsync(test.Id) : new List<OrthoeopyQuestion>();
            var answers = await _orthoeopyAnswerRepository.GetByTestResultIdAsync(testResult.Id);
            
            // Загружаем вопросы для ответов через SQL
            var questionIdsSql = "SELECT DISTINCT OrthoeopyQuestionId AS Id FROM OrthoeopyAnswers WHERE OrthoeopyTestResultId = @TestResultId";
            var questionIdsResults = await _db.QueryAsync<IntId>(questionIdsSql, new { TestResultId = testResult.Id });
            var questionIds = questionIdsResults.Select(r => r.Id).ToList();
            var answerQuestions = new List<OrthoeopyQuestion>();
            foreach (var qId in questionIds)
            {
                var q = await _orthoeopyQuestionRepository.GetByIdAsync(qId);
                if (q != null) answerQuestions.Add(q);
            }
            
            ViewBag.Test = test;
            ViewBag.Questions = questions;
            ViewBag.Answers = answers;
            ViewBag.AnswerQuestions = answerQuestions;

            return View("Result", testResult);
        }

        #endregion

        #region Regular Tests

        // GET: StudentTest/StartRegular/5
        public async Task<IActionResult> StartRegular(int id)
        {
            var currentUser = await _userManager.GetUserAsync(User);
            var student = await _studentRepository.GetByUserIdAsync(currentUser.Id);

            if (student == null) return NotFound();

            var test = await _regularTestRepository.GetByIdAsync(id);
            if (test == null || !test.IsActive) return NotFound();
            
            // Загружаем связанные данные
            var testClasses = await _regularTestClassRepository.GetByTestIdAsync(test.Id);
            var questions = await _regularQuestionRepository.GetByTestIdOrderedAsync(test.Id);
            // Загружаем опции для вопросов через SQL
            var questionIdsSql = "SELECT Id FROM RegularQuestions WHERE RegularTestId = @TestId ORDER BY OrderIndex";
            var questionIdsResults = await _db.QueryAsync<IntId>(questionIdsSql, new { TestId = test.Id });
            var questionIds = questionIdsResults.Select(r => r.Id).ToList();
            var options = new List<RegularQuestionOption>();
            foreach (var qId in questionIds)
            {
                var qOptions = await _regularQuestionOptionRepository.GetByQuestionIdOrderedAsync(qId);
                options.AddRange(qOptions);
            }
            // Получаем результаты теста для студента через SQL
            var studentResultsSql = "SELECT * FROM RegularTestResults WHERE StudentId = @StudentId AND RegularTestId = @TestId";
            var studentResults = await _db.QueryAsync<RegularTestResult>(studentResultsSql, new { StudentId = student.Id, TestId = test.Id });

            if (!await IsRegularTestAvailableAsync(test, student, testClasses))
            {
                TempData["ErrorMessage"] = "Тест недоступен для прохождения.";
                return RedirectToAction(nameof(Index));
            }

            var attemptCount = studentResults.Count;
            if (attemptCount >= test.MaxAttempts)
            {
                _logger.LogWarning("Студент {StudentId} превысил лимит попыток ({MaxAttempts}) для классического теста {TestId}",
                    student.Id, test.MaxAttempts, id);
                TempData["ErrorMessage"] = $"Превышено количество попыток ({test.MaxAttempts}).";
                return RedirectToAction(nameof(Index));
            }

            // Получаем незавершенный результат через SQL
            var ongoingResultSql = "SELECT TOP 1 * FROM RegularTestResults WHERE StudentId = @StudentId AND RegularTestId = @TestId AND IsCompleted = 0 ORDER BY StartedAt DESC";
            var ongoingResults = await _db.QueryAsync<RegularTestResult>(ongoingResultSql, new { StudentId = student.Id, TestId = test.Id });
            var ongoingResult = ongoingResults.FirstOrDefault();

            if (ongoingResult != null)
            {
                _logger.LogInformation("Студент {StudentId} продолжает незавершенный классический тест {TestId}", student.Id, id);

                try
                {
                    var notificationData = new
                    {
                        testId = test.Id,
                        testTitle = test.Title,
                        testType = "regular",
                        studentId = student.Id,
                        studentName = student.User.FullName,
                        timestamp = DateTime.Now,
                        action = "continued"
                    };

                    await _hubContext.Clients.Group($"teacher_{test.TeacherId}")
                        .SendAsync("StudentTestActivity", notificationData);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Ошибка отправки SignalR уведомления");
                }

                return RedirectToAction(nameof(TakeRegular), new { id = ongoingResult.Id });
            }

            var testResult = new RegularTestResult
            {
                RegularTestId = test.Id,
                StudentId = student.Id,
                StartedAt = DateTime.Now,
                AttemptNumber = attemptCount + 1,
                // Вычисляем максимальный балл через SQL
                MaxScore = await _db.QueryScalarAsync<int>("SELECT ISNULL(SUM(Points), 0) FROM OrthoeopyQuestions WHERE OrthoeopyTestId = @TestId", new { TestId = test.Id }),
                IsCompleted = false
            };

            await _regularTestResultRepository.CreateAsync(testResult);

            _logger.LogInformation("Студент {StudentId} начал классический тест {TestId}", student.Id, id);

            try
            {
                var notificationData = new
                {
                    testId = test.Id,
                    testTitle = test.Title,
                    testType = "regular",
                    studentId = student.Id,
                    studentName = student.User.FullName,
                    timestamp = DateTime.Now,
                    action = "started"
                };

                await _hubContext.Clients.Group($"teacher_{test.TeacherId}")
                    .SendAsync("StudentTestActivity", notificationData);

                _logger.LogInformation("SignalR: Отправлены уведомления о начале классического теста {TestId} студентом {StudentName}",
                    test.Id, student.User.FullName);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Ошибка отправки SignalR уведомления");
            }

            return RedirectToAction(nameof(TakeRegular), new { id = testResult.Id });
        }

        // GET: StudentTest/TakeRegular/5
        public async Task<IActionResult> TakeRegular(int id)
        {
            var currentUser = await _userManager.GetUserAsync(User);
            var student = await _studentRepository.GetByUserIdAsync(currentUser.Id);

            if (student == null) return NotFound();

            var testResult = await _regularTestResultRepository.GetByIdAsync(id);
            if (testResult == null || testResult.StudentId != student.Id) return NotFound();
            
            var test = await _regularTestRepository.GetByIdAsync(testResult.RegularTestId);
            if (test == null) return NotFound();
            
            var questions = await _regularQuestionRepository.GetByTestIdOrderedAsync(test.Id);
            // Загружаем опции для вопросов через SQL
            var questionIdsSql = "SELECT Id FROM RegularQuestions WHERE RegularTestId = @TestId ORDER BY OrderIndex";
            var questionIdsResults = await _db.QueryAsync<IntId>(questionIdsSql, new { TestId = test.Id });
            var questionIds = questionIdsResults.Select(r => r.Id).ToList();
            var options = new List<RegularQuestionOption>();
            foreach (var qId in questionIds)
            {
                var qOptions = await _regularQuestionOptionRepository.GetByQuestionIdOrderedAsync(qId);
                options.AddRange(qOptions);
            }
            var answers = await _regularAnswerRepository.GetByTestResultIdAsync(testResult.Id);

            if (testResult.IsCompleted)
            {
                return RedirectToAction(nameof(RegularResult), new { id = testResult.Id });
            }

            var timeElapsed = DateTime.Now - testResult.StartedAt;
            var timeLimit = TimeSpan.FromMinutes(test.TimeLimit);

            if (timeElapsed >= timeLimit)
            {
                _logger.LogInformation("Время классического теста {ResultId} истекло для студента {StudentId}",
                    id, student.Id);
                await CompleteRegularTest(testResult);
                return RedirectToAction(nameof(RegularResult), new { id = testResult.Id });
            }

            ViewBag.Test = test;
            ViewBag.Questions = questions;
            ViewBag.Options = options;
            ViewBag.Answers = answers;
            
            var viewModel = new TakeRegularTestViewModel
            {
                TestResult = testResult,
                TimeRemaining = timeLimit - timeElapsed,
                CurrentQuestionIndex = 0
            };

            return View(viewModel);
        }

        // POST: StudentTest/SubmitRegularAnswer
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SubmitRegularAnswer(SubmitRegularAnswerViewModel model)
        {
            var currentUser = await _userManager.GetUserAsync(User);
            var student = await _studentRepository.GetByUserIdAsync(currentUser.Id);

            if (student == null)
                return Json(new { success = false, message = "Студент не найден" });

            var testResult = await _regularTestResultRepository.GetByIdAsync(model.TestResultId);
            if (testResult == null || testResult.StudentId != student.Id || testResult.IsCompleted)
                return Json(new { success = false, message = "Тест не найден или уже завершен" });

            var question = await _regularQuestionRepository.GetByIdAsync(model.QuestionId);
            if (question == null || question.TestId != testResult.RegularTestId)
                return Json(new { success = false, message = "Вопрос не найден" });
            
            var questionOptions = await _regularQuestionOptionRepository.GetByQuestionIdOrderedAsync(question.Id);
            // Получаем существующий ответ через SQL
            var existingAnswerSql = "SELECT TOP 1 * FROM RegularAnswers WHERE TestResultId = @TestResultId AND QuestionId = @QuestionId";
            var existingAnswers = await _db.QueryAsync<RegularAnswer>(existingAnswerSql, new { TestResultId = testResult.Id, QuestionId = model.QuestionId });
            var existingAnswer = existingAnswers.FirstOrDefault();

            bool isCorrect = false;
            int points = 0;
            string selectedOptionIdsStr = "";

            // Проверка ответа в зависимости от типа вопроса
            switch (question.Type)
            {
                case QuestionType.SingleChoice:
                    if (model.SelectedOptionId.HasValue)
                    {
                        selectedOptionIdsStr = model.SelectedOptionId.Value.ToString();
                        // Находим опцию в уже загруженном списке
                        var selectedOption = questionOptions.Find(o => o.Id == model.SelectedOptionId.Value);
                        isCorrect = selectedOption?.IsCorrect ?? false;
                    }
                    break;

                case QuestionType.MultipleChoice:
                    if (model.SelectedOptionIds != null && model.SelectedOptionIds.Count > 0)
                    {
                        var sortedSelectedIds = new List<int>(model.SelectedOptionIds);
                        sortedSelectedIds.Sort();
                        selectedOptionIdsStr = string.Join(",", sortedSelectedIds);
                        
                        // Получаем правильные опции через SQL
                        var correctOptionIdsSql = "SELECT Id FROM RegularQuestionOptions WHERE QuestionId = @QuestionId AND IsCorrect = 1 ORDER BY Id";
                        var correctOptionIdsResults = await _db.QueryAsync<IntId>(correctOptionIdsSql, new { QuestionId = question.Id });
                        var correctOptionIds = correctOptionIdsResults.Select(r => r.Id).ToList();
                        var correctIdsList = correctOptionIds.ToList();
                        correctIdsList.Sort();
                        
                        isCorrect = correctIdsList.Count == sortedSelectedIds.Count && 
                                   correctIdsList.SequenceEqual(sortedSelectedIds);
                    }
                    break;

                case QuestionType.TrueFalse:
                    if (model.SelectedOptionId.HasValue)
                    {
                        selectedOptionIdsStr = model.SelectedOptionId.Value.ToString();
                        // Находим опцию в уже загруженном списке
                        var selectedOption = questionOptions.Find(o => o.Id == model.SelectedOptionId.Value);
                        isCorrect = selectedOption?.IsCorrect ?? false;
                    }
                    break;
            }

            points = isCorrect ? question.Points : 0;

            if (existingAnswer != null)
            {
                existingAnswer.SelectedOptionIds = selectedOptionIdsStr;
                existingAnswer.IsCorrect = isCorrect;
                existingAnswer.Points = points;
                existingAnswer.AnsweredAt = DateTime.Now;
                await _regularAnswerRepository.UpdateAsync(existingAnswer);
            }
            else
            {
                var answer = new RegularAnswer
                {
                    TestResultId = testResult.Id,
                    QuestionId = question.Id,
                    SelectedOptionIds = selectedOptionIdsStr,
                    IsCorrect = isCorrect,
                    Points = points,
                    AnsweredAt = DateTime.Now
                };

                await _regularAnswerRepository.CreateAsync(answer);
            }


            _logger.LogInformation("Студент {StudentId} ответил на вопрос {QuestionId} классического теста {ResultId}. Правильно: {IsCorrect}, Баллы: {Points}",
                student.Id, model.QuestionId, model.TestResultId, isCorrect, points);

            // Получаем правильные ответы через SQL
            var correctAnswersSql = "SELECT Id FROM RegularQuestionOptions WHERE QuestionId = @QuestionId AND IsCorrect = 1";
            var correctAnswersResults = await _db.QueryAsync<IntId>(correctAnswersSql, new { QuestionId = question.Id });
            var correctAnswers = correctAnswersResults.Select(r => r.Id).ToList();

            return Json(new
            {
                success = true,
                isCorrect = isCorrect,
                points = points,
                correctAnswers = correctAnswers,
                questionType = question.Type.ToString()
            });
        }

        // POST: StudentTest/CompleteRegular
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CompleteRegular(int testResultId)
        {
            try
            {
                var currentUser = await _userManager.GetUserAsync(User);
                var student = await _studentRepository.GetByUserIdAsync(currentUser.Id);
                if (student == null) return NotFound();

                var testResult = await _regularTestResultRepository.GetByIdAsync(testResultId);
                if (testResult == null || testResult.StudentId != student.Id) return NotFound();

                if (testResult == null) return NotFound();

                if (testResult.IsCompleted)
                {
                    return RedirectToAction(nameof(RegularResult), new { id = testResult.Id });
                }

                await CompleteRegularTest(testResult);

                _logger.LogInformation("Студент {StudentId} завершил классический тест {ResultId}. Баллы: {Score}/{MaxScore}, Процент: {Percentage}",
                    student.Id, testResultId, testResult.Score, testResult.MaxScore, testResult.Percentage);

                return RedirectToAction(nameof(Result), new { id = testResult.Id });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Ошибка завершения классического теста {ResultId}", testResultId);
                TempData["ErrorMessage"] = "Произошла ошибка при завершении классического теста.";
                return RedirectToAction(nameof(Index));
            }
        }

        // GET: StudentTest/RegularResult/5
        public async Task<IActionResult> RegularResult(int id)
        {
            var currentUser = await _userManager.GetUserAsync(User);
            var student = await _studentRepository.GetByUserIdAsync(currentUser.Id);

            if (student == null) return NotFound();

            var testResult = await _regularTestResultRepository.GetByIdAsync(id);
            if (testResult == null || testResult.StudentId != student.Id) return NotFound();
            
            // Загружаем связанные данные
            var test = await _regularTestRepository.GetByIdAsync(testResult.RegularTestId);
            var questions = test != null ? await _regularQuestionRepository.GetByTestIdOrderedAsync(test.Id) : new List<RegularQuestion>();
            var answers = await _regularAnswerRepository.GetByTestResultIdAsync(testResult.Id);
            // Загружаем опции для вопросов через SQL
            var questionIdsSql = "SELECT Id FROM RegularQuestions WHERE RegularTestId = @TestId ORDER BY OrderIndex";
            var questionIdsResults = await _db.QueryAsync<IntId>(questionIdsSql, new { TestId = test.Id });
            var questionIds = questionIdsResults.Select(r => r.Id).ToList();
            var options = new List<RegularQuestionOption>();
            foreach (var qId in questionIds)
            {
                var qOptions = await _regularQuestionOptionRepository.GetByQuestionIdOrderedAsync(qId);
                options.AddRange(qOptions);
            }
            
            ViewBag.Test = test;
            ViewBag.Questions = questions;
            ViewBag.Answers = answers;
            ViewBag.Options = options;

            return View("Result", testResult);
        }

        #endregion

        #region History and Common Actions

        // GET: StudentTest/History - История прохождения всех тестов
        public async Task<IActionResult> History(string? testType = null)
        {
            var currentUser = await _userManager.GetUserAsync(User);
            var student = await _studentRepository.GetByUserIdAsync(currentUser.Id);

            if (student == null) return NotFound();

            var viewModel = new StudentTestHistoryViewModel
            {
                Student = student
            };

            if (testType == null || testType == "spelling")
            {
                // Получаем завершенные результаты через SQL
                var spellingResultsSql = "SELECT * FROM SpellingTestResults WHERE StudentId = @StudentId AND IsCompleted = 1 ORDER BY CompletedAt DESC";
                viewModel.SpellingResults = await _db.QueryAsync<SpellingTestResult>(spellingResultsSql, new { StudentId = student.Id });
            }

            if (testType == null || testType == "punctuation")
            {
                // Получаем завершенные результаты через SQL
                var punctuationResultsSql = "SELECT * FROM PunctuationTestResults WHERE StudentId = @StudentId AND IsCompleted = 1 ORDER BY CompletedAt DESC";
                viewModel.PunctuationResults = await _db.QueryAsync<PunctuationTestResult>(punctuationResultsSql, new { StudentId = student.Id });
            }

            if (testType == null || testType == "orthoepy")
            {
                // Получаем завершенные результаты через SQL
                var orthoeopyResultsSql = "SELECT * FROM OrthoeopyTestResults WHERE StudentId = @StudentId AND IsCompleted = 1 ORDER BY CompletedAt DESC";
                viewModel.OrthoeopyResults = await _db.QueryAsync<OrthoeopyTestResult>(orthoeopyResultsSql, new { StudentId = student.Id });
            }

            if (testType == null || testType == "regular")
            {
                // Получаем завершенные результаты через SQL
                var regularResultsSql = "SELECT * FROM RegularTestResults WHERE StudentId = @StudentId AND IsCompleted = 1 ORDER BY CompletedAt DESC";
                viewModel.RegularResults = await _db.QueryAsync<RegularTestResult>(regularResultsSql, new { StudentId = student.Id });
            }

            ViewBag.TestType = testType;
            return View(viewModel);
        }

        #endregion

        #region Private Methods

        // ✅ ИСПРАВЛЕНО: Проверка доступности теста по орфографии
        private async Task<bool> IsSpellingTestAvailableAsync(SpellingTest test, Student student, List<SpellingTestClass> testClasses)
        {
            // Если у теста есть привязки к классам
            if (testClasses.Count > 0)
            {
                // Проверяем через SQL, что студент в одном из этих классов
                if (!student.ClassId.HasValue)
                {
                    return false;
                }
                
                var hasClassLinkSql = "SELECT COUNT(*) FROM SpellingTestClasses WHERE SpellingTestId = @TestId AND ClassId = @ClassId";
                var hasClassLink = await _db.QueryScalarAsync<int>(hasClassLinkSql, new { TestId = test.Id, ClassId = student.ClassId.Value });
                if (hasClassLink == 0)
                {
                    return false;
                }
            }

            if (test.StartDate.HasValue && test.StartDate > DateTime.Now)
                return false;

            if (test.EndDate.HasValue && test.EndDate < DateTime.Now)
                return false;

            return true;
        }

        // ✅ ИСПРАВЛЕНО: Проверка доступности теста по пунктуации
        private async Task<bool> IsPunctuationTestAvailableAsync(PunctuationTest test, Student student, List<PunctuationTestClass> testClasses)
        {
            if (testClasses.Count > 0)
            {
                if (!student.ClassId.HasValue)
                {
                    return false;
                }
                
                var hasClassLinkSql = "SELECT COUNT(*) FROM PunctuationTestClasses WHERE PunctuationTestId = @TestId AND ClassId = @ClassId";
                var hasClassLink = await _db.QueryScalarAsync<int>(hasClassLinkSql, new { TestId = test.Id, ClassId = student.ClassId.Value });
                if (hasClassLink == 0)
                {
                    return false;
                }
            }

            if (test.StartDate.HasValue && test.StartDate > DateTime.Now)
                return false;

            if (test.EndDate.HasValue && test.EndDate < DateTime.Now)
                return false;

            return true;
        }

        // Проверка доступности теста по орфоэпии
        private async Task<bool> IsOrthoeopyTestAvailableAsync(OrthoeopyTest test, Student student, List<OrthoeopyTestClass> testClasses)
        {
            if (testClasses.Count > 0)
            {
                if (!student.ClassId.HasValue)
                {
                    return false;
                }
                
                var hasClassLinkSql = "SELECT COUNT(*) FROM OrthoeopyTestClasses WHERE OrthoeopyTestId = @TestId AND ClassId = @ClassId";
                var hasClassLink = await _db.QueryScalarAsync<int>(hasClassLinkSql, new { TestId = test.Id, ClassId = student.ClassId.Value });
                if (hasClassLink == 0)
                {
                    return false;
                }
            }

            if (test.StartDate.HasValue && test.StartDate > DateTime.Now)
                return false;

            if (test.EndDate.HasValue && test.EndDate < DateTime.Now)
                return false;

            return true;
        }

        // Проверка доступности классического теста
        private async Task<bool> IsRegularTestAvailableAsync(RegularTest test, Student student, List<RegularTestClass> testClasses)
        {
            if (testClasses.Count > 0)
            {
                if (!student.ClassId.HasValue)
                {
                    return false;
                }
                
                var hasClassLinkSql = "SELECT COUNT(*) FROM RegularTestClasses WHERE RegularTestId = @TestId AND ClassId = @ClassId";
                var hasClassLink = await _db.QueryScalarAsync<int>(hasClassLinkSql, new { TestId = test.Id, ClassId = student.ClassId.Value });
                if (hasClassLink == 0)
                {
                    return false;
                }
            }

            if (test.StartDate.HasValue && test.StartDate > DateTime.Now)
                return false;

            if (test.EndDate.HasValue && test.EndDate < DateTime.Now)
                return false;

            return true;
        }

        private bool CheckSpellingAnswer(string correctLetter, string studentAnswer)
        {
            if (string.IsNullOrWhiteSpace(studentAnswer) || string.IsNullOrWhiteSpace(correctLetter))
                return false;

            var correct = correctLetter.Trim().ToLowerInvariant();
            var student = studentAnswer.Trim().ToLowerInvariant();

            if (correct == student) return true;

            if (correct.Contains(',') || student.Contains(','))
            {
                var correctLetters = correct.Split(',')
                    .Select(l => l.Trim())
                    .Where(l => !string.IsNullOrEmpty(l))
                    .ToArray();

                var studentLetters = student.Split(',')
                    .Select(l => l.Trim())
                    .Where(l => !string.IsNullOrEmpty(l))
                    .ToArray();

                return correctLetters.SequenceEqual(studentLetters);
            }

            return false;
        }

        private bool CheckPunctuationAnswer(string correctPositions, string studentAnswer)
        {
            if (string.IsNullOrWhiteSpace(studentAnswer) && string.IsNullOrWhiteSpace(correctPositions))
            {
                return true;
            }

            if (string.IsNullOrWhiteSpace(studentAnswer) || string.IsNullOrWhiteSpace(correctPositions))
            {
                return false;
            }

            var correctSet = correctPositions.Trim()
                .Where(c => char.IsDigit(c))
                .OrderBy(c => c)
                .ToHashSet();

            var studentSet = studentAnswer.Trim()
                .Where(c => char.IsDigit(c))
                .OrderBy(c => c)
                .ToHashSet();

            return correctSet.SetEquals(studentSet);
        }

        private async Task CompleteSpellingTest(SpellingTestResult testResult, bool isAutoCompleted = false)
        {
            testResult.CompletedAt = DateTime.Now;
            testResult.IsCompleted = true;
            
            // Вычисляем сумму баллов через SQL
            var scoreSql = "SELECT ISNULL(SUM(Points), 0) FROM SpellingAnswers WHERE SpellingTestResultId = @TestResultId";
            testResult.Score = await _db.QueryScalarAsync<int>(scoreSql, new { TestResultId = testResult.Id });
            testResult.Percentage = testResult.MaxScore > 0
                ? Math.Round((double)testResult.Score / testResult.MaxScore * 100, 2)
                : 0;

            var test = await _spellingTestRepository.GetByIdAsync(testResult.SpellingTestId);
            var student = await _studentRepository.GetWithUserAsync(testResult.StudentId);

            if (test == null || student == null) return;
            
            var user = await _userManager.FindByIdAsync(student.UserId);
            var studentName = user != null ? $"{user.FirstName} {user.LastName}" : "Unknown";

            try
            {
                var notificationData = new
                {
                    testId = test.Id,
                    testTitle = test.Title,
                    testType = "spelling",
                    studentId = student.Id,
                    studentName = studentName,
                    score = testResult.Score,
                    maxScore = testResult.MaxScore,
                    percentage = testResult.Percentage,
                    timestamp = DateTime.Now,
                    action = "completed",
                    isAutoCompleted = isAutoCompleted
                };

                await _hubContext.Clients.Group($"teacher_{test.TeacherId}")
                    .SendAsync("StudentTestActivity", notificationData);

                _logger.LogInformation("SignalR: Отправлены уведомления о завершении теста {TestId} студентом {StudentName}",
                    test.Id, studentName);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Ошибка отправки SignalR уведомления");
            }
            
            await _spellingTestResultRepository.UpdateAsync(testResult);
        }

        private async Task CompletePunctuationTest(PunctuationTestResult testResult, bool isAutoCompleted = false)
        {
            testResult.CompletedAt = DateTime.Now;
            testResult.IsCompleted = true;
            
            // Вычисляем сумму баллов через SQL
            var scoreSql = "SELECT ISNULL(SUM(Points), 0) FROM PunctuationAnswers WHERE PunctuationTestResultId = @TestResultId";
            testResult.Score = await _db.QueryScalarAsync<int>(scoreSql, new { TestResultId = testResult.Id });
            testResult.Percentage = testResult.MaxScore > 0
                ? Math.Round((double)testResult.Score / testResult.MaxScore * 100, 2)
                : 0;

            var test = await _punctuationTestRepository.GetByIdAsync(testResult.PunctuationTestId);
            var student = await _studentRepository.GetWithUserAsync(testResult.StudentId);

            if (test == null || student == null) return;
            
            var user = await _userManager.FindByIdAsync(student.UserId);
            var studentName = user != null ? $"{user.FirstName} {user.LastName}" : "Unknown";

            try
            {
                var notificationData = new
                {
                    testId = test.Id,
                    testTitle = test.Title,
                    testType = "punctuation",
                    studentId = student.Id,
                    studentName = studentName,
                    score = testResult.Score,
                    maxScore = testResult.MaxScore,
                    percentage = testResult.Percentage,
                    timestamp = DateTime.Now,
                    action = "completed",
                    isAutoCompleted = isAutoCompleted
                };

                await _hubContext.Clients.Group($"teacher_{test.TeacherId}")
                    .SendAsync("StudentTestActivity", notificationData);

                _logger.LogInformation("SignalR: Отправлены уведомления о завершении теста {TestId} студентом {StudentName}",
                    test.Id, studentName);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Ошибка отправки SignalR уведомления");
            }
            
            await _punctuationTestResultRepository.UpdateAsync(testResult);
        }

        private async Task CompleteOrthoeopyTest(OrthoeopyTestResult testResult, bool isAutoCompleted = false)
        {
            testResult.CompletedAt = DateTime.Now;
            testResult.IsCompleted = true;
            
            // Вычисляем сумму баллов через SQL
            var scoreSql = "SELECT ISNULL(SUM(Points), 0) FROM OrthoeopyAnswers WHERE OrthoeopyTestResultId = @TestResultId";
            testResult.Score = await _db.QueryScalarAsync<int>(scoreSql, new { TestResultId = testResult.Id });
            testResult.Percentage = testResult.MaxScore > 0
                ? Math.Round((double)testResult.Score / testResult.MaxScore * 100, 2)
                : 0;

            var test = await _orthoeopyTestRepository.GetByIdAsync(testResult.OrthoeopyTestId);
            var student = await _studentRepository.GetWithUserAsync(testResult.StudentId);

            if (test == null || student == null) return;
            
            var user = await _userManager.FindByIdAsync(student.UserId);
            var studentName = user != null ? $"{user.FirstName} {user.LastName}" : "Unknown";

            try
            {
                var notificationData = new
                {
                    testId = test.Id,
                    testTitle = test.Title,
                    testType = "orthoeopy",
                    studentId = student.Id,
                    studentName = studentName,
                    score = testResult.Score,
                    maxScore = testResult.MaxScore,
                    percentage = testResult.Percentage,
                    timestamp = DateTime.Now,
                    action = "completed",
                    isAutoCompleted = isAutoCompleted
                };

                await _hubContext.Clients.Group($"teacher_{test.TeacherId}")
                    .SendAsync("StudentTestActivity", notificationData);

                _logger.LogInformation("SignalR: Отправлены уведомления о завершении теста {TestId} студентом {StudentName}",
                    test.Id, studentName);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Ошибка отправки SignalR уведомления");
            }
            
            await _orthoeopyTestResultRepository.UpdateAsync(testResult);
        }

        private async Task CompleteRegularTest(RegularTestResult testResult, bool isAutoCompleted = false)
        {
            testResult.CompletedAt = DateTime.Now;
            testResult.IsCompleted = true;
            
            // Вычисляем сумму баллов через SQL
            var scoreSql = "SELECT ISNULL(SUM(Points), 0) FROM RegularAnswers WHERE TestResultId = @TestResultId";
            testResult.Score = await _db.QueryScalarAsync<int>(scoreSql, new { TestResultId = testResult.Id });
            testResult.Percentage = testResult.MaxScore > 0
                ? Math.Round((double)testResult.Score / testResult.MaxScore * 100, 2)
                : 0;

            var test = await _regularTestRepository.GetByIdAsync(testResult.RegularTestId);
            var student = await _studentRepository.GetWithUserAsync(testResult.StudentId);

            if (test == null || student == null) return;
            
            var user = await _userManager.FindByIdAsync(student.UserId);
            var studentName = user != null ? $"{user.FirstName} {user.LastName}" : "Unknown";

            try
            {
                var notificationData = new
                {
                    testId = test.Id,
                    testTitle = test.Title,
                    testType = "regular",
                    studentId = student.Id,
                    studentName = studentName,
                    score = testResult.Score,
                    maxScore = testResult.MaxScore,
                    percentage = testResult.Percentage,
                    timestamp = DateTime.Now,
                    action = "completed",
                    isAutoCompleted = isAutoCompleted
                };

                await _hubContext.Clients.Group($"teacher_{test.TeacherId}")
                    .SendAsync("StudentTestActivity", notificationData);

                _logger.LogInformation("SignalR: Отправлены уведомления о завершении теста {TestId} студентом {StudentName}",
                    test.Id, studentName);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Ошибка отправки SignalR уведомления");
            }
            
            await _regularTestResultRepository.UpdateAsync(testResult);
        }

        #endregion
    }
}
