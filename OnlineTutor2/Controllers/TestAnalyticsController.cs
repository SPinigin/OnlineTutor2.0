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
    public class TestAnalyticsController : Controller
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly ILogger<TestAnalyticsController> _logger;
        private readonly ISpellingTestRepository _spellingTestRepository;
        private readonly IRegularTestRepository _regularTestRepository;
        private readonly IPunctuationTestRepository _punctuationTestRepository;
        private readonly IOrthoeopyTestRepository _orthoeopyTestRepository;
        private readonly ISpellingQuestionRepository _spellingQuestionRepository;
        private readonly IRegularQuestionRepository _regularQuestionRepository;
        private readonly IRegularQuestionOptionRepository _regularQuestionOptionRepository;
        private readonly IPunctuationQuestionRepository _punctuationQuestionRepository;
        private readonly IOrthoeopyQuestionRepository _orthoeopyQuestionRepository;
        private readonly ISpellingTestResultRepository _spellingTestResultRepository;
        private readonly IRegularTestResultRepository _regularTestResultRepository;
        private readonly IPunctuationTestResultRepository _punctuationTestResultRepository;
        private readonly IOrthoeopyTestResultRepository _orthoeopyTestResultRepository;
        private readonly ISpellingAnswerRepository _spellingAnswerRepository;
        private readonly IRegularAnswerRepository _regularAnswerRepository;
        private readonly IPunctuationAnswerRepository _punctuationAnswerRepository;
        private readonly IOrthoeopyAnswerRepository _orthoeopyAnswerRepository;
        private readonly ISpellingTestClassRepository _spellingTestClassRepository;
        private readonly IRegularTestClassRepository _regularTestClassRepository;
        private readonly IPunctuationTestClassRepository _punctuationTestClassRepository;
        private readonly IOrthoeopyTestClassRepository _orthoeopyTestClassRepository;
        private readonly IClassRepository _classRepository;
        private readonly IStudentRepository _studentRepository;
        private readonly IDatabaseConnection _db;

        public TestAnalyticsController(
            UserManager<ApplicationUser> userManager,
            ILogger<TestAnalyticsController> logger,
            ISpellingTestRepository spellingTestRepository,
            IRegularTestRepository regularTestRepository,
            IPunctuationTestRepository punctuationTestRepository,
            IOrthoeopyTestRepository orthoeopyTestRepository,
            ISpellingQuestionRepository spellingQuestionRepository,
            IRegularQuestionRepository regularQuestionRepository,
            IRegularQuestionOptionRepository regularQuestionOptionRepository,
            IPunctuationQuestionRepository punctuationQuestionRepository,
            IOrthoeopyQuestionRepository orthoeopyQuestionRepository,
            ISpellingTestResultRepository spellingTestResultRepository,
            IRegularTestResultRepository regularTestResultRepository,
            IPunctuationTestResultRepository punctuationTestResultRepository,
            IOrthoeopyTestResultRepository orthoeopyTestResultRepository,
            ISpellingAnswerRepository spellingAnswerRepository,
            IRegularAnswerRepository regularAnswerRepository,
            IPunctuationAnswerRepository punctuationAnswerRepository,
            IOrthoeopyAnswerRepository orthoeopyAnswerRepository,
            ISpellingTestClassRepository spellingTestClassRepository,
            IRegularTestClassRepository regularTestClassRepository,
            IPunctuationTestClassRepository punctuationTestClassRepository,
            IOrthoeopyTestClassRepository orthoeopyTestClassRepository,
            IClassRepository classRepository,
            IStudentRepository studentRepository,
            IDatabaseConnection db)
        {
            _userManager = userManager;
            _logger = logger;
            _spellingTestRepository = spellingTestRepository;
            _regularTestRepository = regularTestRepository;
            _punctuationTestRepository = punctuationTestRepository;
            _orthoeopyTestRepository = orthoeopyTestRepository;
            _spellingQuestionRepository = spellingQuestionRepository;
            _regularQuestionRepository = regularQuestionRepository;
            _regularQuestionOptionRepository = regularQuestionOptionRepository;
            _punctuationQuestionRepository = punctuationQuestionRepository;
            _orthoeopyQuestionRepository = orthoeopyQuestionRepository;
            _spellingTestResultRepository = spellingTestResultRepository;
            _regularTestResultRepository = regularTestResultRepository;
            _punctuationTestResultRepository = punctuationTestResultRepository;
            _orthoeopyTestResultRepository = orthoeopyTestResultRepository;
            _spellingAnswerRepository = spellingAnswerRepository;
            _regularAnswerRepository = regularAnswerRepository;
            _punctuationAnswerRepository = punctuationAnswerRepository;
            _orthoeopyAnswerRepository = orthoeopyAnswerRepository;
            _spellingTestClassRepository = spellingTestClassRepository;
            _regularTestClassRepository = regularTestClassRepository;
            _punctuationTestClassRepository = punctuationTestClassRepository;
            _orthoeopyTestClassRepository = orthoeopyTestClassRepository;
            _classRepository = classRepository;
            _studentRepository = studentRepository;
            _db = db;
        }

        // GET: TestAnalytics/Spelling/5 - Аналитика теста по орфографии
        public async Task<IActionResult> Spelling(int id)
        {
            var currentUser = await _userManager.GetUserAsync(User);
            var test = await _spellingTestRepository.GetByIdAsync(id);
            
            if (test == null || test.TeacherId != currentUser.Id) return NotFound();

            var analytics = await BuildSpellingAnalyticsAsync(test);
            return View("SpellingAnalytics", analytics);
        }

        // GET: TestAnalytics/Regular/1 - Аналитика обычного теста
        public async Task<IActionResult> Regular(int id)
        {
            var currentUser = await _userManager.GetUserAsync(User);
            var test = await _regularTestRepository.GetByIdAsync(id);
            
            if (test == null || test.TeacherId != currentUser.Id) return NotFound();

            var analytics = await BuildRegularTestAnalyticsAsync(test);
            return View("RegularTestAnalytics", analytics);
        }

        // GET: TestAnalytics/Orthoeopy/6 - Аналитика теста орфоэпии
        public async Task<IActionResult> Orthoeopy(int id)
        {
            var currentUser = await _userManager.GetUserAsync(User);
            var test = await _orthoeopyTestRepository.GetByIdAsync(id);
            
            if (test == null || test.TeacherId != currentUser.Id) return NotFound();

            var analytics = await BuildOrthoeopyAnalyticsAsync(test);
            return View("OrthoeopyTestAnalytics", analytics);
        }

        // GET: TestAnalytics/Punctuation/5 - Аналитика теста по пунктуации
        public async Task<IActionResult> Punctuation(int id)
        {
            var currentUser = await _userManager.GetUserAsync(User);
            var test = await _punctuationTestRepository.GetByIdAsync(id);
            
            if (test == null || test.TeacherId != currentUser.Id) return NotFound();

            var analytics = await BuildPunctuationAnalyticsAsync(test);
            return View("PunctuationAnalytics", analytics);
        }

        #region Spelling Test Analytics

        // Методы для тестов по орфографии
        private async Task<SpellingTestAnalyticsViewModel> BuildSpellingAnalyticsAsync(SpellingTest test)
        {
            var analytics = new SpellingTestAnalyticsViewModel
            {
                SpellingTest = test
            };

            var allStudents = new List<Student>();

            // Получаем студентов из всех назначенных классов через репозитории
            var testClasses = await _spellingTestClassRepository.GetByTestIdAsync(test.Id);
            if (testClasses.Count > 0)
            {
                // Тест назначен конкретным классам - берем студентов из этих классов
                var classIds = await _spellingTestClassRepository.GetClassIdsByTestIdAsync(test.Id);
                allStudents = await _classRepository.GetStudentsByClassIdsAsync(classIds);
                // Удаляем дубликаты
                allStudents = allStudents.GroupBy(s => s.Id).Select(g => g.First()).ToList();
            }
            else
            {
                // Тест доступен всем - берем студентов из всех классов учителя
                var teacherClasses = await _classRepository.GetByTeacherIdAsync(test.TeacherId);
                if (teacherClasses.Count > 0)
                {
                    var teacherClassIds = teacherClasses.Select(c => c.Id).ToList();
                    allStudents = await _classRepository.GetStudentsByClassIdsAsync(teacherClassIds);
                }
            }

            // Получаем вопросы теста
            var questions = await _spellingQuestionRepository.GetByTestIdOrderedAsync(test.Id);

            analytics.Statistics = await BuildSpellingStatisticsAsync(test, allStudents);
            analytics.SpellingResults = await BuildSpellingStudentResultsAsync(test, allStudents);
            analytics.SpellingQuestionAnalytics = await BuildSpellingQuestionAnalyticsAsync(test, questions);

            // Студенты, которые не проходили тест через репозиторий
            var studentsWithResults = await _spellingTestResultRepository.GetDistinctStudentIdsByTestIdAsync(test.Id);
            var studentsWithResultsSet = new HashSet<int>(studentsWithResults);
            analytics.StudentsNotTaken = allStudents.Where(s => !studentsWithResultsSet.Contains(s.Id)).ToList();

            return analytics;
        }

        private async Task<SpellingTestStatistics> BuildSpellingStatisticsAsync(SpellingTest test, List<Student> allStudents)
        {
            // Получаем статистику через репозиторий
            var studentsWithResults = await _spellingTestResultRepository.GetDistinctStudentsCountByTestIdAsync(test.Id);
            var studentsCompleted = await _spellingTestResultRepository.GetCompletedStudentsCountByTestIdAsync(test.Id);
            var studentsInProgress = await _spellingTestResultRepository.GetInProgressStudentsCountByTestIdAsync(test.Id);

            var stats = new SpellingTestStatistics
            {
                TotalStudents = allStudents.Count,
                StudentsCompleted = studentsCompleted,
                StudentsInProgress = studentsInProgress,
                StudentsNotStarted = allStudents.Count - studentsWithResults
            };

            var completedCount = await _spellingTestResultRepository.GetCountByTestIdAsync(test.Id);
            
            if (completedCount > 0)
            {
                stats.AverageScore = Math.Round(await _spellingTestResultRepository.GetAverageScoreByTestIdAsync(test.Id), 1);
                stats.AveragePercentage = Math.Round(await _spellingTestResultRepository.GetAveragePercentageByTestIdAsync(test.Id), 1);
                stats.HighestScore = await _spellingTestResultRepository.GetHighestScoreByTestIdAsync(test.Id);
                stats.LowestScore = await _spellingTestResultRepository.GetLowestScoreByTestIdAsync(test.Id);
                stats.FirstCompletion = await _spellingTestResultRepository.GetFirstCompletionByTestIdAsync(test.Id);
                stats.LastCompletion = await _spellingTestResultRepository.GetLastCompletionByTestIdAsync(test.Id);

                var avgSeconds = await _spellingTestResultRepository.GetAverageCompletionTimeByTestIdAsync(test.Id);
                if (avgSeconds.HasValue)
                {
                    stats.AverageCompletionTime = TimeSpan.FromSeconds(avgSeconds.Value);
                }

                stats.GradeDistribution = await _spellingTestResultRepository.GetGradeDistributionByTestIdAsync(test.Id);
            }

            return stats;
        }

        private async Task<List<SpellingTestResultViewModel>> BuildSpellingStudentResultsAsync(SpellingTest test, List<Student> allStudents)
        {
            var studentResults = new List<SpellingTestResultViewModel>();

            foreach (var student in allStudents)
            {
                // Получаем результаты студента через репозиторий
                var results = await _spellingTestResultRepository.GetByStudentAndTestIdAsync(student.Id, test.Id);
                var completedResults = await _spellingTestResultRepository.GetCompletedByStudentAndTestIdAsync(student.Id, test.Id);
                var hasCompleted = await _spellingTestResultRepository.HasCompletedByStudentAndTestIdAsync(student.Id, test.Id);
                var isInProgress = await _spellingTestResultRepository.IsInProgressByStudentAndTestIdAsync(student.Id, test.Id);

                var studentResult = new SpellingTestResultViewModel
                {
                    Student = student,
                    Results = results,
                    AttemptsUsed = results.Count,
                    HasCompleted = hasCompleted,
                    IsInProgress = isInProgress
                };

                if (completedResults.Count > 0)
                {
                    studentResult.BestResult = await _spellingTestResultRepository.GetBestResultByStudentAndTestIdAsync(student.Id, test.Id);
                    studentResult.LatestResult = await _spellingTestResultRepository.GetLatestResultByStudentAndTestIdAsync(student.Id, test.Id);

                    var totalSeconds = await _spellingTestResultRepository.GetTotalTimeSpentByStudentAndTestIdAsync(student.Id, test.Id);
                    if (totalSeconds.HasValue && totalSeconds.Value > 0)
                    {
                        studentResult.TotalTimeSpent = TimeSpan.FromSeconds(totalSeconds.Value);
                    }
                }

                studentResults.Add(studentResult);
            }

            // Сортируем в памяти по фамилии
            studentResults.Sort((a, b) => 
            {
                var userA = _userManager.FindByIdAsync(a.Student.UserId).Result;
                var userB = _userManager.FindByIdAsync(b.Student.UserId).Result;
                var lastNameA = userA?.LastName ?? "";
                var lastNameB = userB?.LastName ?? "";
                return string.Compare(lastNameA, lastNameB, StringComparison.Ordinal);
            });

            return studentResults;
        }

        private async Task<List<SpellingQuestionAnalyticsViewModel>> BuildSpellingQuestionAnalyticsAsync(SpellingTest test, List<SpellingQuestion> questions)
        {
            var questionAnalytics = new List<SpellingQuestionAnalyticsViewModel>();

            foreach (var question in questions)
            {
                // Получаем статистику ответов через репозиторий
                var totalAnswers = await _spellingAnswerRepository.GetTotalCountByQuestionIdAsync(question.Id);
                var correctAnswers = await _spellingAnswerRepository.GetCorrectCountByQuestionIdAsync(question.Id);
                var incorrectAnswers = totalAnswers - correctAnswers;

                var analytics = new SpellingQuestionAnalyticsViewModel
                {
                    SpellingQuestion = question,
                    TotalAnswers = totalAnswers,
                    CorrectAnswers = correctAnswers,
                    IncorrectAnswers = incorrectAnswers
                };

                if (totalAnswers > 0)
                {
                    analytics.SuccessRate = Math.Round((double)analytics.CorrectAnswers / analytics.TotalAnswers * 100, 1);

                    // Получаем частые ошибки через репозиторий
                    var mistakes = await _spellingAnswerRepository.GetCommonMistakesByQuestionIdAsync(question.Id, 5);
                    
                    var incorrectAnswersList = mistakes.Select(m => new CommonMistakeViewModel
                    {
                        IncorrectAnswer = m.IncorrectAnswer,
                        Count = m.Count,
                        Percentage = Math.Round((double)m.Count / analytics.IncorrectAnswers * 100, 1),
                        StudentNames = new List<string>()
                    }).ToList();

                    analytics.CommonMistakes = incorrectAnswersList;
                }

                questionAnalytics.Add(analytics);
            }

            if (questionAnalytics.Any(qa => qa.TotalAnswers > 0))
            {
                var lowestSuccessRate = questionAnalytics.Where(qa => qa.TotalAnswers > 0).Min(qa => qa.SuccessRate);
                var highestSuccessRate = questionAnalytics.Where(qa => qa.TotalAnswers > 0).Max(qa => qa.SuccessRate);

                foreach (var qa in questionAnalytics)
                {
                    if (qa.TotalAnswers > 0)
                    {
                        qa.IsMostDifficult = qa.SuccessRate == lowestSuccessRate;
                        qa.IsEasiest = qa.SuccessRate == highestSuccessRate;
                    }
                }
            }

            return questionAnalytics;
        }

        #endregion

        #region Punctuation Test Analytics

        // Методы для тестов по пунктуации
        private async Task<PunctuationTestAnalyticsViewModel> BuildPunctuationAnalyticsAsync(PunctuationTest test)
        {
            var analytics = new PunctuationTestAnalyticsViewModel
            {
                PunctuationTest = test
            };

            var allStudents = new List<Student>();

            // Получаем студентов из всех назначенных классов через репозитории
            var testClasses = await _punctuationTestClassRepository.GetByTestIdAsync(test.Id);
            if (testClasses.Count > 0)
            {
                var classIds = await _punctuationTestClassRepository.GetClassIdsByTestIdAsync(test.Id);
                allStudents = await _classRepository.GetStudentsByClassIdsAsync(classIds);
                allStudents = allStudents.GroupBy(s => s.Id).Select(g => g.First()).ToList();
            }
            else
            {
                var teacherClasses = await _classRepository.GetByTeacherIdAsync(test.TeacherId);
                if (teacherClasses.Count > 0)
                {
                    var teacherClassIds = teacherClasses.Select(c => c.Id).ToList();
                    allStudents = await _classRepository.GetStudentsByClassIdsAsync(teacherClassIds);
                }
            }

            var questions = await _punctuationQuestionRepository.GetByTestIdOrderedAsync(test.Id);

            analytics.Statistics = await BuildPunctuationStatisticsAsync(test, allStudents);
            analytics.StudentResults = await BuildPunctuationStudentResultsAsync(test, allStudents);
            analytics.QuestionAnalytics = await BuildPunctuationQuestionAnalyticsAsync(test, questions);

            // Студенты, которые не проходили тест через репозиторий
            var studentsWithResults = await _punctuationTestResultRepository.GetDistinctStudentIdsByTestIdAsync(test.Id);
            var studentsWithResultsSet = new HashSet<int>(studentsWithResults);
            analytics.StudentsNotTaken = allStudents.Where(s => !studentsWithResultsSet.Contains(s.Id)).ToList();

            return analytics;
        }

        private async Task<PunctuationTestStatistics> BuildPunctuationStatisticsAsync(PunctuationTest test, List<Student> allStudents)
        {
            var studentsWithResults = await _punctuationTestResultRepository.GetDistinctStudentsCountByTestIdAsync(test.Id);
            var studentsCompleted = await _punctuationTestResultRepository.GetCompletedStudentsCountByTestIdAsync(test.Id);
            var studentsInProgress = await _punctuationTestResultRepository.GetInProgressStudentsCountByTestIdAsync(test.Id);

            var stats = new PunctuationTestStatistics
            {
                TotalStudents = allStudents.Count,
                StudentsCompleted = studentsCompleted,
                StudentsInProgress = studentsInProgress,
                StudentsNotStarted = allStudents.Count - studentsWithResults
            };

            var completedCount = await _punctuationTestResultRepository.GetCountByTestIdAsync(test.Id);
            
            if (completedCount > 0)
            {
                stats.AverageScore = Math.Round(await _punctuationTestResultRepository.GetAverageScoreByTestIdAsync(test.Id), 1);
                stats.AveragePercentage = Math.Round(await _punctuationTestResultRepository.GetAveragePercentageByTestIdAsync(test.Id), 1);
                stats.HighestScore = await _punctuationTestResultRepository.GetHighestScoreByTestIdAsync(test.Id);
                stats.LowestScore = await _punctuationTestResultRepository.GetLowestScoreByTestIdAsync(test.Id);
                stats.FirstCompletion = await _punctuationTestResultRepository.GetFirstCompletionByTestIdAsync(test.Id);
                stats.LastCompletion = await _punctuationTestResultRepository.GetLastCompletionByTestIdAsync(test.Id);

                var avgSeconds = await _punctuationTestResultRepository.GetAverageCompletionTimeByTestIdAsync(test.Id);
                if (avgSeconds.HasValue)
                {
                    stats.AverageCompletionTime = TimeSpan.FromSeconds(avgSeconds.Value);
                }

                stats.GradeDistribution = await _punctuationTestResultRepository.GetGradeDistributionByTestIdAsync(test.Id);
            }

            return stats;
        }

        private async Task<List<PunctuationStudentResultViewModel>> BuildPunctuationStudentResultsAsync(PunctuationTest test, List<Student> allStudents)
        {
            var studentResults = new List<PunctuationStudentResultViewModel>();

            foreach (var student in allStudents)
            {
                var results = await _punctuationTestResultRepository.GetByStudentAndTestIdAsync(student.Id, test.Id);
                var completedResults = await _punctuationTestResultRepository.GetCompletedByStudentAndTestIdAsync(student.Id, test.Id);
                var hasCompleted = await _punctuationTestResultRepository.HasCompletedByStudentAndTestIdAsync(student.Id, test.Id);
                var isInProgress = await _punctuationTestResultRepository.IsInProgressByStudentAndTestIdAsync(student.Id, test.Id);

                var studentResult = new PunctuationStudentResultViewModel
                {
                    Student = student,
                    Results = results,
                    AttemptsUsed = results.Count,
                    HasCompleted = hasCompleted,
                    IsInProgress = isInProgress
                };

                if (completedResults.Count > 0)
                {
                    studentResult.BestResult = await _punctuationTestResultRepository.GetBestResultByStudentAndTestIdAsync(student.Id, test.Id);
                    studentResult.LatestResult = await _punctuationTestResultRepository.GetLatestResultByStudentAndTestIdAsync(student.Id, test.Id);

                    var totalSeconds = await _punctuationTestResultRepository.GetTotalTimeSpentByStudentAndTestIdAsync(student.Id, test.Id);
                    if (totalSeconds.HasValue && totalSeconds.Value > 0)
                    {
                        studentResult.TotalTimeSpent = TimeSpan.FromSeconds(totalSeconds.Value);
                    }
                }

                studentResults.Add(studentResult);
            }

            // Сортируем в памяти по фамилии
            studentResults.Sort((a, b) => 
            {
                var userA = _userManager.FindByIdAsync(a.Student.UserId).Result;
                var userB = _userManager.FindByIdAsync(b.Student.UserId).Result;
                var lastNameA = userA?.LastName ?? "";
                var lastNameB = userB?.LastName ?? "";
                return string.Compare(lastNameA, lastNameB, StringComparison.Ordinal);
            });
            return studentResults;
        }

        private async Task<List<PunctuationQuestionAnalyticsViewModel>> BuildPunctuationQuestionAnalyticsAsync(PunctuationTest test, List<PunctuationQuestion> questions)
        {
            var questionAnalytics = new List<PunctuationQuestionAnalyticsViewModel>();

            foreach (var question in questions)
            {
                var totalAnswers = await _punctuationAnswerRepository.GetTotalCountByQuestionIdAsync(question.Id);
                var correctAnswers = await _punctuationAnswerRepository.GetCorrectCountByQuestionIdAsync(question.Id);
                var incorrectAnswers = totalAnswers - correctAnswers;

                var analytics = new PunctuationQuestionAnalyticsViewModel
                {
                    Question = question,
                    TotalAnswers = totalAnswers,
                    CorrectAnswers = correctAnswers,
                    IncorrectAnswers = incorrectAnswers
                };

                if (totalAnswers > 0)
                {
                    analytics.SuccessRate = Math.Round((double)analytics.CorrectAnswers / analytics.TotalAnswers * 100, 1);

                    var mistakes = await _punctuationAnswerRepository.GetCommonMistakesByQuestionIdAsync(question.Id, 5);
                    
                    var incorrectAnswersList = mistakes.Select(m => new CommonMistakeViewModel
                    {
                        IncorrectAnswer = m.IncorrectAnswer,
                        Count = m.Count,
                        Percentage = Math.Round((double)m.Count / analytics.IncorrectAnswers * 100, 1),
                        StudentNames = new List<string>()
                    }).ToList();

                    analytics.CommonMistakes = incorrectAnswersList;
                }

                questionAnalytics.Add(analytics);
            }

            if (questionAnalytics.Any(qa => qa.TotalAnswers > 0))
            {
                var lowestSuccessRate = questionAnalytics.Where(qa => qa.TotalAnswers > 0).Min(qa => qa.SuccessRate);
                var highestSuccessRate = questionAnalytics.Where(qa => qa.TotalAnswers > 0).Max(qa => qa.SuccessRate);

                foreach (var qa in questionAnalytics)
                {
                    if (qa.TotalAnswers > 0)
                    {
                        qa.IsMostDifficult = qa.SuccessRate == lowestSuccessRate;
                        qa.IsEasiest = qa.SuccessRate == highestSuccessRate;
                    }
                }
            }

            return questionAnalytics;
        }

        #endregion

        #region Regular Test Analytics

        // Методы для обычных тестов
        private async Task<RegularTestAnalyticsViewModel> BuildRegularTestAnalyticsAsync(RegularTest test)
        {
            var analytics = new RegularTestAnalyticsViewModel
            {
                RegularTest = test
            };

            var allStudents = new List<Student>();

            // Получаем студентов из всех назначенных классов через репозитории
            var testClasses = await _regularTestClassRepository.GetByTestIdAsync(test.Id);
            if (testClasses.Count > 0)
            {
                var classIds = await _regularTestClassRepository.GetClassIdsByTestIdAsync(test.Id);
                allStudents = await _classRepository.GetStudentsByClassIdsAsync(classIds);
                allStudents = allStudents.GroupBy(s => s.Id).Select(g => g.First()).ToList();
            }
            else
            {
                var teacherClasses = await _classRepository.GetByTeacherIdAsync(test.TeacherId);
                if (teacherClasses.Count > 0)
                {
                    var teacherClassIds = teacherClasses.Select(c => c.Id).ToList();
                    allStudents = await _classRepository.GetStudentsByClassIdsAsync(teacherClassIds);
                }
            }

            var questions = await _regularQuestionRepository.GetByTestIdOrderedAsync(test.Id);

            analytics.Statistics = await BuildRegularTestStatisticsAsync(test, allStudents);
            analytics.RegularResults = await BuildRegularTestStudentResultsAsync(test, allStudents);
            analytics.QuestionAnalytics = await BuildRegularTestQuestionAnalyticsAsync(test, questions);

            // Студенты, которые не проходили тест через репозиторий
            var studentsWithResults = await _regularTestResultRepository.GetDistinctStudentIdsByTestIdAsync(test.Id);
            var studentsWithResultsSet = new HashSet<int>(studentsWithResults);
            analytics.StudentsNotTaken = allStudents.Where(s => !studentsWithResultsSet.Contains(s.Id)).ToList();

            return analytics;
        }

        private async Task<RegularTestStatistics> BuildRegularTestStatisticsAsync(RegularTest test, List<Student> allStudents)
        {
            var studentsWithResults = await _regularTestResultRepository.GetDistinctStudentsCountByTestIdAsync(test.Id);
            var studentsCompleted = await _regularTestResultRepository.GetCompletedStudentsCountByTestIdAsync(test.Id);
            var studentsInProgress = await _regularTestResultRepository.GetInProgressStudentsCountByTestIdAsync(test.Id);

            var stats = new RegularTestStatistics
            {
                TotalStudents = allStudents.Count,
                StudentsCompleted = studentsCompleted,
                StudentsInProgress = studentsInProgress,
                StudentsNotStarted = allStudents.Count - studentsWithResults
            };

            var completedCount = await _regularTestResultRepository.GetCountByTestIdAsync(test.Id);
            
            if (completedCount > 0)
            {
                stats.AverageScore = Math.Round(await _regularTestResultRepository.GetAverageScoreByTestIdAsync(test.Id), 1);
                stats.AveragePercentage = Math.Round(await _regularTestResultRepository.GetAveragePercentageByTestIdAsync(test.Id), 1);
                stats.HighestScore = await _regularTestResultRepository.GetHighestScoreByTestIdAsync(test.Id);
                stats.LowestScore = await _regularTestResultRepository.GetLowestScoreByTestIdAsync(test.Id);
                stats.FirstCompletion = await _regularTestResultRepository.GetFirstCompletionByTestIdAsync(test.Id);
                stats.LastCompletion = await _regularTestResultRepository.GetLastCompletionByTestIdAsync(test.Id);

                var avgSeconds = await _regularTestResultRepository.GetAverageCompletionTimeByTestIdAsync(test.Id);
                if (avgSeconds.HasValue)
                {
                    stats.AverageCompletionTime = TimeSpan.FromSeconds(avgSeconds.Value);
                }

                stats.GradeDistribution = await _regularTestResultRepository.GetGradeDistributionByTestIdAsync(test.Id);
            }

            return stats;
        }

        private async Task<List<RegularTestStudentResultViewModel>> BuildRegularTestStudentResultsAsync(RegularTest test, List<Student> allStudents)
        {
            var studentResults = new List<RegularTestStudentResultViewModel>();

            foreach (var student in allStudents)
            {
                var results = await _regularTestResultRepository.GetByStudentAndTestIdAsync(student.Id, test.Id);
                var completedResults = await _regularTestResultRepository.GetCompletedByStudentAndTestIdAsync(student.Id, test.Id);
                var hasCompleted = await _regularTestResultRepository.HasCompletedByStudentAndTestIdAsync(student.Id, test.Id);
                var isInProgress = await _regularTestResultRepository.IsInProgressByStudentAndTestIdAsync(student.Id, test.Id);

                var studentResult = new RegularTestStudentResultViewModel
                {
                    Student = student,
                    Results = results,
                    AttemptsUsed = results.Count,
                    HasCompleted = hasCompleted,
                    IsInProgress = isInProgress
                };

                if (completedResults.Count > 0)
                {
                    studentResult.BestResult = await _regularTestResultRepository.GetBestResultByStudentAndTestIdAsync(student.Id, test.Id);
                    studentResult.LatestResult = await _regularTestResultRepository.GetLatestResultByStudentAndTestIdAsync(student.Id, test.Id);

                    var totalSeconds = await _regularTestResultRepository.GetTotalTimeSpentByStudentAndTestIdAsync(student.Id, test.Id);
                    if (totalSeconds.HasValue && totalSeconds.Value > 0)
                    {
                        studentResult.TotalTimeSpent = TimeSpan.FromSeconds(totalSeconds.Value);
                    }
                }

                studentResults.Add(studentResult);
            }

            // Сортируем в памяти по фамилии
            studentResults.Sort((a, b) => 
            {
                var lastNameA = a.Student.User?.LastName ?? "";
                var lastNameB = b.Student.User?.LastName ?? "";
                return string.Compare(lastNameA, lastNameB, StringComparison.Ordinal);
            });
            return studentResults;
        }

        private async Task<List<RegularTestQuestionAnalyticsViewModel>> BuildRegularTestQuestionAnalyticsAsync(RegularTest test, List<RegularQuestion> questions)
        {
            var questionAnalytics = new List<RegularTestQuestionAnalyticsViewModel>();

            foreach (var question in questions)
            {
                // Получаем статистику ответов через SQL
                var totalAnswers = await _regularAnswerRepository.GetTotalCountByQuestionIdAsync(question.Id);
                var correctAnswers = await _regularAnswerRepository.GetCorrectCountByQuestionIdAsync(question.Id);
                
                var incorrectAnswers = totalAnswers - correctAnswers;

                var analytics = new RegularTestQuestionAnalyticsViewModel
                {
                    RegularQuestion = question,
                    TotalAnswers = totalAnswers,
                    CorrectAnswers = correctAnswers,
                    IncorrectAnswers = incorrectAnswers
                };

                if (totalAnswers > 0)
                {
                    analytics.SuccessRate = Math.Round((double)analytics.CorrectAnswers / analytics.TotalAnswers * 100, 1);

                    // Получаем опции вопроса
                    var questionOptions = await _regularQuestionOptionRepository.GetByQuestionIdOrderedAsync(question.Id);
                    
                    // Получаем все ответы для этого вопроса через SQL
                    var allAnswersSql = "SELECT * FROM RegularAnswers WHERE QuestionId = @QuestionId";
                    var allAnswers = await _db.QueryAsync<RegularAnswer>(allAnswersSql, new { QuestionId = question.Id });
                    
                    var optionStats = new Dictionary<int, int>();

                    foreach (var answer in allAnswers)
                    {
                        if (!string.IsNullOrEmpty(answer.SelectedOptionIds))
                        {
                            var selectedIds = answer.SelectedOptionIds.Split(',', StringSplitOptions.RemoveEmptyEntries)
                                .Select(id => int.Parse(id.Trim()));

                            foreach (var optionId in selectedIds)
                            {
                                if (!optionStats.ContainsKey(optionId))
                                {
                                    optionStats[optionId] = 0;
                                }
                                optionStats[optionId]++;
                            }
                        }
                    }

                    // Фильтруем неправильные опции
                    var incorrectOptions = new List<CommonMistakeViewModel>();
                    foreach (var option in questionOptions)
                    {
                        if (!option.IsCorrect && optionStats.ContainsKey(option.Id))
                        {
                            incorrectOptions.Add(new CommonMistakeViewModel
                            {
                                IncorrectAnswer = option.Text,
                                Count = optionStats[option.Id],
                                Percentage = Math.Round((double)optionStats[option.Id] / analytics.TotalAnswers * 100, 1),
                                StudentNames = new List<string>() // Упрощаем для производительности
                            });
                        }
                    }
                    
                    // Сортируем и берем топ-5
                    incorrectOptions.Sort((a, b) => b.Count.CompareTo(a.Count));
                    analytics.CommonMistakes = incorrectOptions.Take(5).ToList();
                }

                questionAnalytics.Add(analytics);
            }

            if (questionAnalytics.Any(qa => qa.TotalAnswers > 0))
            {
                var lowestSuccessRate = questionAnalytics.Where(qa => qa.TotalAnswers > 0).Min(qa => qa.SuccessRate);
                var highestSuccessRate = questionAnalytics.Where(qa => qa.TotalAnswers > 0).Max(qa => qa.SuccessRate);

                foreach (var qa in questionAnalytics)
                {
                    if (qa.TotalAnswers > 0)
                    {
                        qa.IsMostDifficult = qa.SuccessRate == lowestSuccessRate;
                        qa.IsEasiest = qa.SuccessRate == highestSuccessRate;
                    }
                }
            }

            return questionAnalytics;
        }

        #endregion

        #region Orthoepy Test Analytics

        private async Task<OrthoeopyTestAnalyticsViewModel> BuildOrthoeopyAnalyticsAsync(OrthoeopyTest test)
        {
            var analytics = new OrthoeopyTestAnalyticsViewModel
            {
                OrthoeopyTest = test
            };

            var allStudents = new List<Student>();

            // Получаем студентов из всех назначенных классов через репозитории
            var testClasses = await _orthoeopyTestClassRepository.GetByTestIdAsync(test.Id);
            if (testClasses.Count > 0)
            {
                var classIds = await _orthoeopyTestClassRepository.GetClassIdsByTestIdAsync(test.Id);
                allStudents = await _classRepository.GetStudentsByClassIdsAsync(classIds);
                allStudents = allStudents.GroupBy(s => s.Id).Select(g => g.First()).ToList();
            }
            else
            {
                var teacherClasses = await _classRepository.GetByTeacherIdAsync(test.TeacherId);
                if (teacherClasses.Count > 0)
                {
                    var teacherClassIds = teacherClasses.Select(c => c.Id).ToList();
                    allStudents = await _classRepository.GetStudentsByClassIdsAsync(teacherClassIds);
                }
            }

            var questions = await _orthoeopyQuestionRepository.GetByTestIdOrderedAsync(test.Id);

            analytics.Statistics = await BuildOrthoeopyStatisticsAsync(test, allStudents);
            analytics.StudentResults = await BuildOrthoeopyStudentResultsAsync(test, allStudents);
            analytics.QuestionAnalytics = await BuildOrthoeopyQuestionAnalyticsAsync(test, questions);
            
            // Студенты, которые не проходили тест через репозиторий
            var studentsWithResults = await _orthoeopyTestResultRepository.GetDistinctStudentIdsByTestIdAsync(test.Id);
            var studentsWithResultsSet = new HashSet<int>(studentsWithResults);
            analytics.StudentsNotTaken = allStudents.Where(s => !studentsWithResultsSet.Contains(s.Id)).ToList();

            return analytics;
        }

        private async Task<OrthoeopyTestStatistics> BuildOrthoeopyStatisticsAsync(OrthoeopyTest test, List<Student> allStudents)
        {
            var studentsWithResults = await _orthoeopyTestResultRepository.GetDistinctStudentsCountByTestIdAsync(test.Id);
            var studentsCompleted = await _orthoeopyTestResultRepository.GetCompletedStudentsCountByTestIdAsync(test.Id);
            var studentsInProgress = await _orthoeopyTestResultRepository.GetInProgressStudentsCountByTestIdAsync(test.Id);

            var stats = new OrthoeopyTestStatistics
            {
                TotalStudents = allStudents.Count,
                StudentsCompleted = studentsCompleted,
                StudentsInProgress = studentsInProgress,
                StudentsNotStarted = allStudents.Count - studentsWithResults
            };

            var completedCount = await _orthoeopyTestResultRepository.GetCountByTestIdAsync(test.Id);
            
            if (completedCount > 0)
            {
                stats.AverageScore = Math.Round(await _orthoeopyTestResultRepository.GetAverageScoreByTestIdAsync(test.Id), 1);
                stats.AveragePercentage = Math.Round(await _orthoeopyTestResultRepository.GetAveragePercentageByTestIdAsync(test.Id), 1);
                stats.HighestScore = await _orthoeopyTestResultRepository.GetHighestScoreByTestIdAsync(test.Id);
                stats.LowestScore = await _orthoeopyTestResultRepository.GetLowestScoreByTestIdAsync(test.Id);
                stats.FirstCompletion = await _orthoeopyTestResultRepository.GetFirstCompletionByTestIdAsync(test.Id);
                stats.LastCompletion = await _orthoeopyTestResultRepository.GetLastCompletionByTestIdAsync(test.Id);

                var avgSeconds = await _orthoeopyTestResultRepository.GetAverageCompletionTimeByTestIdAsync(test.Id);
                if (avgSeconds.HasValue)
                {
                    stats.AverageCompletionTime = TimeSpan.FromSeconds(avgSeconds.Value);
                }

                stats.GradeDistribution = await _orthoeopyTestResultRepository.GetGradeDistributionByTestIdAsync(test.Id);
            }

            return stats;
        }

        private async Task<List<OrthoeopyStudentResultViewModel>> BuildOrthoeopyStudentResultsAsync(OrthoeopyTest test, List<Student> allStudents)
        {
            var studentResults = new List<OrthoeopyStudentResultViewModel>();

            foreach (var student in allStudents)
            {
                var results = await _orthoeopyTestResultRepository.GetByStudentAndTestIdAsync(student.Id, test.Id);
                var completedResults = await _orthoeopyTestResultRepository.GetCompletedByStudentAndTestIdAsync(student.Id, test.Id);
                var hasCompleted = await _orthoeopyTestResultRepository.HasCompletedByStudentAndTestIdAsync(student.Id, test.Id);
                var isInProgress = await _orthoeopyTestResultRepository.IsInProgressByStudentAndTestIdAsync(student.Id, test.Id);

                var studentResult = new OrthoeopyStudentResultViewModel
                {
                    Student = student,
                    Results = results,
                    AttemptsUsed = results.Count,
                    HasCompleted = hasCompleted,
                    IsInProgress = isInProgress
                };

                if (completedResults.Count > 0)
                {
                    studentResult.BestResult = await _orthoeopyTestResultRepository.GetBestResultByStudentAndTestIdAsync(student.Id, test.Id);
                    studentResult.LatestResult = await _orthoeopyTestResultRepository.GetLatestResultByStudentAndTestIdAsync(student.Id, test.Id);

                    var totalSeconds = await _orthoeopyTestResultRepository.GetTotalTimeSpentByStudentAndTestIdAsync(student.Id, test.Id);
                    if (totalSeconds.HasValue && totalSeconds.Value > 0)
                    {
                        studentResult.TotalTimeSpent = TimeSpan.FromSeconds(totalSeconds.Value);
                    }
                }

                studentResults.Add(studentResult);
            }

            // Сортируем в памяти по фамилии
            studentResults.Sort((a, b) => 
            {
                var lastNameA = a.Student.User?.LastName ?? "";
                var lastNameB = b.Student.User?.LastName ?? "";
                return string.Compare(lastNameA, lastNameB, StringComparison.Ordinal);
            });
            return studentResults;
        }

        private async Task<List<OrthoeopyQuestionAnalyticsViewModel>> BuildOrthoeopyQuestionAnalyticsAsync(OrthoeopyTest test, List<OrthoeopyQuestion> questions)
        {
            var questionAnalytics = new List<OrthoeopyQuestionAnalyticsViewModel>();

            foreach (var question in questions)
            {
                var totalAnswers = await _orthoeopyAnswerRepository.GetTotalCountByQuestionIdAsync(question.Id);
                var correctAnswers = await _orthoeopyAnswerRepository.GetCorrectCountByQuestionIdAsync(question.Id);
                
                var incorrectAnswers = totalAnswers - correctAnswers;

                var analytics = new OrthoeopyQuestionAnalyticsViewModel
                {
                    Question = question,
                    TotalAnswers = totalAnswers,
                    CorrectAnswers = correctAnswers,
                    IncorrectAnswers = incorrectAnswers
                };

                if (totalAnswers > 0)
                {
                    analytics.SuccessRate = Math.Round((double)analytics.CorrectAnswers / analytics.TotalAnswers * 100, 1);

                    // Получаем частые ошибки по позиции ударения через репозиторий
                    var mistakes = await _orthoeopyAnswerRepository.GetCommonMistakesByQuestionIdAsync(question.Id, 5);
                    
                    var incorrectAnswersList = mistakes.Select(m => new StressPositionMistakeViewModel
                    {
                        IncorrectPosition = int.TryParse(m.IncorrectAnswer, out var pos) ? pos : 0,
                        Count = m.Count,
                        Percentage = Math.Round((double)m.Count / analytics.IncorrectAnswers * 100, 1),
                        StudentNames = new List<string>() // Упрощаем для производительности
                    }).ToList();

                    analytics.CommonMistakes = incorrectAnswersList;
                }

                questionAnalytics.Add(analytics);
            }

            if (questionAnalytics.Any(qa => qa.TotalAnswers > 0))
            {
                var lowestSuccessRate = questionAnalytics.Where(qa => qa.TotalAnswers > 0).Min(qa => qa.SuccessRate);
                var highestSuccessRate = questionAnalytics.Where(qa => qa.TotalAnswers > 0).Max(qa => qa.SuccessRate);

                foreach (var qa in questionAnalytics)
                {
                    if (qa.TotalAnswers > 0)
                    {
                        qa.IsMostDifficult = qa.SuccessRate == lowestSuccessRate;
                        qa.IsEasiest = qa.SuccessRate == highestSuccessRate;
                    }
                }
            }

            return questionAnalytics;
        }

        #endregion

        #region Student Details API

        // Метод для получения детальной информации о студенте (пунктуация)
        [HttpGet]
        public async Task<IActionResult> GetPunctuationStudentDetails(int studentId, int testId)
        {
            var currentUser = await _userManager.GetUserAsync(User);

            var test = await _punctuationTestRepository.GetByIdAsync(testId);
            if (test == null || test.TeacherId != currentUser.Id)
                return NotFound();

            var student = await _studentRepository.GetWithUserAsync(studentId);
            if (student == null)
                return NotFound();

            // Получаем завершенные результаты студента через SQL
            var resultsSql = "SELECT * FROM PunctuationTestResults WHERE StudentId = @StudentId AND PunctuationTestId = @TestId AND IsCompleted = 1 ORDER BY CompletedAt DESC";
            var results = await _db.QueryAsync<PunctuationTestResult>(resultsSql, new { StudentId = studentId, TestId = testId });

            // Получаем лучший результат через SQL
            var bestResultSql = "SELECT TOP 1 * FROM PunctuationTestResults WHERE StudentId = @StudentId AND PunctuationTestId = @TestId AND IsCompleted = 1 ORDER BY Percentage DESC";
            var bestResults = await _db.QueryAsync<PunctuationTestResult>(bestResultSql, new { StudentId = studentId, TestId = testId });
            var bestResult = bestResults.FirstOrDefault();

            // Получаем частые ошибки через SQL
            var mistakesSql = @"
                SELECT TOP 10
                    ISNULL(pa.StudentAnswer, 'Пустой ответ') as IncorrectAnswer,
                    ISNULL(pq.CorrectPositions, 'Без запятых') as CorrectAnswer,
                    pq.SentenceWithNumbers,
                    COUNT(*) as Count
                FROM PunctuationAnswers pa
                LEFT JOIN PunctuationQuestions pq ON pa.PunctuationQuestionId = pq.Id
                WHERE pa.PunctuationTestResultId IN (SELECT Id FROM PunctuationTestResults WHERE StudentId = @StudentId AND PunctuationTestId = @TestId AND IsCompleted = 1)
                    AND pa.IsCorrect = 0
                GROUP BY ISNULL(pa.StudentAnswer, 'Пустой ответ'), ISNULL(pq.CorrectPositions, 'Без запятых'), pq.SentenceWithNumbers
                ORDER BY COUNT(*) DESC";
            var mistakes = await _db.QueryAsync<dynamic>(mistakesSql, new { StudentId = studentId, TestId = testId });

            // Вычисляем общее время через SQL
            var totalTimeSql = @"
                SELECT SUM(DATEDIFF(SECOND, StartedAt, CompletedAt))
                FROM PunctuationTestResults 
                WHERE StudentId = @StudentId AND PunctuationTestId = @TestId AND IsCompleted = 1 AND CompletedAt IS NOT NULL";
            var totalSeconds = await _db.QueryScalarAsync<long?>(totalTimeSql, new { StudentId = studentId, TestId = testId });
            var totalTime = totalSeconds ?? 0;

            var response = new
            {
                FullName = student.User.FullName,
                School = student.School,
                ClassName = student.Class?.Name,
                AttemptsUsed = results.Count,
                MaxAttempts = test.MaxAttempts,
                BestResult = bestResult != null ? new
                {
                    Percentage = bestResult.Percentage,
                    Score = bestResult.Score,
                    MaxScore = bestResult.MaxScore
                } : null,
                TotalTimeSpent = totalTime > 0 ? new TimeSpan(totalTime).ToString(@"hh\:mm\:ss") : null,
                Attempts = results.Select(r => new
                {
                    AttemptNumber = r.AttemptNumber,
                    Percentage = r.Percentage,
                    Score = r.Score,
                    MaxScore = r.MaxScore,
                    Duration = r.CompletedAt.HasValue ? (r.CompletedAt.Value - r.StartedAt).ToString(@"mm\:ss") : null,
                    CompletedAt = r.CompletedAt
                }).ToList(),
                Mistakes = mistakes
            };

            return Json(response);
        }

        // GET: TestAnalytics/GetStudentDetails
        [HttpGet]
        public async Task<IActionResult> GetStudentDetails(int studentId, int testId)
        {
            var currentUser = await _userManager.GetUserAsync(User);

            var test = await _spellingTestRepository.GetByIdAsync(testId);
            if (test == null || test.TeacherId != currentUser.Id)
                return NotFound();

            var student = await _studentRepository.GetWithUserAsync(studentId);
            if (student == null)
                return NotFound();

            // Получаем завершенные результаты студента через SQL
            var resultsSql = "SELECT * FROM SpellingTestResults WHERE StudentId = @StudentId AND SpellingTestId = @TestId AND IsCompleted = 1 ORDER BY CompletedAt DESC";
            var results = await _db.QueryAsync<SpellingTestResult>(resultsSql, new { StudentId = studentId, TestId = testId });

            // Получаем лучший результат через SQL
            var bestResultSql = "SELECT TOP 1 * FROM SpellingTestResults WHERE StudentId = @StudentId AND SpellingTestId = @TestId AND IsCompleted = 1 ORDER BY Percentage DESC";
            var bestResults = await _db.QueryAsync<SpellingTestResult>(bestResultSql, new { StudentId = studentId, TestId = testId });
            var bestResult = bestResults.FirstOrDefault();

            // Получаем частые ошибки через SQL
            var mistakesSql = @"
                SELECT TOP 10
                    sa.StudentAnswer as IncorrectAnswer,
                    sq.CorrectLetter as CorrectAnswer,
                    sq.FullWord,
                    COUNT(*) as Count
                FROM SpellingAnswers sa
                LEFT JOIN SpellingQuestions sq ON sa.SpellingQuestionId = sq.Id
                WHERE sa.SpellingTestResultId IN (SELECT Id FROM SpellingTestResults WHERE StudentId = @StudentId AND SpellingTestId = @TestId AND IsCompleted = 1)
                    AND sa.IsCorrect = 0
                GROUP BY sa.StudentAnswer, sq.CorrectLetter, sq.FullWord
                ORDER BY COUNT(*) DESC";
            var mistakes = await _db.QueryAsync<dynamic>(mistakesSql, new { StudentId = studentId, TestId = testId });

            // Вычисляем общее время через SQL
            var totalTimeSql = @"
                SELECT SUM(DATEDIFF(SECOND, StartedAt, CompletedAt))
                FROM SpellingTestResults 
                WHERE StudentId = @StudentId AND SpellingTestId = @TestId AND IsCompleted = 1 AND CompletedAt IS NOT NULL";
            var totalSeconds = await _db.QueryScalarAsync<long?>(totalTimeSql, new { StudentId = studentId, TestId = testId });
            var totalTime = totalSeconds ?? 0;

            var user = await _userManager.FindByIdAsync(student.UserId);
            var @class = student.ClassId.HasValue ? await _classRepository.GetByIdAsync(student.ClassId.Value) : null;
            
            var response = new
            {
                FullName = user != null ? $"{user.FirstName} {user.LastName}" : "Unknown",
                School = student.School,
                ClassName = @class?.Name,
                AttemptsUsed = results.Count,
                MaxAttempts = test.MaxAttempts,
                BestResult = bestResult != null ? new
                {
                    Percentage = bestResult.Percentage,
                    Score = bestResult.Score,
                    MaxScore = bestResult.MaxScore
                } : null,
                TotalTimeSpent = totalTime > 0 ? new TimeSpan(totalTime).ToString(@"hh\:mm\:ss") : null,
                Attempts = results.Select(r => new
                {
                    AttemptNumber = r.AttemptNumber,
                    Percentage = r.Percentage,
                    Score = r.Score,
                    MaxScore = r.MaxScore,
                    Duration = r.CompletedAt.HasValue ? (r.CompletedAt.Value - r.StartedAt).ToString(@"mm\:ss") : null,
                    CompletedAt = r.CompletedAt
                }).ToList(),
                Mistakes = mistakes
            };

            return Json(response);
        }

        // GET: TestAnalytics/GetOrthoeopyStudentDetails
        [HttpGet]
        public async Task<IActionResult> GetOrthoeopyStudentDetails(int studentId, int testId)
        {
            var currentUser = await _userManager.GetUserAsync(User);

            var test = await _orthoeopyTestRepository.GetByIdAsync(testId);
            if (test == null || test.TeacherId != currentUser.Id)
                return NotFound();

            var student = await _studentRepository.GetWithUserAsync(studentId);

            if (student == null)
                return NotFound();

            // Получаем завершенные результаты студента через SQL
            var resultsSql = "SELECT * FROM OrthoeopyTestResults WHERE StudentId = @StudentId AND OrthoeopyTestId = @TestId AND IsCompleted = 1 ORDER BY CompletedAt DESC";
            var results = await _db.QueryAsync<OrthoeopyTestResult>(resultsSql, new { StudentId = studentId, TestId = testId });

            // Получаем лучший результат через SQL
            var bestResultSql = "SELECT TOP 1 * FROM OrthoeopyTestResults WHERE StudentId = @StudentId AND OrthoeopyTestId = @TestId AND IsCompleted = 1 ORDER BY Percentage DESC";
            var bestResults = await _db.QueryAsync<OrthoeopyTestResult>(bestResultSql, new { StudentId = studentId, TestId = testId });
            var bestResult = bestResults.FirstOrDefault();

            // Получаем частые ошибки через SQL
            var mistakesSql = @"
                SELECT TOP 10
                    oa.SelectedStressPosition as IncorrectPosition,
                    oq.Word,
                    oq.WordWithStress,
                    oq.StressPosition as CorrectPosition,
                    COUNT(*) as Count
                FROM OrthoeopyAnswers oa
                LEFT JOIN OrthoeopyQuestions oq ON oa.OrthoeopyQuestionId = oq.Id
                WHERE oa.OrthoeopyTestResultId IN (SELECT Id FROM OrthoeopyTestResults WHERE StudentId = @StudentId AND OrthoeopyTestId = @TestId AND IsCompleted = 1)
                    AND oa.IsCorrect = 0
                    AND oa.SelectedStressPosition IS NOT NULL
                GROUP BY oa.SelectedStressPosition, oq.Word, oq.WordWithStress, oq.StressPosition
                ORDER BY COUNT(*) DESC";
            var mistakes = await _db.QueryAsync<dynamic>(mistakesSql, new { StudentId = studentId, TestId = testId });

            // Вычисляем общее время через SQL
            var totalTimeSql = @"
                SELECT SUM(DATEDIFF(SECOND, StartedAt, CompletedAt))
                FROM OrthoeopyTestResults 
                WHERE StudentId = @StudentId AND OrthoeopyTestId = @TestId AND IsCompleted = 1 AND CompletedAt IS NOT NULL";
            var totalSeconds = await _db.QueryScalarAsync<long?>(totalTimeSql, new { StudentId = studentId, TestId = testId });
            var totalTime = totalSeconds ?? 0;

            var user = await _userManager.FindByIdAsync(student.UserId);
            var @class = student.ClassId.HasValue ? await _classRepository.GetByIdAsync(student.ClassId.Value) : null;
            
            var response = new
            {
                FullName = user != null ? $"{user.FirstName} {user.LastName}" : "Unknown",
                School = student.School,
                ClassName = @class?.Name,
                AttemptsUsed = results.Count,
                MaxAttempts = test.MaxAttempts,
                BestResult = bestResult != null ? new
                {
                    Percentage = bestResult.Percentage,
                    Score = bestResult.Score,
                    MaxScore = bestResult.MaxScore
                } : null,
                TotalTimeSpent = totalTime > 0 ? TimeSpan.FromSeconds(totalTime).ToString(@"hh\:mm\:ss") : null,
                Attempts = results.Select(r => new
                {
                    AttemptNumber = r.AttemptNumber,
                    Percentage = r.Percentage,
                    Score = r.Score,
                    MaxScore = r.MaxScore,
                    Duration = r.CompletedAt.HasValue ? (r.CompletedAt.Value - r.StartedAt).ToString(@"mm\:ss") : null,
                    CompletedAt = r.CompletedAt
                }).ToList(),
                Mistakes = mistakes.Select(m => new
                {
                    IncorrectPosition = (int)m.IncorrectPosition,
                    Word = m.Word != null ? (string)m.Word : null,
                    WordWithStress = m.WordWithStress != null ? (string)m.WordWithStress : null,
                    CorrectPosition = m.CorrectPosition != null ? (int?)m.CorrectPosition : null,
                    Count = (int)m.Count
                }).ToList()
            };

            return Json(response);
        }

        #endregion

        #region API Methods for Real-time Updates

        // GET: TestAnalytics/GetSpellingAnalyticsData
        [HttpGet]
        public async Task<IActionResult> GetSpellingAnalyticsData(int testId)
        {
            try
            {
                var currentUser = await _userManager.GetUserAsync(User);

                var test = await _spellingTestRepository.GetByIdAsync(testId);
                if (test == null || test.TeacherId != currentUser.Id)
                    return NotFound();

                var analytics = await BuildSpellingAnalyticsAsync(test);

                // Загружаем всех пользователей заранее
                var userIds = analytics.SpellingResults.Select(sr => sr.Student.UserId).Distinct().ToList();
                var users = new Dictionary<string, ApplicationUser?>();
                foreach (var userId in userIds)
                {
                    users[userId] = await _userManager.FindByIdAsync(userId);
                }

                return Json(new
                {
                    statistics = new
                    {
                        totalStudents = analytics.Statistics.TotalStudents,
                        averagePercentage = analytics.Statistics.AveragePercentage,
                        averageCompletionTime = new
                        {
                            minutes = analytics.Statistics.AverageCompletionTime.Minutes,
                            seconds = analytics.Statistics.AverageCompletionTime.Seconds
                        },
                        studentsCompleted = analytics.Statistics.StudentsCompleted,
                        studentsInProgress = analytics.Statistics.StudentsInProgress,
                        studentsNotStarted = analytics.Statistics.StudentsNotStarted
                    },
                    studentResults = analytics.SpellingResults.Select(sr => new
                    {
                        studentId = sr.Student.Id,
                        studentName = users.GetValueOrDefault(sr.Student.UserId)?.FullName ?? "Unknown",
                        hasCompleted = sr.HasCompleted,
                        isInProgress = sr.IsInProgress,
                        attemptsUsed = sr.AttemptsUsed,
                        maxAttempts = test.MaxAttempts,
                        bestResult = sr.BestResult != null ? new
                        {
                            percentage = sr.BestResult.Percentage,
                            score = sr.BestResult.Score,
                            maxScore = sr.BestResult.MaxScore
                        } : null,
                        latestResult = sr.LatestResult != null ? new
                        {
                            percentage = sr.LatestResult.Percentage,
                            score = sr.LatestResult.Score,
                            maxScore = sr.LatestResult.MaxScore,
                            completedAt = sr.LatestResult.CompletedAt?.ToString("dd.MM")
                        } : null,
                        totalTimeSpent = sr.TotalTimeSpent.HasValue ? new
                        {
                            minutes = sr.TotalTimeSpent.Value.Minutes,
                            seconds = sr.TotalTimeSpent.Value.Seconds
                        } : null
                    }),
                    questionAnalytics = analytics.SpellingQuestionAnalytics.Select(qa => new
                    {
                        questionId = qa.SpellingQuestion.Id,
                        successRate = qa.SuccessRate,
                        totalAnswers = qa.TotalAnswers,
                        correctAnswers = qa.CorrectAnswers,
                        commonMistakes = qa.CommonMistakes.Select(m => new
                        {
                            incorrectAnswer = m.IncorrectAnswer,
                            count = m.Count,
                            studentNames = m.StudentNames
                        })
                    })
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Ошибка получения данных аналитики для теста по орфографии {TestId}", testId);
                return StatusCode(500, new { error = "Ошибка загрузки данных" });
            }
        }

        // GET: TestAnalytics/GetPunctuationAnalyticsData
        [HttpGet]
        public async Task<IActionResult> GetPunctuationAnalyticsData(int testId)
        {
            try
            {
                var currentUser = await _userManager.GetUserAsync(User);

                var test = await _punctuationTestRepository.GetByIdAsync(testId);
                if (test == null || test.TeacherId != currentUser.Id)
                    return NotFound();

                var analytics = await BuildPunctuationAnalyticsAsync(test);

                // Загружаем всех пользователей заранее
                var userIds = analytics.StudentResults.Select(sr => sr.Student.UserId).Distinct().ToList();
                var users = new Dictionary<string, ApplicationUser?>();
                foreach (var userId in userIds)
                {
                    users[userId] = await _userManager.FindByIdAsync(userId);
                }

                return Json(new
                {
                    statistics = new
                    {
                        totalStudents = analytics.Statistics.TotalStudents,
                        averagePercentage = analytics.Statistics.AveragePercentage,
                        averageCompletionTime = new
                        {
                            minutes = analytics.Statistics.AverageCompletionTime.Minutes,
                            seconds = analytics.Statistics.AverageCompletionTime.Seconds
                        },
                        studentsCompleted = analytics.Statistics.StudentsCompleted,
                        studentsInProgress = analytics.Statistics.StudentsInProgress,
                        studentsNotStarted = analytics.Statistics.StudentsNotStarted
                    },
                    studentResults = analytics.StudentResults.Select(sr => new
                    {
                        studentId = sr.Student.Id,
                        studentName = users.GetValueOrDefault(sr.Student.UserId)?.FullName ?? "Unknown",
                        hasCompleted = sr.HasCompleted,
                        isInProgress = sr.IsInProgress,
                        attemptsUsed = sr.AttemptsUsed,
                        maxAttempts = test.MaxAttempts,
                        bestResult = sr.BestResult != null ? new
                        {
                            percentage = sr.BestResult.Percentage,
                            score = sr.BestResult.Score,
                            maxScore = sr.BestResult.MaxScore
                        } : null,
                        latestResult = sr.LatestResult != null ? new
                        {
                            percentage = sr.LatestResult.Percentage,
                            score = sr.LatestResult.Score,
                            maxScore = sr.LatestResult.MaxScore,
                            completedAt = sr.LatestResult.CompletedAt?.ToString("dd.MM")
                        } : null,
                        totalTimeSpent = sr.TotalTimeSpent.HasValue ? new
                        {
                            minutes = sr.TotalTimeSpent.Value.Minutes,
                            seconds = sr.TotalTimeSpent.Value.Seconds
                        } : null
                    }),
                    questionAnalytics = analytics.QuestionAnalytics.Select(qa => new
                    {
                        questionId = qa.Question.Id,
                        successRate = qa.SuccessRate,
                        totalAnswers = qa.TotalAnswers,
                        correctAnswers = qa.CorrectAnswers,
                        commonMistakes = qa.CommonMistakes.Select(m => new
                        {
                            incorrectAnswer = m.IncorrectAnswer,
                            count = m.Count,
                            studentNames = m.StudentNames
                        })
                    })
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Ошибка получения данных аналитики для теста по пунктуации {TestId}", testId);
                return StatusCode(500, new { error = "Ошибка загрузки данных" });
            }
        }

        // GET: TestAnalytics/GetOrthoeopyAnalyticsData
        [HttpGet]
        public async Task<IActionResult> GetOrthoeopyAnalyticsData(int testId)
        {
            try
            {
                var currentUser = await _userManager.GetUserAsync(User);

                var test = await _orthoeopyTestRepository.GetByIdAsync(testId);
                if (test == null || test.TeacherId != currentUser.Id)
                    return NotFound();

                var analytics = await BuildOrthoeopyAnalyticsAsync(test);

                // Загружаем всех пользователей заранее
                var userIds = analytics.StudentResults.Select(sr => sr.Student.UserId).Distinct().ToList();
                var users = new Dictionary<string, ApplicationUser?>();
                foreach (var userId in userIds)
                {
                    users[userId] = await _userManager.FindByIdAsync(userId);
                }

                return Json(new
                {
                    statistics = new
                    {
                        totalStudents = analytics.Statistics.TotalStudents,
                        averagePercentage = analytics.Statistics.AveragePercentage,
                        averageCompletionTime = new
                        {
                            minutes = analytics.Statistics.AverageCompletionTime.Minutes,
                            seconds = analytics.Statistics.AverageCompletionTime.Seconds
                        },
                        studentsCompleted = analytics.Statistics.StudentsCompleted,
                        studentsInProgress = analytics.Statistics.StudentsInProgress,
                        studentsNotStarted = analytics.Statistics.StudentsNotStarted
                    },
                    studentResults = analytics.StudentResults.Select(sr => new
                    {
                        studentId = sr.Student.Id,
                        studentName = users.GetValueOrDefault(sr.Student.UserId)?.FullName ?? "Unknown",
                        hasCompleted = sr.HasCompleted,
                        isInProgress = sr.IsInProgress,
                        attemptsUsed = sr.AttemptsUsed,
                        maxAttempts = test.MaxAttempts,
                        bestResult = sr.BestResult != null ? new
                        {
                            percentage = sr.BestResult.Percentage,
                            score = sr.BestResult.Score,
                            maxScore = sr.BestResult.MaxScore
                        } : null,
                        latestResult = sr.LatestResult != null ? new
                        {
                            percentage = sr.LatestResult.Percentage,
                            score = sr.LatestResult.Score,
                            maxScore = sr.LatestResult.MaxScore,
                            completedAt = sr.LatestResult.CompletedAt?.ToString("dd.MM")
                        } : null,
                        totalTimeSpent = sr.TotalTimeSpent.HasValue ? new
                        {
                            minutes = sr.TotalTimeSpent.Value.Minutes,
                            seconds = sr.TotalTimeSpent.Value.Seconds
                        } : null
                    }),
                    questionAnalytics = analytics.QuestionAnalytics.Select(qa => new
                    {
                        questionId = qa.Question.Id,
                        successRate = qa.SuccessRate,
                        totalAnswers = qa.TotalAnswers,
                        correctAnswers = qa.CorrectAnswers,
                        commonMistakes = qa.CommonMistakes.Select(m => new
                        {
                            incorrectPosition = m.IncorrectPosition,
                            count = m.Count,
                            studentNames = m.StudentNames
                        })
                    })
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Ошибка получения данных аналитики для теста по орфоэпии {TestId}", testId);
                return StatusCode(500, new { error = "Ошибка загрузки данных" });
            }
        }

        // GET: TestAnalytics/GetRegularTestAnalyticsData
        [HttpGet]
        public async Task<IActionResult> GetRegularTestAnalyticsData(int testId)
        {
            try
            {
                var currentUser = await _userManager.GetUserAsync(User);

                var test = await _regularTestRepository.GetByIdAsync(testId);
                if (test == null || test.TeacherId != currentUser.Id)
                    return NotFound();

                var analytics = await BuildRegularTestAnalyticsAsync(test);

                // Загружаем всех пользователей заранее
                var userIds = analytics.RegularResults.Select(sr => sr.Student.UserId).Distinct().ToList();
                var users = new Dictionary<string, ApplicationUser?>();
                foreach (var userId in userIds)
                {
                    users[userId] = await _userManager.FindByIdAsync(userId);
                }

                return Json(new
                {
                    statistics = new
                    {
                        totalStudents = analytics.Statistics.TotalStudents,
                        averagePercentage = analytics.Statistics.AveragePercentage,
                        averageCompletionTime = new
                        {
                            minutes = analytics.Statistics.AverageCompletionTime.Minutes,
                            seconds = analytics.Statistics.AverageCompletionTime.Seconds
                        },
                        studentsCompleted = analytics.Statistics.StudentsCompleted,
                        studentsInProgress = analytics.Statistics.StudentsInProgress,
                        studentsNotStarted = analytics.Statistics.StudentsNotStarted
                    },
                    studentResults = analytics.RegularResults.Select(sr => new
                    {
                        studentId = sr.Student.Id,
                        studentName = users.GetValueOrDefault(sr.Student.UserId)?.FullName ?? "Unknown",
                        hasCompleted = sr.HasCompleted,
                        isInProgress = sr.IsInProgress,
                        attemptsUsed = sr.AttemptsUsed,
                        maxAttempts = test.MaxAttempts,
                        bestResult = sr.BestResult != null ? new
                        {
                            percentage = sr.BestResult.Percentage,
                            score = sr.BestResult.Score,
                            maxScore = sr.BestResult.MaxScore
                        } : null,
                        latestResult = sr.LatestResult != null ? new
                        {
                            percentage = sr.LatestResult.Percentage,
                            score = sr.LatestResult.Score,
                            maxScore = sr.LatestResult.MaxScore,
                            completedAt = sr.LatestResult.CompletedAt?.ToString("dd.MM")
                        } : null,
                        totalTimeSpent = sr.TotalTimeSpent.HasValue ? new
                        {
                            minutes = sr.TotalTimeSpent.Value.Minutes,
                            seconds = sr.TotalTimeSpent.Value.Seconds
                        } : null
                    }),
                    questionAnalytics = analytics.QuestionAnalytics.Select(qa => new
                    {
                        questionId = qa.RegularQuestion.Id,
                        successRate = qa.SuccessRate,
                        totalAnswers = qa.TotalAnswers,
                        correctAnswers = qa.CorrectAnswers,
                        commonMistakes = qa.CommonMistakes.Select(m => new
                        {
                            incorrectAnswer = m.IncorrectAnswer,
                            count = m.Count,
                            studentNames = m.StudentNames
                        })
                    })
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Ошибка получения данных аналитики для классического теста {TestId}", testId);
                return StatusCode(500, new { error = "Ошибка загрузки данных" });
            }
        }

        #endregion
    }
}
