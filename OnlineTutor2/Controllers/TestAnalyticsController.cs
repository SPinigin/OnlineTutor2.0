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
    public class TestAnalyticsController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly ILogger<TestAnalyticsController> _logger;

        public TestAnalyticsController(ApplicationDbContext context, 
            UserManager<ApplicationUser> userManager,
            ILogger<TestAnalyticsController> logger)
        {
            _context = context;
            _userManager = userManager;
            _logger = logger;
        }

        // GET: TestAnalytics/Spelling/5 - Аналитика теста по орфографии
        public async Task<IActionResult> Spelling(int id)
        {
            var currentUser = await _userManager.GetUserAsync(User);
            var test = await _context.SpellingTests
                .Include(st => st.Teacher)
                .Include(st => st.Class)
                    .ThenInclude(c => c.Students)
                        .ThenInclude(s => s.User)
                .Include(st => st.SpellingQuestions.OrderBy(q => q.OrderIndex))
                .Include(st => st.SpellingTestResults)
                    .ThenInclude(tr => tr.Student)
                        .ThenInclude(s => s.User)
                .Include(st => st.SpellingTestResults)
                    .ThenInclude(tr => tr.SpellingAnswers)
                        .ThenInclude(a => a.SpellingQuestion)
                .FirstOrDefaultAsync(st => st.Id == id && st.TeacherId == currentUser.Id);

            if (test == null) return NotFound();

            var analytics = await BuildSpellingAnalyticsAsync(test);
            return View("SpellingAnalytics", analytics);
        }

        // GET: TestAnalytics/Regular/1 - Аналитика обычного теста
        public async Task<IActionResult> Regular(int id)
        {
            var currentUser = await _userManager.GetUserAsync(User);
            var test = await _context.RegularTests
                .Include(st => st.Teacher)
                .Include(st => st.Class)
                    .ThenInclude(c => c.Students)
                        .ThenInclude(s => s.User)
                .Include(st => st.RegularQuestions.OrderBy(q => q.OrderIndex))
                .Include(st => st.RegularTestResults)
                    .ThenInclude(tr => tr.Student)
                        .ThenInclude(s => s.User)
                .Include(st => st.RegularTestResults)
                    .ThenInclude(tr => tr.RegularAnswers)
                        .ThenInclude(a => a.RegularQuestion)
                .FirstOrDefaultAsync(st => st.Id == id && st.TeacherId == currentUser.Id);

            if (test == null) return NotFound();

            var analytics = await BuildRegularTestAnalyticsAsync(test);
            return View("RegularTestAnalytics", analytics);
        }

        // GET: TestAnalytics/Regular/6 - Аналитика теста орфоэпии
        public async Task<IActionResult> Orthoeopy(int id)
        {
            var currentUser = await _userManager.GetUserAsync(User);
            var test = await _context.OrthoeopyTests
                .Include(st => st.Teacher)
                .Include(st => st.Class)
                    .ThenInclude(c => c.Students)
                        .ThenInclude(s => s.User)
                .Include(st => st.OrthoeopyQuestions.OrderBy(q => q.OrderIndex))
                .Include(st => st.OrthoeopyTestResults)
                    .ThenInclude(tr => tr.Student)
                        .ThenInclude(s => s.User)
                .Include(st => st.OrthoeopyTestResults)
                    .ThenInclude(tr => tr.OrthoeopyAnswers)
                        .ThenInclude(a => a.OrthoeopyQuestion)
                .FirstOrDefaultAsync(st => st.Id == id && st.TeacherId == currentUser.Id);

            if (test == null) return NotFound();

            var analytics = await BuildOrthoeopyAnalyticsAsync(test);
            return View("OrthoeopyTestAnalytics", analytics);
        }

        // GET: TestAnalytics/Punctuation/5 - Аналитика теста по пунктуации
        public async Task<IActionResult> Punctuation(int id)
        {
            var currentUser = await _userManager.GetUserAsync(User);
            var test = await _context.PunctuationTests
                .Include(pt => pt.Teacher)
                .Include(pt => pt.Class)
                    .ThenInclude(c => c.Students)
                        .ThenInclude(s => s.User)
                .Include(pt => pt.PunctuationQuestions.OrderBy(q => q.OrderIndex))
                .Include(pt => pt.PunctuationTestResults)
                    .ThenInclude(tr => tr.Student)
                        .ThenInclude(s => s.User)
                .Include(pt => pt.PunctuationTestResults)
                    .ThenInclude(tr => tr.PunctuationAnswers)
                        .ThenInclude(a => a.PunctuationQuestion)
                .FirstOrDefaultAsync(pt => pt.Id == id && pt.TeacherId == currentUser.Id);

            if (test == null) return NotFound();

            var analytics = await BuildPunctuationAnalyticsAsync(test);
            return View("PunctuationAnalytics", analytics);
        }

        // Методы для тестов по орфографии
        private async Task<SpellingTestAnalyticsViewModel> BuildSpellingAnalyticsAsync(SpellingTest test)
        {
            var analytics = new SpellingTestAnalyticsViewModel
            {
                SpellingTest = test
            };

            var allStudents = new List<Student>();
            if (test.Class != null)
            {
                allStudents = test.Class.Students.ToList();
            }
            else
            {
                var teacherClassIds = await _context.Classes
                    .Where(c => c.TeacherId == test.TeacherId)
                    .Select(c => c.Id)
                    .ToListAsync();

                allStudents = await _context.Students
                    .Include(s => s.User)
                    .Where(s => s.ClassId.HasValue && teacherClassIds.Contains(s.ClassId.Value))
                    .ToListAsync();
            }

            analytics.Statistics = BuildSpellingStatistics(test, allStudents);
            analytics.SpellingResults = BuildSpellingStudentResults(test, allStudents);
            analytics.SpellingQuestionAnalytics = BuildSpellingQuestionAnalytics(test);

            return analytics;
        }

        private SpellingTestStatistics BuildSpellingStatistics(SpellingTest test, List<Student> allStudents)
        {
            var completedResults = test.SpellingTestResults.Where(tr => tr.IsCompleted).ToList();
            var inProgressResults = test.SpellingTestResults.Where(tr => !tr.IsCompleted).ToList();
            var studentsWithResults = test.SpellingTestResults.Select(tr => tr.StudentId).Distinct().Count();

            var stats = new SpellingTestStatistics
            {
                TotalStudents = allStudents.Count,
                StudentsCompleted = completedResults.Select(tr => tr.StudentId).Distinct().Count(),
                StudentsInProgress = inProgressResults.Select(tr => tr.StudentId).Distinct().Count(),
                StudentsNotStarted = allStudents.Count - studentsWithResults
            };

            if (completedResults.Any())
            {
                stats.AverageScore = Math.Round(completedResults.Average(tr => tr.Score), 1);
                stats.AveragePercentage = Math.Round(completedResults.Average(tr => tr.Percentage), 1);
                stats.HighestScore = completedResults.Max(tr => tr.Score);
                stats.LowestScore = completedResults.Min(tr => tr.Score);
                stats.FirstCompletion = completedResults.Min(tr => tr.CompletedAt);
                stats.LastCompletion = completedResults.Max(tr => tr.CompletedAt);

                // Среднее время выполнения
                var completionTimes = completedResults
                    .Where(tr => tr.CompletedAt.HasValue)
                    .Select(tr => tr.CompletedAt.Value - tr.StartedAt)
                    .ToList();

                if (completionTimes.Any())
                {
                    var averageTicks = (long)completionTimes.Average(ts => ts.Ticks);
                    stats.AverageCompletionTime = new TimeSpan(averageTicks);
                }

                // Распределение оценок
                stats.GradeDistribution = new Dictionary<string, int>
                {
                    ["Отлично (80-100%)"] = completedResults.Count(tr => tr.Percentage >= 80),
                    ["Хорошо (60-79%)"] = completedResults.Count(tr => tr.Percentage >= 60 && tr.Percentage < 80),
                    ["Удовлетворительно (40-59%)"] = completedResults.Count(tr => tr.Percentage >= 40 && tr.Percentage < 60),
                    ["Неудовлетворительно (0-39%)"] = completedResults.Count(tr => tr.Percentage < 40)
                };
            }

            return stats;
        }

        private List<SpellingTestResultViewModel> BuildSpellingStudentResults(SpellingTest test, List<Student> allStudents)
        {
            var studentResults = new List<SpellingTestResultViewModel>();

            foreach (var student in allStudents)
            {
                var results = test.SpellingTestResults.Where(tr => tr.StudentId == student.Id).ToList();
                var completedResults = results.Where(tr => tr.IsCompleted).ToList();

                var studentResult = new SpellingTestResultViewModel
                {
                    Student = student,
                    Results = results,
                    AttemptsUsed = results.Count,
                    HasCompleted = completedResults.Any(),
                    IsInProgress = results.Any(tr => !tr.IsCompleted)
                };

                if (completedResults.Any())
                {
                    studentResult.BestResult = completedResults.OrderByDescending(tr => tr.Percentage).First();
                    studentResult.LatestResult = completedResults.OrderByDescending(tr => tr.CompletedAt).First();

                    var totalTime = completedResults
                        .Where(tr => tr.CompletedAt.HasValue)
                        .Sum(tr => (tr.CompletedAt.Value - tr.StartedAt).Ticks);

                    if (totalTime > 0)
                    {
                        studentResult.TotalTimeSpent = new TimeSpan(totalTime);
                    }
                }

                studentResults.Add(studentResult);
            }

            return studentResults.OrderBy(sr => sr.Student.User.LastName).ToList();
        }

        private List<SpellingQuestionAnalyticsViewModel> BuildSpellingQuestionAnalytics(SpellingTest test)
        {
            var questionAnalytics = new List<SpellingQuestionAnalyticsViewModel>();

            foreach (var question in test.SpellingQuestions.OrderBy(q => q.OrderIndex))
            {
                var answers = test.SpellingTestResults
                    .SelectMany(tr => tr.SpellingAnswers)
                    .Where(a => a.SpellingQuestionId == question.Id)
                    .ToList();

                var analytics = new SpellingQuestionAnalyticsViewModel
                {
                    SpellingQuestion = question,
                    TotalAnswers = answers.Count,
                    CorrectAnswers = answers.Count(a => a.IsCorrect),
                    IncorrectAnswers = answers.Count(a => !a.IsCorrect)
                };

                if (answers.Any())
                {
                    analytics.SuccessRate = Math.Round((double)analytics.CorrectAnswers / analytics.TotalAnswers * 100, 1);

                    // Анализ частых ошибок
                    var incorrectAnswers = answers
                        .Where(a => !a.IsCorrect && !string.IsNullOrEmpty(a.StudentAnswer))
                        .GroupBy(a => a.StudentAnswer.ToLower())
                        .Select(g => new CommonMistakeViewModel
                        {
                            IncorrectAnswer = g.Key,
                            Count = g.Count(),
                            Percentage = Math.Round((double)g.Count() / analytics.IncorrectAnswers * 100, 1),
                            StudentNames = g.Select(a => a.SpellingTestResult.Student.User.FullName).ToList()
                        })
                        .OrderByDescending(m => m.Count)
                        .Take(5)
                        .ToList();

                    analytics.CommonMistakes = incorrectAnswers;
                }

                questionAnalytics.Add(analytics);
            }

            // Отмечаем самые сложные и легкие вопросы
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

        // Методы для тестов по пунктуации
        private async Task<PunctuationTestAnalyticsViewModel> BuildPunctuationAnalyticsAsync(PunctuationTest test)
        {
            var analytics = new PunctuationTestAnalyticsViewModel
            {
                PunctuationTest = test
            };

            var allStudents = new List<Student>();
            if (test.Class != null)
            {
                allStudents = test.Class.Students.ToList();
            }
            else
            {
                var teacherClassIds = await _context.Classes
                    .Where(c => c.TeacherId == test.TeacherId)
                    .Select(c => c.Id)
                    .ToListAsync();

                allStudents = await _context.Students
                    .Include(s => s.User)
                    .Where(s => s.ClassId.HasValue && teacherClassIds.Contains(s.ClassId.Value))
                    .ToListAsync();
            }

            analytics.Statistics = BuildPunctuationStatistics(test, allStudents);
            analytics.StudentResults = BuildPunctuationStudentResults(test, allStudents);
            analytics.QuestionAnalytics = BuildPunctuationQuestionAnalytics(test);

            return analytics;
        }

        private PunctuationTestStatistics BuildPunctuationStatistics(PunctuationTest test, List<Student> allStudents)
        {
            var completedResults = test.PunctuationTestResults.Where(tr => tr.IsCompleted).ToList();
            var inProgressResults = test.PunctuationTestResults.Where(tr => !tr.IsCompleted).ToList();
            var studentsWithResults = test.PunctuationTestResults.Select(tr => tr.StudentId).Distinct().Count();

            var stats = new PunctuationTestStatistics
            {
                TotalStudents = allStudents.Count,
                StudentsCompleted = completedResults.Select(tr => tr.StudentId).Distinct().Count(),
                StudentsInProgress = inProgressResults.Select(tr => tr.StudentId).Distinct().Count(),
                StudentsNotStarted = allStudents.Count - studentsWithResults
            };

            if (completedResults.Any())
            {
                stats.AverageScore = Math.Round(completedResults.Average(tr => tr.Score), 1);
                stats.AveragePercentage = Math.Round(completedResults.Average(tr => tr.Percentage), 1);
                stats.HighestScore = completedResults.Max(tr => tr.Score);
                stats.LowestScore = completedResults.Min(tr => tr.Score);
                stats.FirstCompletion = completedResults.Min(tr => tr.CompletedAt);
                stats.LastCompletion = completedResults.Max(tr => tr.CompletedAt);

                var completionTimes = completedResults
                    .Where(tr => tr.CompletedAt.HasValue)
                    .Select(tr => tr.CompletedAt.Value - tr.StartedAt)
                    .ToList();

                if (completionTimes.Any())
                {
                    var averageTicks = (long)completionTimes.Average(ts => ts.Ticks);
                    stats.AverageCompletionTime = new TimeSpan(averageTicks);
                }

                stats.GradeDistribution = new Dictionary<string, int>
                {
                    ["Отлично (80-100%)"] = completedResults.Count(tr => tr.Percentage >= 80),
                    ["Хорошо (60-79%)"] = completedResults.Count(tr => tr.Percentage >= 60 && tr.Percentage < 80),
                    ["Удовлетворительно (40-59%)"] = completedResults.Count(tr => tr.Percentage >= 40 && tr.Percentage < 60),
                    ["Неудовлетворительно (0-39%)"] = completedResults.Count(tr => tr.Percentage < 40)
                };
            }

            return stats;
        }

        private List<PunctuationStudentResultViewModel> BuildPunctuationStudentResults(PunctuationTest test, List<Student> allStudents)
        {
            var studentResults = new List<PunctuationStudentResultViewModel>();

            foreach (var student in allStudents)
            {
                var results = test.PunctuationTestResults.Where(tr => tr.StudentId == student.Id).ToList();
                var completedResults = results.Where(tr => tr.IsCompleted).ToList();

                var studentResult = new PunctuationStudentResultViewModel
                {
                    Student = student,
                    Results = results,
                    AttemptsUsed = results.Count,
                    HasCompleted = completedResults.Any(),
                    IsInProgress = results.Any(tr => !tr.IsCompleted)
                };

                if (completedResults.Any())
                {
                    studentResult.BestResult = completedResults.OrderByDescending(tr => tr.Percentage).First();
                    studentResult.LatestResult = completedResults.OrderByDescending(tr => tr.CompletedAt).First();

                    var totalTime = completedResults
                        .Where(tr => tr.CompletedAt.HasValue)
                        .Sum(tr => (tr.CompletedAt.Value - tr.StartedAt).Ticks);

                    if (totalTime > 0)
                    {
                        studentResult.TotalTimeSpent = new TimeSpan(totalTime);
                    }
                }

                studentResults.Add(studentResult);
            }

            return studentResults.OrderBy(sr => sr.Student.User.LastName).ToList();
        }

        private List<PunctuationQuestionAnalyticsViewModel> BuildPunctuationQuestionAnalytics(PunctuationTest test)
        {
            var questionAnalytics = new List<PunctuationQuestionAnalyticsViewModel>();

            foreach (var question in test.PunctuationQuestions.OrderBy(q => q.OrderIndex))
            {
                var answers = test.PunctuationTestResults
                    .SelectMany(tr => tr.PunctuationAnswers)
                    .Where(a => a.PunctuationQuestionId == question.Id)
                    .ToList();

                var analytics = new PunctuationQuestionAnalyticsViewModel
                {
                    Question = question,
                    TotalAnswers = answers.Count,
                    CorrectAnswers = answers.Count(a => a.IsCorrect),
                    IncorrectAnswers = answers.Count(a => !a.IsCorrect)
                };

                if (answers.Any())
                {
                    analytics.SuccessRate = Math.Round((double)analytics.CorrectAnswers / analytics.TotalAnswers * 100, 1);
                    // Анализ частых ошибок
                    var incorrectAnswers = answers
                        .Where(a => !a.IsCorrect)
                        .GroupBy(a => a.StudentAnswer ?? "Пустой ответ")
                        .Select(g => new CommonMistakeViewModel
                        {
                            IncorrectAnswer = g.Key,
                            Count = g.Count(),
                            Percentage = Math.Round((double)g.Count() / analytics.IncorrectAnswers * 100, 1),
                            StudentNames = g.Select(a => a.PunctuationTestResult.Student.User.FullName).ToList()
                        })
                        .OrderByDescending(m => m.Count)
                        .Take(5)
                        .ToList();

                    analytics.CommonMistakes = incorrectAnswers;
                }

                questionAnalytics.Add(analytics);
            }

            // Отмечаем самые сложные и легкие вопросы
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

        // Методы для обычных тестов
        private async Task<RegularTestAnalyticsViewModel> BuildRegularTestAnalyticsAsync(RegularTest test)
        {
            var analytics = new RegularTestAnalyticsViewModel
            {
                RegularTest = test
            };

            var allStudents = new List<Student>();
            if (test.Class != null)
            {
                allStudents = test.Class.Students.ToList();
            }
            else
            {
                var teacherClassIds = await _context.Classes
                    .Where(c => c.TeacherId == test.TeacherId)
                    .Select(c => c.Id)
                    .ToListAsync();

                allStudents = await _context.Students
                    .Include(s => s.User)
                    .Where(s => s.ClassId.HasValue && teacherClassIds.Contains(s.ClassId.Value))
                    .ToListAsync();
            }

            analytics.Statistics = BuildRegularTestStatistics(test, allStudents);
            analytics.RegularResults = BuildRegularTestStudentResults(test, allStudents);
            analytics.QuestionAnalytics = BuildRegularTestQuestionAnalytics(test);

            return analytics;
        }

        private RegularTestStatistics BuildRegularTestStatistics(RegularTest test, List<Student> allStudents)
        {
            var completedResults = test.RegularTestResults.Where(tr => tr.IsCompleted).ToList();
            var inProgressResults = test.RegularTestResults.Where(tr => !tr.IsCompleted).ToList();
            var studentsWithResults = test.RegularTestResults.Select(tr => tr.StudentId).Distinct().Count();

            var stats = new RegularTestStatistics
            {
                TotalStudents = allStudents.Count,
                StudentsCompleted = completedResults.Select(tr => tr.StudentId).Distinct().Count(),
                StudentsInProgress = inProgressResults.Select(tr => tr.StudentId).Distinct().Count(),
                StudentsNotStarted = allStudents.Count - studentsWithResults
            };

            if (completedResults.Any())
            {
                stats.AverageScore = Math.Round(completedResults.Average(tr => tr.Score), 1);
                stats.AveragePercentage = Math.Round(completedResults.Average(tr => tr.Percentage), 1);
                stats.HighestScore = completedResults.Max(tr => tr.Score);
                stats.LowestScore = completedResults.Min(tr => tr.Score);
                stats.FirstCompletion = completedResults.Min(tr => tr.CompletedAt);
                stats.LastCompletion = completedResults.Max(tr => tr.CompletedAt);

                var completionTimes = completedResults
                    .Where(tr => tr.CompletedAt.HasValue)
                    .Select(tr => tr.CompletedAt.Value - tr.StartedAt)
                    .ToList();

                if (completionTimes.Any())
                {
                    var averageTicks = (long)completionTimes.Average(ts => ts.Ticks);
                    stats.AverageCompletionTime = new TimeSpan(averageTicks);
                }

                //изменить градацию
                stats.GradeDistribution = new Dictionary<string, int>
                {
                    ["Отлично (80-100%)"] = completedResults.Count(tr => tr.Percentage >= 80),
                    ["Хорошо (60-79%)"] = completedResults.Count(tr => tr.Percentage >= 60 && tr.Percentage < 80),
                    ["Удовлетворительно (40-59%)"] = completedResults.Count(tr => tr.Percentage >= 40 && tr.Percentage < 60),
                    ["Неудовлетворительно (0-39%)"] = completedResults.Count(tr => tr.Percentage < 40)
                };
            }

            return stats;
        }

        private List<RegularTestStudentResultViewModel> BuildRegularTestStudentResults(RegularTest test, List<Student> allStudents)
        {
            var studentResults = new List<RegularTestStudentResultViewModel>();

            foreach (var student in allStudents)
            {
                var results = test.RegularTestResults.Where(tr => tr.StudentId == student.Id).ToList();
                var completedResults = results.Where(tr => tr.IsCompleted).ToList();

                var studentResult = new RegularTestStudentResultViewModel
                {
                    Student = student,
                    Results = results,
                    AttemptsUsed = results.Count,
                    HasCompleted = completedResults.Any(),
                    IsInProgress = results.Any(tr => !tr.IsCompleted)
                };

                if (completedResults.Any())
                {
                    studentResult.BestResult = completedResults.OrderByDescending(tr => tr.Percentage).First();
                    studentResult.LatestResult = completedResults.OrderByDescending(tr => tr.CompletedAt).First();

                    var totalTime = completedResults
                        .Where(tr => tr.CompletedAt.HasValue)
                        .Sum(tr => (tr.CompletedAt.Value - tr.StartedAt).Ticks);

                    if (totalTime > 0)
                    {
                        studentResult.TotalTimeSpent = new TimeSpan(totalTime);
                    }
                }

                studentResults.Add(studentResult);
            }

            return studentResults.OrderBy(sr => sr.Student.User.LastName).ToList();
        }

        private List<RegularTestQuestionAnalyticsViewModel> BuildRegularTestQuestionAnalytics(RegularTest test)
        {
            var questionAnalytics = new List<RegularTestQuestionAnalyticsViewModel>();

            foreach (var question in test.RegularQuestions.OrderBy(q => q.OrderIndex))
            {
                var answers = test.RegularTestResults
                    .SelectMany(tr => tr.RegularAnswers)
                    .Where(sa => sa.QuestionId == question.Id)
                    .ToList();

                var analytics = new RegularTestQuestionAnalyticsViewModel
                {
                    RegularQuestion = question,
                    TotalAnswers = answers.Count,
                    CorrectAnswers = answers.Count(a => a.IsCorrect),
                    IncorrectAnswers = answers.Count(a => !a.IsCorrect)
                };

                if (answers.Any())
                {
                    analytics.SuccessRate = Math.Round((double)analytics.CorrectAnswers / analytics.TotalAnswers * 100, 1);

                    // Анализ по вариантам ответов
                    var optionStats = new Dictionary<int, int>();

                    foreach (var answer in answers)
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

                    // Находим самые популярные неправильные ответы
                    var incorrectOptions = question.Options
                        .Where(o => !o.IsCorrect && optionStats.ContainsKey(o.Id))
                        .Select(o => new CommonMistakeViewModel
                        {
                            IncorrectAnswer = o.Text,
                            Count = optionStats[o.Id],
                            Percentage = Math.Round((double)optionStats[o.Id] / analytics.TotalAnswers * 100, 1),
                            StudentNames = answers
                                .Where(a => a.SelectedOptionIds != null && a.SelectedOptionIds.Contains(o.Id.ToString()))
                                .Select(a => a.RegularTestResult.Student.User.FullName)
                                .ToList()
                        })
                        .OrderByDescending(m => m.Count)
                        .Take(5)
                        .ToList();

                    analytics.CommonMistakes = incorrectOptions;
                }

                questionAnalytics.Add(analytics);
            }

            // Отмечаем самые сложные и легкие вопросы
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


        // Метод для получения детальной информации о студенте (пунктуация)
        [HttpGet]
        public async Task<IActionResult> GetPunctuationStudentDetails(int studentId, int testId)
        {
            var currentUser = await _userManager.GetUserAsync(User);

            // Проверяем права доступа к тесту
            var test = await _context.PunctuationTests
                .FirstOrDefaultAsync(pt => pt.Id == testId && pt.TeacherId == currentUser.Id);

            if (test == null)
                return NotFound();

            var student = await _context.Students
                .Include(s => s.User)
                .Include(s => s.Class)
                .FirstOrDefaultAsync(s => s.Id == studentId);

            if (student == null)
                return NotFound();

            var results = await _context.PunctuationTestResults
                .Include(tr => tr.PunctuationAnswers)
                    .ThenInclude(a => a.PunctuationQuestion)
                .Where(tr => tr.PunctuationTestId == testId && tr.StudentId == studentId && tr.IsCompleted)
                .OrderByDescending(tr => tr.CompletedAt)
                .ToListAsync();

            var bestResult = results.OrderByDescending(r => r.Percentage).FirstOrDefault();

            var mistakes = results
                .SelectMany(r => r.PunctuationAnswers)
                .Where(a => !a.IsCorrect)
                .GroupBy(a => new {
                    a.StudentAnswer,
                    a.PunctuationQuestion.CorrectPositions,
                    a.PunctuationQuestion.SentenceWithNumbers
                })
                .Select(g => new
                {
                    IncorrectAnswer = g.Key.StudentAnswer ?? "Пустой ответ",
                    CorrectAnswer = g.Key.CorrectPositions ?? "Без запятых",
                    SentenceWithNumbers = g.Key.SentenceWithNumbers,
                    Count = g.Count()
                })
                .OrderByDescending(m => m.Count)
                .Take(10)
                .ToList();

            var totalTime = results
                .Where(r => r.CompletedAt.HasValue)
                .Sum(r => (r.CompletedAt.Value - r.StartedAt).Ticks);

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

            // Проверяем права доступа к тесту
            var test = await _context.SpellingTests
                .FirstOrDefaultAsync(st => st.Id == testId && st.TeacherId == currentUser.Id);

            if (test == null)
                return NotFound();

            var student = await _context.Students
                .Include(s => s.User)
                .Include(s => s.Class)
                .FirstOrDefaultAsync(s => s.Id == studentId);

            if (student == null)
                return NotFound();

            var results = await _context.SpellingTestResults
                .Include(tr => tr.SpellingAnswers)
                    .ThenInclude(a => a.SpellingQuestion)
                .Where(tr => tr.SpellingTestId == testId && tr.StudentId == studentId && tr.IsCompleted)
                .OrderByDescending(tr => tr.CompletedAt)
                .ToListAsync();

            var bestResult = results.OrderByDescending(r => r.Percentage).FirstOrDefault();

            var mistakes = results
                .SelectMany(r => r.SpellingAnswers)
                .Where(a => !a.IsCorrect)
                .GroupBy(a => new { a.StudentAnswer, a.SpellingQuestion.CorrectLetter, a.SpellingQuestion.FullWord })
                .Select(g => new
                {
                    IncorrectAnswer = g.Key.StudentAnswer,
                    CorrectAnswer = g.Key.CorrectLetter,
                    FullWord = g.Key.FullWord,
                    Count = g.Count()
                })
                .OrderByDescending(m => m.Count)
                .Take(10)
                .ToList();

            var totalTime = results
                .Where(r => r.CompletedAt.HasValue)
                .Sum(r => (r.CompletedAt.Value - r.StartedAt).Ticks);

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

        // GET: TestAnalytics/Orthoepy/5 - Аналитика теста по орфоэпии
        public async Task<IActionResult> Orthoepy(int id)
        {
            var currentUser = await _userManager.GetUserAsync(User);
            var test = await _context.OrthoeopyTests
                .Include(ot => ot.Teacher)
                .Include(ot => ot.Class)
                    .ThenInclude(c => c.Students)
                        .ThenInclude(s => s.User)
                .Include(ot => ot.OrthoeopyQuestions.OrderBy(q => q.OrderIndex))
                .Include(ot => ot.OrthoeopyTestResults)
                    .ThenInclude(tr => tr.Student)
                        .ThenInclude(s => s.User)
                .Include(ot => ot.OrthoeopyTestResults)
                    .ThenInclude(tr => tr.OrthoeopyAnswers)
                        .ThenInclude(a => a.OrthoeopyQuestion)
                .FirstOrDefaultAsync(ot => ot.Id == id && ot.TeacherId == currentUser.Id);

            if (test == null) return NotFound();

            var analytics = await BuildOrthoeopyAnalyticsAsync(test);
            return View("OrthoeopyAnalytics", analytics);
        }

        private async Task<OrthoeopyTestAnalyticsViewModel> BuildOrthoeopyAnalyticsAsync(OrthoeopyTest test)
        {
            var analytics = new OrthoeopyTestAnalyticsViewModel
            {
                OrthoeopyTest = test
            };

            var allStudents = new List<Student>();
            if (test.Class != null)
            {
                allStudents = test.Class.Students.ToList();
            }
            else
            {
                var teacherClassIds = await _context.Classes
                    .Where(c => c.TeacherId == test.TeacherId)
                    .Select(c => c.Id)
                    .ToListAsync();

                allStudents = await _context.Students
                    .Include(s => s.User)
                    .Where(s => s.ClassId.HasValue && teacherClassIds.Contains(s.ClassId.Value))
                    .ToListAsync();
            }

            analytics.Statistics = BuildOrthoeopyStatistics(test, allStudents);
            analytics.StudentResults = BuildOrthoeopyStudentResults(test, allStudents);
            analytics.QuestionAnalytics = BuildOrthoeopyQuestionAnalytics(test);
            analytics.StudentsNotTaken = allStudents.Where(s => !test.OrthoeopyTestResults.Any(tr => tr.StudentId == s.Id)).ToList();

            return analytics;
        }

        private OrthoeopyTestStatistics BuildOrthoeopyStatistics(OrthoeopyTest test, List<Student> allStudents)
        {
            var completedResults = test.OrthoeopyTestResults.Where(tr => tr.IsCompleted).ToList();
            var inProgressResults = test.OrthoeopyTestResults.Where(tr => !tr.IsCompleted).ToList();
            var studentsWithResults = test.OrthoeopyTestResults.Select(tr => tr.StudentId).Distinct().Count();

            var stats = new OrthoeopyTestStatistics
            {
                TotalStudents = allStudents.Count,
                StudentsCompleted = completedResults.Select(tr => tr.StudentId).Distinct().Count(),
                StudentsInProgress = inProgressResults.Select(tr => tr.StudentId).Distinct().Count(),
                StudentsNotStarted = allStudents.Count - studentsWithResults
            };

            if (completedResults.Any())
            {
                stats.AverageScore = Math.Round(completedResults.Average(tr => tr.Score), 1);
                stats.AveragePercentage = Math.Round(completedResults.Average(tr => tr.Percentage), 1);
                stats.HighestScore = completedResults.Max(tr => tr.Score);
                stats.LowestScore = completedResults.Min(tr => tr.Score);
                stats.FirstCompletion = completedResults.Min(tr => tr.CompletedAt);
                stats.LastCompletion = completedResults.Max(tr => tr.CompletedAt);

                var completionTimes = completedResults
                    .Where(tr => tr.CompletedAt.HasValue)
                    .Select(tr => tr.CompletedAt.Value - tr.StartedAt)
                    .ToList();

                if (completionTimes.Any())
                {
                    var averageTicks = (long)completionTimes.Average(ts => ts.Ticks);
                    stats.AverageCompletionTime = new TimeSpan(averageTicks);
                }

                stats.GradeDistribution = new Dictionary<string, int>
                {
                    ["Отлично (80-100%)"] = completedResults.Count(tr => tr.Percentage >= 80),
                    ["Хорошо (60-79%)"] = completedResults.Count(tr => tr.Percentage >= 60 && tr.Percentage < 80),
                    ["Удовлетворительно (40-59%)"] = completedResults.Count(tr => tr.Percentage >= 40 && tr.Percentage < 60),
                    ["Неудовлетворительно (0-39%)"] = completedResults.Count(tr => tr.Percentage < 40)
                };
            }

            return stats;
        }

        private List<OrthoeopyStudentResultViewModel> BuildOrthoeopyStudentResults(OrthoeopyTest test, List<Student> allStudents)
        {
            var studentResults = new List<OrthoeopyStudentResultViewModel>();

            foreach (var student in allStudents)
            {
                var results = test.OrthoeopyTestResults.Where(tr => tr.StudentId == student.Id).ToList();
                var completedResults = results.Where(tr => tr.IsCompleted).ToList();

                var studentResult = new OrthoeopyStudentResultViewModel
                {
                    Student = student,
                    Results = results,
                    AttemptsUsed = results.Count,
                    HasCompleted = completedResults.Any(),
                    IsInProgress = results.Any(tr => !tr.IsCompleted)
                };

                if (completedResults.Any())
                {
                    studentResult.BestResult = completedResults.OrderByDescending(tr => tr.Percentage).First();
                    studentResult.LatestResult = completedResults.OrderByDescending(tr => tr.CompletedAt).First();

                    var totalTime = completedResults
                        .Where(tr => tr.CompletedAt.HasValue)
                        .Sum(tr => (tr.CompletedAt.Value - tr.StartedAt).Ticks);

                    if (totalTime > 0)
                    {
                        studentResult.TotalTimeSpent = new TimeSpan(totalTime);
                    }
                }

                studentResults.Add(studentResult);
            }

            return studentResults.OrderBy(sr => sr.Student.User.LastName).ToList();
        }

        private List<OrthoeopyQuestionAnalyticsViewModel> BuildOrthoeopyQuestionAnalytics(OrthoeopyTest test)
        {
            var questionAnalytics = new List<OrthoeopyQuestionAnalyticsViewModel>();

            foreach (var question in test.OrthoeopyQuestions.OrderBy(q => q.OrderIndex))
            {
                var answers = test.OrthoeopyTestResults
                    .SelectMany(tr => tr.OrthoeopyAnswers)
                    .Where(a => a.OrthoeopyQuestionId == question.Id)
                    .ToList();

                var analytics = new OrthoeopyQuestionAnalyticsViewModel
                {
                    Question = question,
                    TotalAnswers = answers.Count,
                    CorrectAnswers = answers.Count(a => a.IsCorrect),
                    IncorrectAnswers = answers.Count(a => !a.IsCorrect)
                };

                if (answers.Any())
                {
                    analytics.SuccessRate = Math.Round((double)analytics.CorrectAnswers / analytics.TotalAnswers * 100, 1);

                    // Анализ частых ошибок - неправильные позиции ударения
                    var incorrectAnswers = answers
                        .Where(a => !a.IsCorrect && a.SelectedStressPosition.HasValue)
                        .GroupBy(a => a.SelectedStressPosition.Value)
                        .Select(g => new StressPositionMistakeViewModel
                        {
                            IncorrectPosition = g.Key,
                            Count = g.Count(),
                            Percentage = Math.Round((double)g.Count() / analytics.IncorrectAnswers * 100, 1),
                            StudentNames = g.Select(a => a.OrthoeopyTestResult.Student.User.FullName).ToList()
                        })
                        .OrderByDescending(m => m.Count)
                        .Take(5)
                        .ToList();

                    analytics.CommonMistakes = incorrectAnswers;
                }

                questionAnalytics.Add(analytics);
            }

            // Отмечаем самые сложные и легкие вопросы
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

        // GET: TestAnalytics/GetOrthoeopyStudentDetails
        [HttpGet]
        public async Task<IActionResult> GetOrthoeopyStudentDetails(int studentId, int testId)
        {
            var currentUser = await _userManager.GetUserAsync(User);

            var test = await _context.OrthoeopyTests
                .FirstOrDefaultAsync(ot => ot.Id == testId && ot.TeacherId == currentUser.Id);

            if (test == null)
                return NotFound();

            var student = await _context.Students
                .Include(s => s.User)
                .Include(s => s.Class)
                .FirstOrDefaultAsync(s => s.Id == studentId);

            if (student == null)
                return NotFound();

            var results = await _context.OrthoeopyTestResults
                .Include(tr => tr.OrthoeopyAnswers)
                    .ThenInclude(a => a.OrthoeopyQuestion)
                .Where(tr => tr.OrthoeopyTestId == testId && tr.StudentId == studentId && tr.IsCompleted)
                .OrderByDescending(tr => tr.CompletedAt)
                .ToListAsync();

            var bestResult = results.OrderByDescending(r => r.Percentage).FirstOrDefault();

            var mistakes = results
                .SelectMany(r => r.OrthoeopyAnswers)
                .Where(a => !a.IsCorrect && a.SelectedStressPosition.HasValue)
                .GroupBy(a => new {
                    a.SelectedStressPosition,
                    a.OrthoeopyQuestion.Word,
                    a.OrthoeopyQuestion.WordWithStress,
                    a.OrthoeopyQuestion.StressPosition
                })
                .Select(g => new
                {
                    IncorrectPosition = g.Key.SelectedStressPosition,
                    Word = g.Key.Word,
                    WordWithStress = g.Key.WordWithStress,
                    CorrectPosition = g.Key.StressPosition,
                    Count = g.Count()
                })
                .OrderByDescending(m => m.Count)
                .Take(10)
                .ToList();

            var totalTime = results
                .Where(r => r.CompletedAt.HasValue)
                .Sum(r => (r.CompletedAt.Value - r.StartedAt).Ticks);

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

        #region API Methods for Real-time Updates

        // GET: TestAnalytics/GetSpellingAnalyticsData
        [HttpGet]
        public async Task<IActionResult> GetSpellingAnalyticsData(int testId)
        {
            try
            {
                var currentUser = await _userManager.GetUserAsync(User);

                var test = await _context.SpellingTests
                    .Include(st => st.Class)
                        .ThenInclude(c => c.Students)
                            .ThenInclude(s => s.User)
                    .Include(st => st.SpellingQuestions.OrderBy(q => q.OrderIndex))
                    .Include(st => st.SpellingTestResults)
                        .ThenInclude(tr => tr.Student)
                            .ThenInclude(s => s.User)
                    .Include(st => st.SpellingTestResults)
                        .ThenInclude(tr => tr.SpellingAnswers)
                            .ThenInclude(a => a.SpellingQuestion)
                    .FirstOrDefaultAsync(st => st.Id == testId && st.TeacherId == currentUser.Id);

                if (test == null)
                {
                    return NotFound();
                }

                var analytics = await BuildSpellingAnalyticsAsync(test);

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
                        studentName = sr.Student.User.FullName,
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

                var test = await _context.PunctuationTests
                    .Include(pt => pt.Class)
                        .ThenInclude(c => c.Students)
                            .ThenInclude(s => s.User)
                    .Include(pt => pt.PunctuationQuestions.OrderBy(q => q.OrderIndex))
                    .Include(pt => pt.PunctuationTestResults)
                        .ThenInclude(tr => tr.Student)
                            .ThenInclude(s => s.User)
                    .Include(pt => pt.PunctuationTestResults)
                        .ThenInclude(tr => tr.PunctuationAnswers)
                            .ThenInclude(a => a.PunctuationQuestion)
                    .FirstOrDefaultAsync(pt => pt.Id == testId && pt.TeacherId == currentUser.Id);

                if (test == null)
                {
                    return NotFound();
                }

                var analytics = await BuildPunctuationAnalyticsAsync(test);

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
                        studentName = sr.Student.User.FullName,
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

                var test = await _context.OrthoeopyTests
                    .Include(ot => ot.Class)
                        .ThenInclude(c => c.Students)
                            .ThenInclude(s => s.User)
                    .Include(ot => ot.OrthoeopyQuestions.OrderBy(q => q.OrderIndex))
                    .Include(ot => ot.OrthoeopyTestResults)
                        .ThenInclude(tr => tr.Student)
                            .ThenInclude(s => s.User)
                    .Include(ot => ot.OrthoeopyTestResults)
                        .ThenInclude(tr => tr.OrthoeopyAnswers)
                            .ThenInclude(a => a.OrthoeopyQuestion)
                    .FirstOrDefaultAsync(ot => ot.Id == testId && ot.TeacherId == currentUser.Id);

                if (test == null)
                {
                    return NotFound();
                }

                var analytics = await BuildOrthoeopyAnalyticsAsync(test);

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
                        studentName = sr.Student.User.FullName,
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

                var test = await _context.RegularTests
                    .Include(rt => rt.Class)
                        .ThenInclude(c => c.Students)
                            .ThenInclude(s => s.User)
                    .Include(rt => rt.RegularQuestions.OrderBy(q => q.OrderIndex))
                        .ThenInclude(q => q.Options)
                    .Include(rt => rt.RegularTestResults)
                        .ThenInclude(tr => tr.Student)
                            .ThenInclude(s => s.User)
                    .Include(rt => rt.RegularTestResults)
                        .ThenInclude(tr => tr.RegularAnswers)
                            .ThenInclude(a => a.RegularQuestion)
                    .FirstOrDefaultAsync(rt => rt.Id == testId && rt.TeacherId == currentUser.Id);

                if (test == null)
                {
                    return NotFound();
                }

                var analytics = await BuildRegularTestAnalyticsAsync(test);

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
                        studentName = sr.Student.User.FullName,
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
