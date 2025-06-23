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

        public TestAnalyticsController(ApplicationDbContext context, UserManager<ApplicationUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        // GET: TestAnalytics/Spelling/5 - Аналитика теста на правописание
        public async Task<IActionResult> Spelling(int id)
        {
            var currentUser = await _userManager.GetUserAsync(User);
            var test = await _context.SpellingTests
                .Include(st => st.Teacher)
                .Include(st => st.Class)
                    .ThenInclude(c => c.Students)
                        .ThenInclude(s => s.User)
                .Include(st => st.Questions.OrderBy(q => q.OrderIndex))
                .Include(st => st.TestResults)
                    .ThenInclude(tr => tr.Student)
                        .ThenInclude(s => s.User)
                .Include(st => st.TestResults)
                    .ThenInclude(tr => tr.Answers)
                        .ThenInclude(a => a.Question)
                .FirstOrDefaultAsync(st => st.Id == id && st.TeacherId == currentUser.Id);

            if (test == null) return NotFound();

            var analytics = await BuildSpellingAnalyticsAsync(test);
            return View("SpellingAnalytics", analytics);
        }

        // GET: TestAnalytics/Regular/5 - Аналитика обычного теста
        public async Task<IActionResult> Regular(int id)
        {
            var currentUser = await _userManager.GetUserAsync(User);
            var test = await _context.Tests
                .Include(t => t.Teacher)
                .Include(t => t.Class)
                    .ThenInclude(c => c.Students)
                        .ThenInclude(s => s.User)
                .Include(t => t.Questions.OrderBy(q => q.OrderIndex))
                    .ThenInclude(q => q.Answers)
                .Include(t => t.TestResults)
                    .ThenInclude(tr => tr.Student)
                        .ThenInclude(s => s.User)
                .Include(t => t.TestResults)
                    .ThenInclude(tr => tr.StudentAnswers)
                        .ThenInclude(sa => sa.Question)
                .Include(t => t.TestResults)
                    .ThenInclude(tr => tr.StudentAnswers)
                        .ThenInclude(sa => sa.Answer)
                .FirstOrDefaultAsync(t => t.Id == id && t.TeacherId == currentUser.Id);

            if (test == null) return NotFound();

            var analytics = await BuildRegularTestAnalyticsAsync(test);
            return View("RegularTestAnalytics", analytics);
        }

        // Методы для тестов на правописание
        private async Task<SpellingTestAnalyticsViewModel> BuildSpellingAnalyticsAsync(SpellingTest test)
        {
            var analytics = new SpellingTestAnalyticsViewModel
            {
                Test = test
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
            analytics.StudentResults = BuildSpellingStudentResults(test, allStudents);

            var studentsWithResults = test.TestResults.Select(tr => tr.StudentId).Distinct().ToList();
            analytics.StudentsNotTaken = allStudents
                .Where(s => !studentsWithResults.Contains(s.Id))
                .ToList();

            analytics.QuestionAnalytics = BuildSpellingQuestionAnalytics(test);

            return analytics;
        }

        private TestStatistics BuildSpellingStatistics(SpellingTest test, List<Student> allStudents)
        {
            var completedResults = test.TestResults.Where(tr => tr.IsCompleted).ToList();
            var inProgressResults = test.TestResults.Where(tr => !tr.IsCompleted).ToList();
            var studentsWithResults = test.TestResults.Select(tr => tr.StudentId).Distinct().Count();

            var stats = new TestStatistics
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

        private List<StudentTestResultViewModel> BuildSpellingStudentResults(SpellingTest test, List<Student> allStudents)
        {
            var studentResults = new List<StudentTestResultViewModel>();

            foreach (var student in allStudents)
            {
                var results = test.TestResults.Where(tr => tr.StudentId == student.Id).ToList();
                var completedResults = results.Where(tr => tr.IsCompleted).ToList();

                var studentResult = new StudentTestResultViewModel
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

        private List<QuestionAnalyticsViewModel> BuildSpellingQuestionAnalytics(SpellingTest test)
        {
            var questionAnalytics = new List<QuestionAnalyticsViewModel>();

            foreach (var question in test.Questions.OrderBy(q => q.OrderIndex))
            {
                var answers = test.TestResults
                    .SelectMany(tr => tr.Answers)
                    .Where(a => a.SpellingQuestionId == question.Id)
                    .ToList();

                var analytics = new QuestionAnalyticsViewModel
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
                        .Where(a => !a.IsCorrect && !string.IsNullOrEmpty(a.StudentAnswer))
                        .GroupBy(a => a.StudentAnswer.ToLower())
                        .Select(g => new CommonMistakeViewModel
                        {
                            IncorrectAnswer = g.Key,
                            Count = g.Count(),
                            Percentage = Math.Round((double)g.Count() / analytics.IncorrectAnswers * 100, 1),
                            StudentNames = g.Select(a => a.TestResult.Student.User.FullName).ToList()
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
        private async Task<RegularTestAnalyticsViewModel> BuildRegularTestAnalyticsAsync(Test test)
        {
            var analytics = new RegularTestAnalyticsViewModel
            {
                Test = test
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
            analytics.StudentResults = BuildRegularTestStudentResults(test, allStudents);

            var studentsWithResults = test.TestResults.Select(tr => tr.StudentId).Distinct().ToList();
            analytics.StudentsNotTaken = allStudents
                .Where(s => !studentsWithResults.Contains(s.Id))
                .ToList();

            analytics.QuestionAnalytics = BuildRegularTestQuestionAnalytics(test);

            return analytics;
        }

        private TestStatistics BuildRegularTestStatistics(Test test, List<Student> allStudents)
        {
            var completedResults = test.TestResults.Where(tr => tr.IsCompleted).ToList();
            var inProgressResults = test.TestResults.Where(tr => !tr.IsCompleted).ToList();
            var studentsWithResults = test.TestResults.Select(tr => tr.StudentId).Distinct().Count();

            var stats = new TestStatistics
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

        private List<RegularTestStudentResultViewModel> BuildRegularTestStudentResults(Test test, List<Student> allStudents)
        {
            var studentResults = new List<RegularTestStudentResultViewModel>();

            foreach (var student in allStudents)
            {
                var results = test.TestResults.Where(tr => tr.StudentId == student.Id).ToList();
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

        private List<RegularTestQuestionAnalyticsViewModel> BuildRegularTestQuestionAnalytics(Test test)
        {
            var questionAnalytics = new List<RegularTestQuestionAnalyticsViewModel>();

            foreach (var question in test.Questions.OrderBy(q => q.OrderIndex))
            {
                var answers = test.TestResults
                    .SelectMany(tr => tr.StudentAnswers)
                    .Where(sa => sa.QuestionId == question.Id)
                    .ToList();

                var analytics = new RegularTestQuestionAnalyticsViewModel
                {
                    Question = question,
                    TotalAnswers = answers.Count,
                    CorrectAnswers = answers.Count(a => a.IsCorrect),
                    IncorrectAnswers = answers.Count(a => !a.IsCorrect)
                };

                if (answers.Any())
                {
                    analytics.SuccessRate = Math.Round((double)analytics.CorrectAnswers / analytics.TotalAnswers * 100, 1);

                    // Анализ частых неправильных вариантов ответов
                    var incorrectAnswers = answers
                        .Where(a => !a.IsCorrect && a.Answer != null)
                        .GroupBy(a => a.Answer.Text)
                        .Select(g => new CommonMistakeViewModel
                        {
                            IncorrectAnswer = g.Key,
                            Count = g.Count(),
                            Percentage = Math.Round((double)g.Count() / analytics.IncorrectAnswers * 100, 1),
                            StudentNames = g.Select(a => a.TestResult.Student.User.FullName).ToList()
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
    }
}
