using OnlineTutor2.Data;
using OnlineTutor2.Models;

namespace OnlineTutor2.Data.Repositories
{
    public class SpellingTestResultRepository : BaseRepository<SpellingTestResult>, ISpellingTestResultRepository
    {
        public SpellingTestResultRepository(IDatabaseConnection db) : base(db, "SpellingTestResults")
        {
        }

        public async Task<List<SpellingTestResult>> GetByTestIdAsync(int testId)
        {
            var sql = "SELECT * FROM SpellingTestResults WHERE SpellingTestId = @TestId ORDER BY StartedAt DESC";
            return await _db.QueryAsync<SpellingTestResult>(sql, new { TestId = testId });
        }

        public async Task<List<SpellingTestResult>> GetByStudentIdAsync(int studentId)
        {
            var sql = "SELECT * FROM SpellingTestResults WHERE StudentId = @StudentId ORDER BY StartedAt DESC";
            return await _db.QueryAsync<SpellingTestResult>(sql, new { StudentId = studentId });
        }

        public async Task<List<SpellingTestResult>> GetCompletedByStudentIdAsync(int studentId)
        {
            var sql = "SELECT * FROM SpellingTestResults WHERE StudentId = @StudentId AND CompletedAt IS NOT NULL ORDER BY StartedAt DESC";
            return await _db.QueryAsync<SpellingTestResult>(sql, new { StudentId = studentId });
        }

        public async Task<int> GetTotalCompletedCountByStudentIdAsync(int studentId)
        {
            var sql = "SELECT COUNT(*) FROM SpellingTestResults WHERE StudentId = @StudentId AND CompletedAt IS NOT NULL";
            var result = await _db.QueryScalarAsync<int?>(sql, new { StudentId = studentId });
            return result ?? 0;
        }

        public async Task<int> GetTotalScoreByStudentIdAsync(int studentId)
        {
            var sql = "SELECT ISNULL(SUM(Score), 0) FROM SpellingTestResults WHERE StudentId = @StudentId AND CompletedAt IS NOT NULL";
            var result = await _db.QueryScalarAsync<int?>(sql, new { StudentId = studentId });
            return result ?? 0;
        }

        public async Task<double> GetAveragePercentageByStudentIdAsync(int studentId)
        {
            var sql = "SELECT AVG(CAST(Percentage AS FLOAT)) FROM SpellingTestResults WHERE StudentId = @StudentId AND CompletedAt IS NOT NULL";
            var result = await _db.QueryScalarAsync<double?>(sql, new { StudentId = studentId });
            return result ?? 0;
        }

        public async Task<int> GetCountByTestIdAsync(int testId)
        {
            var sql = "SELECT COUNT(*) FROM SpellingTestResults WHERE SpellingTestId = @TestId";
            var result = await _db.QueryScalarAsync<int?>(sql, new { TestId = testId });
            return result ?? 0;
        }

        public async Task<List<SpellingTestResult>> GetByTeacherIdWithDetailsAsync(string teacherId)
        {
            // Получаем результаты с JOIN для получения данных теста и студента
            var sql = @"
                SELECT TOP 50
                    str.*,
                    st.Id as Test_Id, st.Title as Test_Title, st.TeacherId as Test_TeacherId,
                    s.Id as Student_Id, s.UserId as Student_UserId,
                    u.Id as User_Id, u.FirstName as User_FirstName, u.LastName as User_LastName
                FROM SpellingTestResults str
                INNER JOIN SpellingTests st ON str.SpellingTestId = st.Id
                INNER JOIN Students s ON str.StudentId = s.Id
                INNER JOIN AspNetUsers u ON s.UserId = u.Id
                WHERE st.TeacherId = @TeacherId
                ORDER BY COALESCE(str.CompletedAt, str.StartedAt) DESC";
            
            var results = await _db.QueryAsync<SpellingTestResult>(sql, new { TeacherId = teacherId });
            // TODO: Маппинг навигационных свойств требует дополнительной логики
            return results;
        }

        public async Task<SpellingTestResult?> GetByIdWithDetailsAsync(int id, string teacherId)
        {
            var sql = @"
                SELECT 
                    str.*,
                    st.Id as Test_Id, st.Title as Test_Title, st.TeacherId as Test_TeacherId,
                    s.Id as Student_Id, s.UserId as Student_UserId,
                    u.Id as User_Id, u.FirstName as User_FirstName, u.LastName as User_LastName
                FROM SpellingTestResults str
                INNER JOIN SpellingTests st ON str.SpellingTestId = st.Id
                INNER JOIN Students s ON str.StudentId = s.Id
                INNER JOIN AspNetUsers u ON s.UserId = u.Id
                WHERE str.Id = @Id AND st.TeacherId = @TeacherId";
            
            return await _db.QueryFirstOrDefaultAsync<SpellingTestResult>(sql, new { Id = id, TeacherId = teacherId });
        }

        public override async Task<int> CreateAsync(SpellingTestResult entity)
        {
            var sql = @"
                INSERT INTO SpellingTestResults (
                    SpellingTestId, StudentId, StartedAt, CompletedAt, Score, MaxScore,
                    Percentage, IsCompleted, AttemptNumber
                )
                OUTPUT INSERTED.Id
                VALUES (
                    @SpellingTestId, @StudentId, @StartedAt, @CompletedAt, @Score, @MaxScore,
                    @Percentage, @IsCompleted, @AttemptNumber
                )";
            
            return await _db.QueryScalarAsync<int>(sql, entity);
        }

        public override async Task<int> UpdateAsync(SpellingTestResult entity)
        {
            var sql = @"
                UPDATE SpellingTestResults 
                SET CompletedAt = @CompletedAt, Score = @Score, MaxScore = @MaxScore,
                    Percentage = @Percentage, IsCompleted = @IsCompleted
                WHERE Id = @Id";
            return await _db.ExecuteAsync(sql, entity);
        }

        // Analytics methods
        public async Task<List<int>> GetDistinctStudentIdsByTestIdAsync(int testId)
        {
            var sql = "SELECT DISTINCT StudentId AS Id FROM SpellingTestResults WHERE SpellingTestId = @TestId";
            var results = await _db.QueryAsync<IntId>(sql, new { TestId = testId });
            return results.Select(r => r.Id).ToList();
        }

        public async Task<int> GetDistinctStudentsCountByTestIdAsync(int testId)
        {
            var sql = "SELECT COUNT(DISTINCT StudentId) FROM SpellingTestResults WHERE SpellingTestId = @TestId";
            var result = await _db.QueryScalarAsync<int?>(sql, new { TestId = testId });
            return result ?? 0;
        }

        public async Task<int> GetCompletedStudentsCountByTestIdAsync(int testId)
        {
            var sql = "SELECT COUNT(DISTINCT StudentId) FROM SpellingTestResults WHERE SpellingTestId = @TestId AND IsCompleted = 1";
            var result = await _db.QueryScalarAsync<int?>(sql, new { TestId = testId });
            return result ?? 0;
        }

        public async Task<int> GetInProgressStudentsCountByTestIdAsync(int testId)
        {
            var sql = "SELECT COUNT(DISTINCT StudentId) FROM SpellingTestResults WHERE SpellingTestId = @TestId AND IsCompleted = 0";
            var result = await _db.QueryScalarAsync<int?>(sql, new { TestId = testId });
            return result ?? 0;
        }

        public async Task<List<SpellingTestResult>> GetCompletedByTestIdAsync(int testId)
        {
            var sql = "SELECT * FROM SpellingTestResults WHERE SpellingTestId = @TestId AND IsCompleted = 1";
            return await _db.QueryAsync<SpellingTestResult>(sql, new { TestId = testId });
        }

        public async Task<List<SpellingTestResult>> GetInProgressByTestIdAsync(int testId)
        {
            var sql = "SELECT * FROM SpellingTestResults WHERE SpellingTestId = @TestId AND IsCompleted = 0";
            return await _db.QueryAsync<SpellingTestResult>(sql, new { TestId = testId });
        }

        public async Task<double> GetAverageScoreByTestIdAsync(int testId)
        {
            var sql = "SELECT AVG(CAST(Score AS FLOAT)) FROM SpellingTestResults WHERE SpellingTestId = @TestId AND IsCompleted = 1";
            var result = await _db.QueryScalarAsync<double?>(sql, new { TestId = testId });
            return result ?? 0;
        }

        public async Task<double> GetAveragePercentageByTestIdAsync(int testId)
        {
            var sql = "SELECT AVG(CAST(Percentage AS FLOAT)) FROM SpellingTestResults WHERE SpellingTestId = @TestId AND IsCompleted = 1";
            var result = await _db.QueryScalarAsync<double?>(sql, new { TestId = testId });
            return result ?? 0;
        }

        public async Task<int> GetHighestScoreByTestIdAsync(int testId)
        {
            var sql = "SELECT MAX(Score) FROM SpellingTestResults WHERE SpellingTestId = @TestId AND IsCompleted = 1";
            var result = await _db.QueryScalarAsync<int?>(sql, new { TestId = testId });
            return result ?? 0;
        }

        public async Task<int> GetLowestScoreByTestIdAsync(int testId)
        {
            var sql = "SELECT MIN(Score) FROM SpellingTestResults WHERE SpellingTestId = @TestId AND IsCompleted = 1";
            var result = await _db.QueryScalarAsync<int?>(sql, new { TestId = testId });
            return result ?? 0;
        }

        public async Task<DateTime?> GetFirstCompletionByTestIdAsync(int testId)
        {
            var sql = "SELECT MIN(CompletedAt) FROM SpellingTestResults WHERE SpellingTestId = @TestId AND IsCompleted = 1";
            return await _db.QueryScalarAsync<DateTime?>(sql, new { TestId = testId });
        }

        public async Task<DateTime?> GetLastCompletionByTestIdAsync(int testId)
        {
            var sql = "SELECT MAX(CompletedAt) FROM SpellingTestResults WHERE SpellingTestId = @TestId AND IsCompleted = 1";
            return await _db.QueryScalarAsync<DateTime?>(sql, new { TestId = testId });
        }

        public async Task<double?> GetAverageCompletionTimeByTestIdAsync(int testId)
        {
            var sql = @"
                SELECT AVG(CAST(DATEDIFF(SECOND, StartedAt, CompletedAt) AS FLOAT))
                FROM SpellingTestResults 
                WHERE SpellingTestId = @TestId AND IsCompleted = 1 AND CompletedAt IS NOT NULL";
            return await _db.QueryScalarAsync<double?>(sql, new { TestId = testId });
        }

        public async Task<Dictionary<string, int>> GetGradeDistributionByTestIdAsync(int testId)
        {
            var excellentSql = "SELECT COUNT(*) FROM SpellingTestResults WHERE SpellingTestId = @TestId AND IsCompleted = 1 AND Percentage >= 80";
            var goodSql = "SELECT COUNT(*) FROM SpellingTestResults WHERE SpellingTestId = @TestId AND IsCompleted = 1 AND Percentage >= 60 AND Percentage < 80";
            var satisfactorySql = "SELECT COUNT(*) FROM SpellingTestResults WHERE SpellingTestId = @TestId AND IsCompleted = 1 AND Percentage >= 40 AND Percentage < 60";
            var unsatisfactorySql = "SELECT COUNT(*) FROM SpellingTestResults WHERE SpellingTestId = @TestId AND IsCompleted = 1 AND Percentage < 40";

            return new Dictionary<string, int>
            {
                ["Отлично (80-100%)"] = await _db.QueryScalarAsync<int>(excellentSql, new { TestId = testId }),
                ["Хорошо (60-79%)"] = await _db.QueryScalarAsync<int>(goodSql, new { TestId = testId }),
                ["Удовлетворительно (40-59%)"] = await _db.QueryScalarAsync<int>(satisfactorySql, new { TestId = testId }),
                ["Неудовлетворительно (0-39%)"] = await _db.QueryScalarAsync<int>(unsatisfactorySql, new { TestId = testId })
            };
        }

        public async Task<List<SpellingTestResult>> GetByStudentAndTestIdAsync(int studentId, int testId)
        {
            var sql = "SELECT * FROM SpellingTestResults WHERE StudentId = @StudentId AND SpellingTestId = @TestId";
            return await _db.QueryAsync<SpellingTestResult>(sql, new { StudentId = studentId, TestId = testId });
        }

        public async Task<List<SpellingTestResult>> GetCompletedByStudentAndTestIdAsync(int studentId, int testId)
        {
            var sql = "SELECT * FROM SpellingTestResults WHERE StudentId = @StudentId AND SpellingTestId = @TestId AND IsCompleted = 1";
            return await _db.QueryAsync<SpellingTestResult>(sql, new { StudentId = studentId, TestId = testId });
        }

        public async Task<bool> HasCompletedByStudentAndTestIdAsync(int studentId, int testId)
        {
            var sql = "SELECT COUNT(*) FROM SpellingTestResults WHERE StudentId = @StudentId AND SpellingTestId = @TestId AND IsCompleted = 1";
            var count = await _db.QueryScalarAsync<int>(sql, new { StudentId = studentId, TestId = testId });
            return count > 0;
        }

        public async Task<bool> IsInProgressByStudentAndTestIdAsync(int studentId, int testId)
        {
            var sql = "SELECT COUNT(*) FROM SpellingTestResults WHERE StudentId = @StudentId AND SpellingTestId = @TestId AND IsCompleted = 0";
            var count = await _db.QueryScalarAsync<int>(sql, new { StudentId = studentId, TestId = testId });
            return count > 0;
        }

        public async Task<SpellingTestResult?> GetBestResultByStudentAndTestIdAsync(int studentId, int testId)
        {
            var sql = "SELECT TOP 1 * FROM SpellingTestResults WHERE StudentId = @StudentId AND SpellingTestId = @TestId AND IsCompleted = 1 ORDER BY Percentage DESC";
            return await _db.QueryFirstOrDefaultAsync<SpellingTestResult>(sql, new { StudentId = studentId, TestId = testId });
        }

        public async Task<SpellingTestResult?> GetLatestResultByStudentAndTestIdAsync(int studentId, int testId)
        {
            var sql = "SELECT TOP 1 * FROM SpellingTestResults WHERE StudentId = @StudentId AND SpellingTestId = @TestId AND IsCompleted = 1 ORDER BY CompletedAt DESC";
            return await _db.QueryFirstOrDefaultAsync<SpellingTestResult>(sql, new { StudentId = studentId, TestId = testId });
        }

        public async Task<long?> GetTotalTimeSpentByStudentAndTestIdAsync(int studentId, int testId)
        {
            var sql = @"
                SELECT SUM(DATEDIFF(SECOND, StartedAt, CompletedAt))
                FROM SpellingTestResults 
                WHERE StudentId = @StudentId AND SpellingTestId = @TestId AND IsCompleted = 1 AND CompletedAt IS NOT NULL";
            return await _db.QueryScalarAsync<long?>(sql, new { StudentId = studentId, TestId = testId });
        }
    }
}

