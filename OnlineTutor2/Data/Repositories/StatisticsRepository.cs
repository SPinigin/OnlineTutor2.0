using OnlineTutor2.Models;

namespace OnlineTutor2.Data.Repositories
{
    /// <summary>
    /// Реализация репозитория для получения статистики системы
    /// </summary>
    public class StatisticsRepository : IStatisticsRepository
    {
        private readonly IDatabaseConnection _db;

        public StatisticsRepository(IDatabaseConnection db)
        {
            _db = db;
        }

        public async Task<int> GetTotalUsersCountAsync()
        {
            var sql = "SELECT COUNT(*) FROM AspNetUsers";
            var result = await _db.QueryScalarAsync<int?>(sql);
            return result ?? 0;
        }

        public async Task<int> GetTotalStudentsCountAsync()
        {
            var sql = "SELECT COUNT(*) FROM Students";
            var result = await _db.QueryScalarAsync<int?>(sql);
            return result ?? 0;
        }

        public async Task<int> GetTotalTeachersCountAsync()
        {
            var sql = "SELECT COUNT(*) FROM Teachers";
            var result = await _db.QueryScalarAsync<int?>(sql);
            return result ?? 0;
        }

        public async Task<int> GetTotalClassesCountAsync()
        {
            var sql = "SELECT COUNT(*) FROM Classes";
            var result = await _db.QueryScalarAsync<int?>(sql);
            return result ?? 0;
        }

        public async Task<int> GetTotalSpellingTestsCountAsync()
        {
            var sql = "SELECT COUNT(*) FROM SpellingTests";
            var result = await _db.QueryScalarAsync<int?>(sql);
            return result ?? 0;
        }

        public async Task<int> GetTotalRegularTestsCountAsync()
        {
            var sql = "SELECT COUNT(*) FROM RegularTests";
            var result = await _db.QueryScalarAsync<int?>(sql);
            return result ?? 0;
        }

        public async Task<int> GetTotalPunctuationTestsCountAsync()
        {
            var sql = "SELECT COUNT(*) FROM PunctuationTests";
            var result = await _db.QueryScalarAsync<int?>(sql);
            return result ?? 0;
        }

        public async Task<int> GetTotalOrthoeopyTestsCountAsync()
        {
            var sql = "SELECT COUNT(*) FROM OrthoeopyTests";
            var result = await _db.QueryScalarAsync<int?>(sql);
            return result ?? 0;
        }

        public async Task<int> GetTotalTestResultsCountAsync()
        {
            var spellingCount = await GetTotalSpellingResultsCountAsync();
            var regularCount = await GetTotalRegularResultsCountAsync();
            var punctuationCount = await _db.QueryScalarAsync<int?>("SELECT COUNT(*) FROM PunctuationTestResults") ?? 0;
            var orthoeopyCount = await _db.QueryScalarAsync<int?>("SELECT COUNT(*) FROM OrthoeopyTestResults") ?? 0;
            return spellingCount + regularCount + punctuationCount + orthoeopyCount;
        }

        public async Task<int> GetPendingTeachersCountAsync()
        {
            var sql = "SELECT COUNT(*) FROM Teachers WHERE IsApproved = 0";
            var result = await _db.QueryScalarAsync<int?>(sql);
            return result ?? 0;
        }

        public async Task<List<ApplicationUser>> GetRecentUsersAsync(int count = 5)
        {
            var sql = $@"
                SELECT TOP {count} * 
                FROM AspNetUsers 
                ORDER BY CreatedAt DESC";
            return await _db.QueryAsync<ApplicationUser>(sql);
        }

        public async Task<List<SpellingTest>> GetRecentSpellingTestsAsync(int count = 5)
        {
            var sql = $@"
                SELECT TOP {count} st.*, 
                       t.Id as Teacher_Id, t.UserId as Teacher_UserId, t.Subject as Teacher_Subject,
                       t.Education as Teacher_Education, t.Experience as Teacher_Experience,
                       t.IsApproved as Teacher_IsApproved, t.CreatedAt as Teacher_CreatedAt
                FROM SpellingTests st
                INNER JOIN Teachers t ON st.TeacherId = t.UserId
                ORDER BY st.CreatedAt DESC";
            return await _db.QueryAsync<SpellingTest>(sql);
        }

        public async Task<Dictionary<DateTime, int>> GetUserRegistrationsByDateAsync(DateTime fromDate)
        {
            var sql = @"
                SELECT CAST(CreatedAt AS DATE) as Date, COUNT(*) as Count
                FROM AspNetUsers
                WHERE CreatedAt >= @FromDate
                GROUP BY CAST(CreatedAt AS DATE)
                ORDER BY CAST(CreatedAt AS DATE)";
            var results = await _db.QueryAsync<dynamic>(sql, new { FromDate = fromDate });
            
            var dict = new Dictionary<DateTime, int>();
            foreach (var r in results)
            {
                dict[((DateTime)r.Date).Date] = (int)r.Count;
            }
            return dict;
        }

        public async Task<Dictionary<DateTime, int>> GetTestResultsByDateAsync(DateTime fromDate, string testType)
        {
            string tableName = testType switch
            {
                "spelling" => "SpellingTestResults",
                "regular" => "RegularTestResults",
                "punctuation" => "PunctuationTestResults",
                "orthoeopy" => "OrthoeopyTestResults",
                _ => throw new ArgumentException($"Unknown test type: {testType}")
            };

            var sql = $@"
                SELECT CAST(StartedAt AS DATE) as Date, COUNT(*) as Count
                FROM {tableName}
                WHERE StartedAt >= @FromDate AND IsCompleted = 1
                GROUP BY CAST(StartedAt AS DATE)";
            var results = await _db.QueryAsync<dynamic>(sql, new { FromDate = fromDate });
            
            var dict = new Dictionary<DateTime, int>();
            foreach (var r in results)
            {
                dict[((DateTime)r.Date).Date] = (int)r.Count;
            }
            return dict;
        }

        public async Task<Dictionary<string, int>> GetTestsByTypeAsync()
        {
            return new Dictionary<string, int>
            {
                { "Орфография", await GetTotalSpellingTestsCountAsync() },
                { "Классические", await GetTotalRegularTestsCountAsync() },
                { "Пунктуация", await GetTotalPunctuationTestsCountAsync() },
                { "Орфоэпия", await GetTotalOrthoeopyTestsCountAsync() }
            };
        }

        public async Task<Dictionary<string, double>> GetAverageScoresByTypeAsync()
        {
            var spellingAvg = await _db.QueryScalarAsync<double?>("SELECT AVG(CAST(Percentage AS FLOAT)) FROM SpellingTestResults WHERE IsCompleted = 1") ?? 0;
            var regularAvg = await _db.QueryScalarAsync<double?>("SELECT AVG(CAST(Percentage AS FLOAT)) FROM RegularTestResults WHERE IsCompleted = 1") ?? 0;
            var punctuationAvg = await _db.QueryScalarAsync<double?>("SELECT AVG(CAST(Percentage AS FLOAT)) FROM PunctuationTestResults WHERE IsCompleted = 1") ?? 0;
            var orthoeopyAvg = await _db.QueryScalarAsync<double?>("SELECT AVG(CAST(Percentage AS FLOAT)) FROM OrthoeopyTestResults WHERE IsCompleted = 1") ?? 0;

            return new Dictionary<string, double>
            {
                { "Орфография", Math.Round(spellingAvg, 1) },
                { "Классические", Math.Round(regularAvg, 1) },
                { "Пунктуация", Math.Round(punctuationAvg, 1) },
                { "Орфоэпия", Math.Round(orthoeopyAvg, 1) }
            };
        }

        public async Task<Dictionary<string, int>> GetAdminActionsByTypeAsync(DateTime fromDate)
        {
            var sql = @"
                SELECT TOP 10 Action, COUNT(*) as Count
                FROM AuditLogs
                WHERE CreatedAt >= @FromDate
                GROUP BY Action
                ORDER BY COUNT(*) DESC";
            var results = await _db.QueryAsync<dynamic>(sql, new { FromDate = fromDate });
            
            var dict = new Dictionary<string, int>();
            foreach (var r in results)
            {
                dict[(string)r.Action] = (int)r.Count;
            }
            return dict;
        }

        public async Task<Dictionary<string, int>> GetActivityByDayOfWeekAsync(DateTime fromDate)
        {
            var sql = @"
                SELECT DATEPART(WEEKDAY, CreatedAt) as DayOfWeek, COUNT(*) as Count
                FROM AuditLogs
                WHERE CreatedAt >= @FromDate
                GROUP BY DATEPART(WEEKDAY, CreatedAt)";
            var results = await _db.QueryAsync<dynamic>(sql, new { FromDate = fromDate });

            var dayNames = new[] { "Понедельник", "Вторник", "Среда", "Четверг", "Пятница", "Суббота", "Воскресенье" };
            var activityDict = new Dictionary<int, int>();
            foreach (var item in results)
            {
                var dayOfWeek = (int)item.DayOfWeek;
                // SQL Server: 1=Sunday, 2=Monday, ..., 7=Saturday. Конвертируем в 0=Monday, ..., 6=Sunday
                var adjustedDay = dayOfWeek == 1 ? 6 : dayOfWeek - 2;
                activityDict[adjustedDay] = (int)item.Count;
            }
            
            var resultDict = new Dictionary<string, int>();
            for (int i = 0; i < dayNames.Length; i++)
            {
                resultDict[dayNames[i]] = activityDict.ContainsKey(i) ? activityDict[i] : 0;
            }
            return resultDict;
        }

        public async Task<int> GetActiveUsersCountAsync()
        {
            var sql = "SELECT COUNT(*) FROM AspNetUsers WHERE IsActive = 1";
            var result = await _db.QueryScalarAsync<int?>(sql);
            return result ?? 0;
        }

        public async Task<int> GetInactiveUsersCountAsync()
        {
            var sql = "SELECT COUNT(*) FROM AspNetUsers WHERE IsActive = 0";
            var result = await _db.QueryScalarAsync<int?>(sql);
            return result ?? 0;
        }

        public async Task<Dictionary<string, int>> GetTopTeachersByTestsAsync(int count = 5)
        {
            var sql = $@"
                SELECT TOP {count} 
                    u.FirstName + ' ' + u.LastName as Name,
                    (SELECT COUNT(*) FROM RegularTests WHERE TeacherId = t.UserId) +
                    (SELECT COUNT(*) FROM SpellingTests WHERE TeacherId = t.UserId) +
                    (SELECT COUNT(*) FROM PunctuationTests WHERE TeacherId = t.UserId) +
                    (SELECT COUNT(*) FROM OrthoeopyTests WHERE TeacherId = t.UserId) as TestsCount
                FROM Teachers t
                INNER JOIN AspNetUsers u ON t.UserId = u.Id
                ORDER BY TestsCount DESC";
            var results = await _db.QueryAsync<dynamic>(sql);
            
            var dict = new Dictionary<string, int>();
            foreach (var r in results)
            {
                dict[(string)r.Name] = (int)r.TestsCount;
            }
            return dict;
        }

        public async Task<Dictionary<string, int>> GetTopStudentsByResultsAsync(int count = 5)
        {
            var sql = $@"
                SELECT TOP {count}
                    u.FirstName + ' ' + u.LastName as Name,
                    (SELECT COUNT(*) FROM RegularTestResults WHERE StudentId = s.Id AND IsCompleted = 1) +
                    (SELECT COUNT(*) FROM SpellingTestResults WHERE StudentId = s.Id AND IsCompleted = 1) +
                    (SELECT COUNT(*) FROM PunctuationTestResults WHERE StudentId = s.Id AND IsCompleted = 1) +
                    (SELECT COUNT(*) FROM OrthoeopyTestResults WHERE StudentId = s.Id AND IsCompleted = 1) as ResultsCount
                FROM Students s
                INNER JOIN AspNetUsers u ON s.UserId = u.Id
                ORDER BY ResultsCount DESC";
            var results = await _db.QueryAsync<dynamic>(sql);
            
            var dict = new Dictionary<string, int>();
            foreach (var r in results)
            {
                dict[(string)r.Name] = (int)r.ResultsCount;
            }
            return dict;
        }

        public async Task<int> GetTotalSpellingQuestionsCountAsync()
        {
            var sql = "SELECT COUNT(*) FROM SpellingQuestions";
            var result = await _db.QueryScalarAsync<int?>(sql);
            return result ?? 0;
        }

        public async Task<int> GetTotalRegularQuestionsCountAsync()
        {
            var sql = "SELECT COUNT(*) FROM RegularQuestions";
            var result = await _db.QueryScalarAsync<int?>(sql);
            return result ?? 0;
        }

        public async Task<int> GetTotalSpellingResultsCountAsync()
        {
            var sql = "SELECT COUNT(*) FROM SpellingTestResults";
            var result = await _db.QueryScalarAsync<int?>(sql);
            return result ?? 0;
        }

        public async Task<int> GetTotalRegularResultsCountAsync()
        {
            var sql = "SELECT COUNT(*) FROM RegularTestResults";
            var result = await _db.QueryScalarAsync<int?>(sql);
            return result ?? 0;
        }

        public async Task<int> GetTotalMaterialsCountAsync()
        {
            var sql = "SELECT COUNT(*) FROM Materials";
            var result = await _db.QueryScalarAsync<int?>(sql);
            return result ?? 0;
        }
    }
}





