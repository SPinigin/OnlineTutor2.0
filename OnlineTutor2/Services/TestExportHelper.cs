using OnlineTutor2.Data;
using OnlineTutor2.Data.Repositories;
using OnlineTutor2.Models;

namespace OnlineTutor2.Services
{
    /// <summary>
    /// Вспомогательный класс для экспорта тестов, устраняющий дублирование кода
    /// </summary>
    public static class TestExportHelper
    {
        /// <summary>
        /// Получает все тесты с связанными данными для экспорта
        /// </summary>
        public static async Task<List<TestExportInfo>> GetAllTestsForExportAsync(IDatabaseConnection db)
        {
            var result = new List<TestExportInfo>();

            // Орфография - получаем через SQL
            var spellingTests = await db.QueryAsync<SpellingTest>("SELECT * FROM SpellingTests");
            foreach (var t in spellingTests)
            {
                var teacher = await db.QueryFirstOrDefaultAsync<Teacher>("SELECT * FROM Teachers WHERE UserId = @TeacherId", new { TeacherId = t.TeacherId });
                var teacherUser = teacher != null ? await db.QueryFirstOrDefaultAsync<ApplicationUser>("SELECT * FROM AspNetUsers WHERE Id = @UserId", new { UserId = t.TeacherId }) : null;
                var testClasses = await db.QueryAsync<SpellingTestClass>("SELECT * FROM SpellingTestClasses WHERE SpellingTestId = @TestId", new { TestId = t.Id });
                var questionsCount = await db.QueryScalarAsync<int>("SELECT COUNT(*) FROM SpellingQuestions WHERE SpellingTestId = @TestId", new { TestId = t.Id });
                var resultsCount = await db.QueryScalarAsync<int>("SELECT COUNT(*) FROM SpellingTestResults WHERE SpellingTestId = @TestId", new { TestId = t.Id });
                
                var classNames = new List<string>();
                foreach (var tc in testClasses)
                {
                    var @class = await db.QueryFirstOrDefaultAsync<Class>("SELECT * FROM Classes WHERE Id = @ClassId", new { ClassId = tc.ClassId });
                    if (@class != null) classNames.Add(@class.Name);
                }

                result.Add(new TestExportInfo
                {
                    Id = t.Id,
                    Title = t.Title,
                    Type = "Орфография",
                    TeacherName = teacherUser != null ? $"{teacherUser.FirstName} {teacherUser.LastName}" : "Unknown",
                    ClassNames = classNames.Any() ? string.Join(", ", classNames) : "Все ученики",
                    QuestionsCount = questionsCount,
                    ResultsCount = resultsCount,
                    CreatedAt = t.CreatedAt,
                    IsActive = t.IsActive
                });
            }

            // Классические тесты
            var regularTests = await db.QueryAsync<RegularTest>("SELECT * FROM RegularTests");
            foreach (var t in regularTests)
            {
                var teacher = await db.QueryFirstOrDefaultAsync<Teacher>("SELECT * FROM Teachers WHERE UserId = @TeacherId", new { TeacherId = t.TeacherId });
                var teacherUser = teacher != null ? await db.QueryFirstOrDefaultAsync<ApplicationUser>("SELECT * FROM AspNetUsers WHERE Id = @UserId", new { UserId = t.TeacherId }) : null;
                var testClasses = await db.QueryAsync<RegularTestClass>("SELECT * FROM RegularTestClasses WHERE RegularTestId = @TestId", new { TestId = t.Id });
                var questionsCount = await db.QueryScalarAsync<int>("SELECT COUNT(*) FROM RegularQuestions WHERE TestId = @TestId", new { TestId = t.Id });
                var resultsCount = await db.QueryScalarAsync<int>("SELECT COUNT(*) FROM RegularTestResults WHERE RegularTestId = @TestId", new { TestId = t.Id });
                
                var classNames = new List<string>();
                foreach (var tc in testClasses)
                {
                    var @class = await db.QueryFirstOrDefaultAsync<Class>("SELECT * FROM Classes WHERE Id = @ClassId", new { ClassId = tc.ClassId });
                    if (@class != null) classNames.Add(@class.Name);
                }

                result.Add(new TestExportInfo
                {
                    Id = t.Id,
                    Title = t.Title,
                    Type = "Классический",
                    TeacherName = teacherUser != null ? $"{teacherUser.FirstName} {teacherUser.LastName}" : "Unknown",
                    ClassNames = classNames.Any() ? string.Join(", ", classNames) : "Все ученики",
                    QuestionsCount = questionsCount,
                    ResultsCount = resultsCount,
                    CreatedAt = t.CreatedAt,
                    IsActive = t.IsActive
                });
            }

            // Пунктуация
            var punctuationTests = await db.QueryAsync<PunctuationTest>("SELECT * FROM PunctuationTests");
            foreach (var t in punctuationTests)
            {
                var teacher = await db.QueryFirstOrDefaultAsync<Teacher>("SELECT * FROM Teachers WHERE UserId = @TeacherId", new { TeacherId = t.TeacherId });
                var teacherUser = teacher != null ? await db.QueryFirstOrDefaultAsync<ApplicationUser>("SELECT * FROM AspNetUsers WHERE Id = @UserId", new { UserId = t.TeacherId }) : null;
                var testClasses = await db.QueryAsync<PunctuationTestClass>("SELECT * FROM PunctuationTestClasses WHERE PunctuationTestId = @TestId", new { TestId = t.Id });
                var questionsCount = await db.QueryScalarAsync<int>("SELECT COUNT(*) FROM PunctuationQuestions WHERE PunctuationTestId = @TestId", new { TestId = t.Id });
                var resultsCount = await db.QueryScalarAsync<int>("SELECT COUNT(*) FROM PunctuationTestResults WHERE PunctuationTestId = @TestId", new { TestId = t.Id });
                
                var classNames = new List<string>();
                foreach (var tc in testClasses)
                {
                    var @class = await db.QueryFirstOrDefaultAsync<Class>("SELECT * FROM Classes WHERE Id = @ClassId", new { ClassId = tc.ClassId });
                    if (@class != null) classNames.Add(@class.Name);
                }

                result.Add(new TestExportInfo
                {
                    Id = t.Id,
                    Title = t.Title,
                    Type = "Пунктуация",
                    TeacherName = teacherUser != null ? $"{teacherUser.FirstName} {teacherUser.LastName}" : "Unknown",
                    ClassNames = classNames.Any() ? string.Join(", ", classNames) : "Все ученики",
                    QuestionsCount = questionsCount,
                    ResultsCount = resultsCount,
                    CreatedAt = t.CreatedAt,
                    IsActive = t.IsActive
                });
            }

            // Орфоэпия
            var orthoeopyTests = await db.QueryAsync<OrthoeopyTest>("SELECT * FROM OrthoeopyTests");
            foreach (var t in orthoeopyTests)
            {
                var teacher = await db.QueryFirstOrDefaultAsync<Teacher>("SELECT * FROM Teachers WHERE UserId = @TeacherId", new { TeacherId = t.TeacherId });
                var teacherUser = teacher != null ? await db.QueryFirstOrDefaultAsync<ApplicationUser>("SELECT * FROM AspNetUsers WHERE Id = @UserId", new { UserId = t.TeacherId }) : null;
                var testClasses = await db.QueryAsync<OrthoeopyTestClass>("SELECT * FROM OrthoeopyTestClasses WHERE OrthoeopyTestId = @TestId", new { TestId = t.Id });
                var questionsCount = await db.QueryScalarAsync<int>("SELECT COUNT(*) FROM OrthoeopyQuestions WHERE OrthoeopyTestId = @TestId", new { TestId = t.Id });
                var resultsCount = await db.QueryScalarAsync<int>("SELECT COUNT(*) FROM OrthoeopyTestResults WHERE OrthoeopyTestId = @TestId", new { TestId = t.Id });
                
                var classNames = new List<string>();
                foreach (var tc in testClasses)
                {
                    var @class = await db.QueryFirstOrDefaultAsync<Class>("SELECT * FROM Classes WHERE Id = @ClassId", new { ClassId = tc.ClassId });
                    if (@class != null) classNames.Add(@class.Name);
                }

                result.Add(new TestExportInfo
                {
                    Id = t.Id,
                    Title = t.Title,
                    Type = "Орфоэпия",
                    TeacherName = teacherUser != null ? $"{teacherUser.FirstName} {teacherUser.LastName}" : "Unknown",
                    ClassNames = classNames.Any() ? string.Join(", ", classNames) : "Все ученики",
                    QuestionsCount = questionsCount,
                    ResultsCount = resultsCount,
                    CreatedAt = t.CreatedAt,
                    IsActive = t.IsActive
                });
            }

            // Сортируем в памяти по дате создания
            result.Sort((a, b) => b.CreatedAt.CompareTo(a.CreatedAt));
            return result;
        }

        /// <summary>
        /// Получает все результаты тестов для экспорта
        /// </summary>
        public static async Task<List<TestResultExportInfo>> GetAllTestResultsForExportAsync(IDatabaseConnection db)
        {
            var result = new List<TestResultExportInfo>();

            // Орфография - получаем через SQL с JOIN
            var spellingResultsSql = @"
                SELECT str.*, st.Title as TestTitle, u.FirstName + ' ' + u.LastName as StudentName
                FROM SpellingTestResults str
                INNER JOIN SpellingTests st ON str.SpellingTestId = st.Id
                INNER JOIN Students s ON str.StudentId = s.Id
                INNER JOIN AspNetUsers u ON s.UserId = u.Id";
            var spellingResults = await db.QueryAsync<dynamic>(spellingResultsSql);
            foreach (var r in spellingResults)
            {
                result.Add(new TestResultExportInfo
                {
                    Id = r.Id,
                    TestTitle = r.TestTitle ?? "Unknown",
                    TestType = "Орфография",
                    StudentName = r.StudentName ?? "Unknown",
                    Score = r.Score,
                    MaxScore = r.MaxScore,
                    Percentage = r.Percentage,
                    StartedAt = r.StartedAt,
                    CompletedAt = r.CompletedAt,
                    IsCompleted = r.IsCompleted
                });
            }

            // Классические тесты
            var regularResultsSql = @"
                SELECT rtr.*, rt.Title as TestTitle, u.FirstName + ' ' + u.LastName as StudentName
                FROM RegularTestResults rtr
                INNER JOIN RegularTests rt ON rtr.RegularTestId = rt.Id
                INNER JOIN Students s ON rtr.StudentId = s.Id
                INNER JOIN AspNetUsers u ON s.UserId = u.Id";
            var regularResults = await db.QueryAsync<dynamic>(regularResultsSql);
            foreach (var r in regularResults)
            {
                result.Add(new TestResultExportInfo
                {
                    Id = r.Id,
                    TestTitle = r.TestTitle ?? "Unknown",
                    TestType = "Классический",
                    StudentName = r.StudentName ?? "Unknown",
                    Score = r.Score,
                    MaxScore = r.MaxScore,
                    Percentage = r.Percentage,
                    StartedAt = r.StartedAt,
                    CompletedAt = r.CompletedAt,
                    IsCompleted = r.IsCompleted
                });
            }

            // Пунктуация
            var punctuationResultsSql = @"
                SELECT ptr.*, pt.Title as TestTitle, u.FirstName + ' ' + u.LastName as StudentName
                FROM PunctuationTestResults ptr
                INNER JOIN PunctuationTests pt ON ptr.PunctuationTestId = pt.Id
                INNER JOIN Students s ON ptr.StudentId = s.Id
                INNER JOIN AspNetUsers u ON s.UserId = u.Id";
            var punctuationResults = await db.QueryAsync<dynamic>(punctuationResultsSql);
            foreach (var r in punctuationResults)
            {
                result.Add(new TestResultExportInfo
                {
                    Id = r.Id,
                    TestTitle = r.TestTitle ?? "Unknown",
                    TestType = "Пунктуация",
                    StudentName = r.StudentName ?? "Unknown",
                    Score = r.Score,
                    MaxScore = r.MaxScore,
                    Percentage = r.Percentage,
                    StartedAt = r.StartedAt,
                    CompletedAt = r.CompletedAt,
                    IsCompleted = r.IsCompleted
                });
            }

            // Орфоэпия
            var orthoeopyResultsSql = @"
                SELECT otr.*, ot.Title as TestTitle, u.FirstName + ' ' + u.LastName as StudentName
                FROM OrthoeopyTestResults otr
                INNER JOIN OrthoeopyTests ot ON otr.OrthoeopyTestId = ot.Id
                INNER JOIN Students s ON otr.StudentId = s.Id
                INNER JOIN AspNetUsers u ON s.UserId = u.Id";
            var orthoeopyResults = await db.QueryAsync<dynamic>(orthoeopyResultsSql);
            foreach (var r in orthoeopyResults)
            {
                result.Add(new TestResultExportInfo
                {
                    Id = r.Id,
                    TestTitle = r.TestTitle ?? "Unknown",
                    TestType = "Орфоэпия",
                    StudentName = r.StudentName ?? "Unknown",
                    Score = r.Score,
                    MaxScore = r.MaxScore,
                    Percentage = r.Percentage,
                    StartedAt = r.StartedAt,
                    CompletedAt = r.CompletedAt,
                    IsCompleted = r.IsCompleted
                });
            }

            // Сортируем в памяти по дате начала
            result.Sort((a, b) => b.StartedAt.CompareTo(a.StartedAt));
            return result;
        }
    }

    /// <summary>
    /// Информация о тесте для экспорта
    /// </summary>
    public class TestExportInfo
    {
        public int Id { get; set; }
        public string Title { get; set; } = string.Empty;
        public string Type { get; set; } = string.Empty;
        public string TeacherName { get; set; } = string.Empty;
        public string ClassNames { get; set; } = string.Empty;
        public int QuestionsCount { get; set; }
        public int ResultsCount { get; set; }
        public DateTime CreatedAt { get; set; }
        public bool IsActive { get; set; }
    }

    /// <summary>
    /// Информация о результате теста для экспорта
    /// </summary>
    public class TestResultExportInfo
    {
        public int Id { get; set; }
        public string TestTitle { get; set; } = string.Empty;
        public string TestType { get; set; } = string.Empty;
        public string StudentName { get; set; } = string.Empty;
        public int Score { get; set; }
        public int MaxScore { get; set; }
        public double Percentage { get; set; }
        public DateTime StartedAt { get; set; }
        public DateTime? CompletedAt { get; set; }
        public bool IsCompleted { get; set; }
    }
}

