using OnlineTutor2.Data;
using OnlineTutor2.Models;

namespace OnlineTutor2.Data.Repositories
{
    public class PunctuationTestResultRepository : BaseRepository<PunctuationTestResult>, IPunctuationTestResultRepository
    {
        public PunctuationTestResultRepository(IDatabaseConnection db) : base(db, "PunctuationTestResults")
        {
        }

        public async Task<List<PunctuationTestResult>> GetByTestIdAsync(int testId)
        {
            var sql = "SELECT * FROM PunctuationTestResults WHERE PunctuationTestId = @TestId ORDER BY StartedAt DESC";
            return await _db.QueryAsync<PunctuationTestResult>(sql, new { TestId = testId });
        }

        public async Task<List<PunctuationTestResult>> GetByStudentIdAsync(int studentId)
        {
            var sql = "SELECT * FROM PunctuationTestResults WHERE StudentId = @StudentId ORDER BY StartedAt DESC";
            return await _db.QueryAsync<PunctuationTestResult>(sql, new { StudentId = studentId });
        }

        public async Task<List<PunctuationTestResult>> GetCompletedByStudentIdAsync(int studentId)
        {
            var sql = "SELECT * FROM PunctuationTestResults WHERE StudentId = @StudentId AND CompletedAt IS NOT NULL ORDER BY StartedAt DESC";
            return await _db.QueryAsync<PunctuationTestResult>(sql, new { StudentId = studentId });
        }

        public async Task<int> GetTotalCompletedCountByStudentIdAsync(int studentId)
        {
            var sql = "SELECT COUNT(*) FROM PunctuationTestResults WHERE StudentId = @StudentId AND CompletedAt IS NOT NULL";
            var result = await _db.QueryScalarAsync<int?>(sql, new { StudentId = studentId });
            return result ?? 0;
        }

        public async Task<int> GetTotalScoreByStudentIdAsync(int studentId)
        {
            var sql = "SELECT ISNULL(SUM(Score), 0) FROM PunctuationTestResults WHERE StudentId = @StudentId AND CompletedAt IS NOT NULL";
            var result = await _db.QueryScalarAsync<int?>(sql, new { StudentId = studentId });
            return result ?? 0;
        }

        public async Task<double> GetAveragePercentageByStudentIdAsync(int studentId)
        {
            var sql = "SELECT AVG(CAST(Percentage AS FLOAT)) FROM PunctuationTestResults WHERE StudentId = @StudentId AND CompletedAt IS NOT NULL";
            var result = await _db.QueryScalarAsync<double?>(sql, new { StudentId = studentId });
            return result ?? 0;
        }

        public async Task<int> GetCountByTestIdAsync(int testId)
        {
            var sql = "SELECT COUNT(*) FROM PunctuationTestResults WHERE PunctuationTestId = @TestId";
            var result = await _db.QueryScalarAsync<int?>(sql, new { TestId = testId });
            return result ?? 0;
        }

        public async Task<List<PunctuationTestResult>> GetByTeacherIdWithDetailsAsync(string teacherId)
        {
            var sql = @"
                SELECT TOP 50
                    ptr.*,
                    pt.Id as Test_Id, pt.Title as Test_Title, pt.TeacherId as Test_TeacherId,
                    s.Id as Student_Id, s.UserId as Student_UserId,
                    u.Id as User_Id, u.FirstName as User_FirstName, u.LastName as User_LastName
                FROM PunctuationTestResults ptr
                INNER JOIN PunctuationTests pt ON ptr.PunctuationTestId = pt.Id
                INNER JOIN Students s ON ptr.StudentId = s.Id
                INNER JOIN AspNetUsers u ON s.UserId = u.Id
                WHERE pt.TeacherId = @TeacherId
                ORDER BY COALESCE(ptr.CompletedAt, ptr.StartedAt) DESC";
            
            return await _db.QueryAsync<PunctuationTestResult>(sql, new { TeacherId = teacherId });
        }

        public async Task<PunctuationTestResult?> GetByIdWithDetailsAsync(int id, string teacherId)
        {
            var sql = @"
                SELECT 
                    ptr.*,
                    pt.Id as Test_Id, pt.Title as Test_Title, pt.TeacherId as Test_TeacherId,
                    s.Id as Student_Id, s.UserId as Student_UserId,
                    u.Id as User_Id, u.FirstName as User_FirstName, u.LastName as User_LastName
                FROM PunctuationTestResults ptr
                INNER JOIN PunctuationTests pt ON ptr.PunctuationTestId = pt.Id
                INNER JOIN Students s ON ptr.StudentId = s.Id
                INNER JOIN AspNetUsers u ON s.UserId = u.Id
                WHERE ptr.Id = @Id AND pt.TeacherId = @TeacherId";
            
            return await _db.QueryFirstOrDefaultAsync<PunctuationTestResult>(sql, new { Id = id, TeacherId = teacherId });
        }

        public override async Task<int> CreateAsync(PunctuationTestResult entity)
        {
            var sql = @"
                INSERT INTO PunctuationTestResults (
                    PunctuationTestId, StudentId, StartedAt, CompletedAt, Score, MaxScore,
                    Percentage, IsCompleted, AttemptNumber
                )
                OUTPUT INSERTED.Id
                VALUES (
                    @PunctuationTestId, @StudentId, @StartedAt, @CompletedAt, @Score, @MaxScore,
                    @Percentage, @IsCompleted, @AttemptNumber
                )";
            return await _db.QueryScalarAsync<int>(sql, entity);
        }

        public override async Task<int> UpdateAsync(PunctuationTestResult entity)
        {
            var sql = @"
                UPDATE PunctuationTestResults 
                SET CompletedAt = @CompletedAt, Score = @Score, MaxScore = @MaxScore,
                    Percentage = @Percentage, IsCompleted = @IsCompleted
                WHERE Id = @Id";
            return await _db.ExecuteAsync(sql, entity);
        }

        // Analytics methods
        public async Task<List<int>> GetDistinctStudentIdsByTestIdAsync(int testId)
        {
            var sql = "SELECT DISTINCT StudentId AS Id FROM PunctuationTestResults WHERE PunctuationTestId = @TestId";
            var results = await _db.QueryAsync<IntId>(sql, new { TestId = testId });
            return results.Select(r => r.Id).ToList();
        }

        public async Task<int> GetDistinctStudentsCountByTestIdAsync(int testId)
        {
            var sql = "SELECT COUNT(DISTINCT StudentId) FROM PunctuationTestResults WHERE PunctuationTestId = @TestId";
            var result = await _db.QueryScalarAsync<int?>(sql, new { TestId = testId });
            return result ?? 0;
        }

        public async Task<int> GetCompletedStudentsCountByTestIdAsync(int testId)
        {
            var sql = "SELECT COUNT(DISTINCT StudentId) FROM PunctuationTestResults WHERE PunctuationTestId = @TestId AND IsCompleted = 1";
            var result = await _db.QueryScalarAsync<int?>(sql, new { TestId = testId });
            return result ?? 0;
        }

        public async Task<int> GetInProgressStudentsCountByTestIdAsync(int testId)
        {
            var sql = "SELECT COUNT(DISTINCT StudentId) FROM PunctuationTestResults WHERE PunctuationTestId = @TestId AND IsCompleted = 0";
            var result = await _db.QueryScalarAsync<int?>(sql, new { TestId = testId });
            return result ?? 0;
        }

        public async Task<List<PunctuationTestResult>> GetCompletedByTestIdAsync(int testId)
        {
            var sql = "SELECT * FROM PunctuationTestResults WHERE PunctuationTestId = @TestId AND IsCompleted = 1";
            return await _db.QueryAsync<PunctuationTestResult>(sql, new { TestId = testId });
        }

        public async Task<List<PunctuationTestResult>> GetInProgressByTestIdAsync(int testId)
        {
            var sql = "SELECT * FROM PunctuationTestResults WHERE PunctuationTestId = @TestId AND IsCompleted = 0";
            return await _db.QueryAsync<PunctuationTestResult>(sql, new { TestId = testId });
        }

        public async Task<double> GetAverageScoreByTestIdAsync(int testId)
        {
            var sql = "SELECT AVG(CAST(Score AS FLOAT)) FROM PunctuationTestResults WHERE PunctuationTestId = @TestId AND IsCompleted = 1";
            var result = await _db.QueryScalarAsync<double?>(sql, new { TestId = testId });
            return result ?? 0;
        }

        public async Task<double> GetAveragePercentageByTestIdAsync(int testId)
        {
            var sql = "SELECT AVG(CAST(Percentage AS FLOAT)) FROM PunctuationTestResults WHERE PunctuationTestId = @TestId AND IsCompleted = 1";
            var result = await _db.QueryScalarAsync<double?>(sql, new { TestId = testId });
            return result ?? 0;
        }

        public async Task<int> GetHighestScoreByTestIdAsync(int testId)
        {
            var sql = "SELECT MAX(Score) FROM PunctuationTestResults WHERE PunctuationTestId = @TestId AND IsCompleted = 1";
            var result = await _db.QueryScalarAsync<int?>(sql, new { TestId = testId });
            return result ?? 0;
        }

        public async Task<int> GetLowestScoreByTestIdAsync(int testId)
        {
            var sql = "SELECT MIN(Score) FROM PunctuationTestResults WHERE PunctuationTestId = @TestId AND IsCompleted = 1";
            var result = await _db.QueryScalarAsync<int?>(sql, new { TestId = testId });
            return result ?? 0;
        }

        public async Task<DateTime?> GetFirstCompletionByTestIdAsync(int testId)
        {
            var sql = "SELECT MIN(CompletedAt) FROM PunctuationTestResults WHERE PunctuationTestId = @TestId AND IsCompleted = 1";
            return await _db.QueryScalarAsync<DateTime?>(sql, new { TestId = testId });
        }

        public async Task<DateTime?> GetLastCompletionByTestIdAsync(int testId)
        {
            var sql = "SELECT MAX(CompletedAt) FROM PunctuationTestResults WHERE PunctuationTestId = @TestId AND IsCompleted = 1";
            return await _db.QueryScalarAsync<DateTime?>(sql, new { TestId = testId });
        }

        public async Task<double?> GetAverageCompletionTimeByTestIdAsync(int testId)
        {
            var sql = @"
                SELECT AVG(CAST(DATEDIFF(SECOND, StartedAt, CompletedAt) AS FLOAT))
                FROM PunctuationTestResults 
                WHERE PunctuationTestId = @TestId AND IsCompleted = 1 AND CompletedAt IS NOT NULL";
            return await _db.QueryScalarAsync<double?>(sql, new { TestId = testId });
        }

        public async Task<Dictionary<string, int>> GetGradeDistributionByTestIdAsync(int testId)
        {
            var excellentSql = "SELECT COUNT(*) FROM PunctuationTestResults WHERE PunctuationTestId = @TestId AND IsCompleted = 1 AND Percentage >= 80";
            var goodSql = "SELECT COUNT(*) FROM PunctuationTestResults WHERE PunctuationTestId = @TestId AND IsCompleted = 1 AND Percentage >= 60 AND Percentage < 80";
            var satisfactorySql = "SELECT COUNT(*) FROM PunctuationTestResults WHERE PunctuationTestId = @TestId AND IsCompleted = 1 AND Percentage >= 40 AND Percentage < 60";
            var unsatisfactorySql = "SELECT COUNT(*) FROM PunctuationTestResults WHERE PunctuationTestId = @TestId AND IsCompleted = 1 AND Percentage < 40";

            return new Dictionary<string, int>
            {
                ["Отлично (80-100%)"] = await _db.QueryScalarAsync<int>(excellentSql, new { TestId = testId }),
                ["Хорошо (60-79%)"] = await _db.QueryScalarAsync<int>(goodSql, new { TestId = testId }),
                ["Удовлетворительно (40-59%)"] = await _db.QueryScalarAsync<int>(satisfactorySql, new { TestId = testId }),
                ["Неудовлетворительно (0-39%)"] = await _db.QueryScalarAsync<int>(unsatisfactorySql, new { TestId = testId })
            };
        }

        public async Task<List<PunctuationTestResult>> GetByStudentAndTestIdAsync(int studentId, int testId)
        {
            var sql = "SELECT * FROM PunctuationTestResults WHERE StudentId = @StudentId AND PunctuationTestId = @TestId";
            return await _db.QueryAsync<PunctuationTestResult>(sql, new { StudentId = studentId, TestId = testId });
        }

        public async Task<List<PunctuationTestResult>> GetCompletedByStudentAndTestIdAsync(int studentId, int testId)
        {
            var sql = "SELECT * FROM PunctuationTestResults WHERE StudentId = @StudentId AND PunctuationTestId = @TestId AND IsCompleted = 1";
            return await _db.QueryAsync<PunctuationTestResult>(sql, new { StudentId = studentId, TestId = testId });
        }

        public async Task<bool> HasCompletedByStudentAndTestIdAsync(int studentId, int testId)
        {
            var sql = "SELECT COUNT(*) FROM PunctuationTestResults WHERE StudentId = @StudentId AND PunctuationTestId = @TestId AND IsCompleted = 1";
            var count = await _db.QueryScalarAsync<int>(sql, new { StudentId = studentId, TestId = testId });
            return count > 0;
        }

        public async Task<bool> IsInProgressByStudentAndTestIdAsync(int studentId, int testId)
        {
            var sql = "SELECT COUNT(*) FROM PunctuationTestResults WHERE StudentId = @StudentId AND PunctuationTestId = @TestId AND IsCompleted = 0";
            var count = await _db.QueryScalarAsync<int>(sql, new { StudentId = studentId, TestId = testId });
            return count > 0;
        }

        public async Task<PunctuationTestResult?> GetBestResultByStudentAndTestIdAsync(int studentId, int testId)
        {
            var sql = "SELECT TOP 1 * FROM PunctuationTestResults WHERE StudentId = @StudentId AND PunctuationTestId = @TestId AND IsCompleted = 1 ORDER BY Percentage DESC";
            return await _db.QueryFirstOrDefaultAsync<PunctuationTestResult>(sql, new { StudentId = studentId, TestId = testId });
        }

        public async Task<PunctuationTestResult?> GetLatestResultByStudentAndTestIdAsync(int studentId, int testId)
        {
            var sql = "SELECT TOP 1 * FROM PunctuationTestResults WHERE StudentId = @StudentId AND PunctuationTestId = @TestId AND IsCompleted = 1 ORDER BY CompletedAt DESC";
            return await _db.QueryFirstOrDefaultAsync<PunctuationTestResult>(sql, new { StudentId = studentId, TestId = testId });
        }

        public async Task<long?> GetTotalTimeSpentByStudentAndTestIdAsync(int studentId, int testId)
        {
            var sql = @"
                SELECT SUM(DATEDIFF(SECOND, StartedAt, CompletedAt))
                FROM PunctuationTestResults 
                WHERE StudentId = @StudentId AND PunctuationTestId = @TestId AND IsCompleted = 1 AND CompletedAt IS NOT NULL";
            return await _db.QueryScalarAsync<long?>(sql, new { StudentId = studentId, TestId = testId });
        }
    }
}

