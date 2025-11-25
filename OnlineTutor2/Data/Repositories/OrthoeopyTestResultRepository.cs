using OnlineTutor2.Data;
using OnlineTutor2.Models;

namespace OnlineTutor2.Data.Repositories
{
    public class OrthoeopyTestResultRepository : BaseRepository<OrthoeopyTestResult>, IOrthoeopyTestResultRepository
    {
        public OrthoeopyTestResultRepository(IDatabaseConnection db) : base(db, "OrthoeopyTestResults")
        {
        }

        public async Task<List<OrthoeopyTestResult>> GetByTestIdAsync(int testId)
        {
            var sql = "SELECT * FROM OrthoeopyTestResults WHERE OrthoeopyTestId = @TestId ORDER BY StartedAt DESC";
            return await _db.QueryAsync<OrthoeopyTestResult>(sql, new { TestId = testId });
        }

        public async Task<List<OrthoeopyTestResult>> GetByStudentIdAsync(int studentId)
        {
            var sql = "SELECT * FROM OrthoeopyTestResults WHERE StudentId = @StudentId ORDER BY StartedAt DESC";
            return await _db.QueryAsync<OrthoeopyTestResult>(sql, new { StudentId = studentId });
        }

        public async Task<List<OrthoeopyTestResult>> GetCompletedByStudentIdAsync(int studentId)
        {
            var sql = "SELECT * FROM OrthoeopyTestResults WHERE StudentId = @StudentId AND CompletedAt IS NOT NULL ORDER BY StartedAt DESC";
            return await _db.QueryAsync<OrthoeopyTestResult>(sql, new { StudentId = studentId });
        }

        public async Task<int> GetTotalCompletedCountByStudentIdAsync(int studentId)
        {
            var sql = "SELECT COUNT(*) FROM OrthoeopyTestResults WHERE StudentId = @StudentId AND CompletedAt IS NOT NULL";
            var result = await _db.QueryScalarAsync<int?>(sql, new { StudentId = studentId });
            return result ?? 0;
        }

        public async Task<int> GetTotalScoreByStudentIdAsync(int studentId)
        {
            var sql = "SELECT ISNULL(SUM(Score), 0) FROM OrthoeopyTestResults WHERE StudentId = @StudentId AND CompletedAt IS NOT NULL";
            var result = await _db.QueryScalarAsync<int?>(sql, new { StudentId = studentId });
            return result ?? 0;
        }

        public async Task<double> GetAveragePercentageByStudentIdAsync(int studentId)
        {
            var sql = "SELECT AVG(CAST(Percentage AS FLOAT)) FROM OrthoeopyTestResults WHERE StudentId = @StudentId AND CompletedAt IS NOT NULL";
            var result = await _db.QueryScalarAsync<double?>(sql, new { StudentId = studentId });
            return result ?? 0;
        }

        public async Task<int> GetCountByTestIdAsync(int testId)
        {
            var sql = "SELECT COUNT(*) FROM OrthoeopyTestResults WHERE OrthoeopyTestId = @TestId";
            var result = await _db.QueryScalarAsync<int?>(sql, new { TestId = testId });
            return result ?? 0;
        }

        public async Task<List<OrthoeopyTestResult>> GetByTeacherIdWithDetailsAsync(string teacherId)
        {
            var sql = @"
                SELECT TOP 50
                    otr.*,
                    ot.Id as Test_Id, ot.Title as Test_Title, ot.TeacherId as Test_TeacherId,
                    s.Id as Student_Id, s.UserId as Student_UserId,
                    u.Id as User_Id, u.FirstName as User_FirstName, u.LastName as User_LastName
                FROM OrthoeopyTestResults otr
                INNER JOIN OrthoeopyTests ot ON otr.OrthoeopyTestId = ot.Id
                INNER JOIN Students s ON otr.StudentId = s.Id
                INNER JOIN AspNetUsers u ON s.UserId = u.Id
                WHERE ot.TeacherId = @TeacherId
                ORDER BY COALESCE(otr.CompletedAt, otr.StartedAt) DESC";
            
            return await _db.QueryAsync<OrthoeopyTestResult>(sql, new { TeacherId = teacherId });
        }

        public async Task<OrthoeopyTestResult?> GetByIdWithDetailsAsync(int id, string teacherId)
        {
            var sql = @"
                SELECT 
                    otr.*,
                    ot.Id as Test_Id, ot.Title as Test_Title, ot.TeacherId as Test_TeacherId,
                    s.Id as Student_Id, s.UserId as Student_UserId,
                    u.Id as User_Id, u.FirstName as User_FirstName, u.LastName as User_LastName
                FROM OrthoeopyTestResults otr
                INNER JOIN OrthoeopyTests ot ON otr.OrthoeopyTestId = ot.Id
                INNER JOIN Students s ON otr.StudentId = s.Id
                INNER JOIN AspNetUsers u ON s.UserId = u.Id
                WHERE otr.Id = @Id AND ot.TeacherId = @TeacherId";
            
            return await _db.QueryFirstOrDefaultAsync<OrthoeopyTestResult>(sql, new { Id = id, TeacherId = teacherId });
        }

        public override async Task<int> CreateAsync(OrthoeopyTestResult entity)
        {
            var sql = @"
                INSERT INTO OrthoeopyTestResults (
                    OrthoeopyTestId, StudentId, StartedAt, CompletedAt, Score, MaxScore,
                    Percentage, IsCompleted, AttemptNumber
                )
                OUTPUT INSERTED.Id
                VALUES (
                    @OrthoeopyTestId, @StudentId, @StartedAt, @CompletedAt, @Score, @MaxScore,
                    @Percentage, @IsCompleted, @AttemptNumber
                )";
            return await _db.QueryScalarAsync<int>(sql, entity);
        }

        public override async Task<int> UpdateAsync(OrthoeopyTestResult entity)
        {
            var sql = @"
                UPDATE OrthoeopyTestResults 
                SET CompletedAt = @CompletedAt, Score = @Score, MaxScore = @MaxScore,
                    Percentage = @Percentage, IsCompleted = @IsCompleted
                WHERE Id = @Id";
            return await _db.ExecuteAsync(sql, entity);
        }

        // Analytics methods - аналогично PunctuationTestResultRepository
        public async Task<List<int>> GetDistinctStudentIdsByTestIdAsync(int testId)
        {
            var sql = "SELECT DISTINCT StudentId AS Id FROM OrthoeopyTestResults WHERE OrthoeopyTestId = @TestId";
            var results = await _db.QueryAsync<IntId>(sql, new { TestId = testId });
            return results.Select(r => r.Id).ToList();
        }

        public async Task<int> GetDistinctStudentsCountByTestIdAsync(int testId)
        {
            var sql = "SELECT COUNT(DISTINCT StudentId) FROM OrthoeopyTestResults WHERE OrthoeopyTestId = @TestId";
            var result = await _db.QueryScalarAsync<int?>(sql, new { TestId = testId });
            return result ?? 0;
        }

        public async Task<int> GetCompletedStudentsCountByTestIdAsync(int testId)
        {
            var sql = "SELECT COUNT(DISTINCT StudentId) FROM OrthoeopyTestResults WHERE OrthoeopyTestId = @TestId AND IsCompleted = 1";
            var result = await _db.QueryScalarAsync<int?>(sql, new { TestId = testId });
            return result ?? 0;
        }

        public async Task<int> GetInProgressStudentsCountByTestIdAsync(int testId)
        {
            var sql = "SELECT COUNT(DISTINCT StudentId) FROM OrthoeopyTestResults WHERE OrthoeopyTestId = @TestId AND IsCompleted = 0";
            var result = await _db.QueryScalarAsync<int?>(sql, new { TestId = testId });
            return result ?? 0;
        }

        public async Task<List<OrthoeopyTestResult>> GetCompletedByTestIdAsync(int testId)
        {
            var sql = "SELECT * FROM OrthoeopyTestResults WHERE OrthoeopyTestId = @TestId AND IsCompleted = 1";
            return await _db.QueryAsync<OrthoeopyTestResult>(sql, new { TestId = testId });
        }

        public async Task<List<OrthoeopyTestResult>> GetInProgressByTestIdAsync(int testId)
        {
            var sql = "SELECT * FROM OrthoeopyTestResults WHERE OrthoeopyTestId = @TestId AND IsCompleted = 0";
            return await _db.QueryAsync<OrthoeopyTestResult>(sql, new { TestId = testId });
        }

        public async Task<double> GetAverageScoreByTestIdAsync(int testId)
        {
            var sql = "SELECT AVG(CAST(Score AS FLOAT)) FROM OrthoeopyTestResults WHERE OrthoeopyTestId = @TestId AND IsCompleted = 1";
            var result = await _db.QueryScalarAsync<double?>(sql, new { TestId = testId });
            return result ?? 0;
        }

        public async Task<double> GetAveragePercentageByTestIdAsync(int testId)
        {
            var sql = "SELECT AVG(CAST(Percentage AS FLOAT)) FROM OrthoeopyTestResults WHERE OrthoeopyTestId = @TestId AND IsCompleted = 1";
            var result = await _db.QueryScalarAsync<double?>(sql, new { TestId = testId });
            return result ?? 0;
        }

        public async Task<int> GetHighestScoreByTestIdAsync(int testId)
        {
            var sql = "SELECT MAX(Score) FROM OrthoeopyTestResults WHERE OrthoeopyTestId = @TestId AND IsCompleted = 1";
            var result = await _db.QueryScalarAsync<int?>(sql, new { TestId = testId });
            return result ?? 0;
        }

        public async Task<int> GetLowestScoreByTestIdAsync(int testId)
        {
            var sql = "SELECT MIN(Score) FROM OrthoeopyTestResults WHERE OrthoeopyTestId = @TestId AND IsCompleted = 1";
            var result = await _db.QueryScalarAsync<int?>(sql, new { TestId = testId });
            return result ?? 0;
        }

        public async Task<DateTime?> GetFirstCompletionByTestIdAsync(int testId)
        {
            var sql = "SELECT MIN(CompletedAt) FROM OrthoeopyTestResults WHERE OrthoeopyTestId = @TestId AND IsCompleted = 1";
            return await _db.QueryScalarAsync<DateTime?>(sql, new { TestId = testId });
        }

        public async Task<DateTime?> GetLastCompletionByTestIdAsync(int testId)
        {
            var sql = "SELECT MAX(CompletedAt) FROM OrthoeopyTestResults WHERE OrthoeopyTestId = @TestId AND IsCompleted = 1";
            return await _db.QueryScalarAsync<DateTime?>(sql, new { TestId = testId });
        }

        public async Task<double?> GetAverageCompletionTimeByTestIdAsync(int testId)
        {
            var sql = @"
                SELECT AVG(CAST(DATEDIFF(SECOND, StartedAt, CompletedAt) AS FLOAT))
                FROM OrthoeopyTestResults 
                WHERE OrthoeopyTestId = @TestId AND IsCompleted = 1 AND CompletedAt IS NOT NULL";
            return await _db.QueryScalarAsync<double?>(sql, new { TestId = testId });
        }

        public async Task<Dictionary<string, int>> GetGradeDistributionByTestIdAsync(int testId)
        {
            var excellentSql = "SELECT COUNT(*) FROM OrthoeopyTestResults WHERE OrthoeopyTestId = @TestId AND IsCompleted = 1 AND Percentage >= 80";
            var goodSql = "SELECT COUNT(*) FROM OrthoeopyTestResults WHERE OrthoeopyTestId = @TestId AND IsCompleted = 1 AND Percentage >= 60 AND Percentage < 80";
            var satisfactorySql = "SELECT COUNT(*) FROM OrthoeopyTestResults WHERE OrthoeopyTestId = @TestId AND IsCompleted = 1 AND Percentage >= 40 AND Percentage < 60";
            var unsatisfactorySql = "SELECT COUNT(*) FROM OrthoeopyTestResults WHERE OrthoeopyTestId = @TestId AND IsCompleted = 1 AND Percentage < 40";

            return new Dictionary<string, int>
            {
                ["Отлично (80-100%)"] = await _db.QueryScalarAsync<int>(excellentSql, new { TestId = testId }),
                ["Хорошо (60-79%)"] = await _db.QueryScalarAsync<int>(goodSql, new { TestId = testId }),
                ["Удовлетворительно (40-59%)"] = await _db.QueryScalarAsync<int>(satisfactorySql, new { TestId = testId }),
                ["Неудовлетворительно (0-39%)"] = await _db.QueryScalarAsync<int>(unsatisfactorySql, new { TestId = testId })
            };
        }

        public async Task<List<OrthoeopyTestResult>> GetByStudentAndTestIdAsync(int studentId, int testId)
        {
            var sql = "SELECT * FROM OrthoeopyTestResults WHERE StudentId = @StudentId AND OrthoeopyTestId = @TestId";
            return await _db.QueryAsync<OrthoeopyTestResult>(sql, new { StudentId = studentId, TestId = testId });
        }

        public async Task<List<OrthoeopyTestResult>> GetCompletedByStudentAndTestIdAsync(int studentId, int testId)
        {
            var sql = "SELECT * FROM OrthoeopyTestResults WHERE StudentId = @StudentId AND OrthoeopyTestId = @TestId AND IsCompleted = 1";
            return await _db.QueryAsync<OrthoeopyTestResult>(sql, new { StudentId = studentId, TestId = testId });
        }

        public async Task<bool> HasCompletedByStudentAndTestIdAsync(int studentId, int testId)
        {
            var sql = "SELECT COUNT(*) FROM OrthoeopyTestResults WHERE StudentId = @StudentId AND OrthoeopyTestId = @TestId AND IsCompleted = 1";
            var count = await _db.QueryScalarAsync<int>(sql, new { StudentId = studentId, TestId = testId });
            return count > 0;
        }

        public async Task<bool> IsInProgressByStudentAndTestIdAsync(int studentId, int testId)
        {
            var sql = "SELECT COUNT(*) FROM OrthoeopyTestResults WHERE StudentId = @StudentId AND OrthoeopyTestId = @TestId AND IsCompleted = 0";
            var count = await _db.QueryScalarAsync<int>(sql, new { StudentId = studentId, TestId = testId });
            return count > 0;
        }

        public async Task<OrthoeopyTestResult?> GetBestResultByStudentAndTestIdAsync(int studentId, int testId)
        {
            var sql = "SELECT TOP 1 * FROM OrthoeopyTestResults WHERE StudentId = @StudentId AND OrthoeopyTestId = @TestId AND IsCompleted = 1 ORDER BY Percentage DESC";
            return await _db.QueryFirstOrDefaultAsync<OrthoeopyTestResult>(sql, new { StudentId = studentId, TestId = testId });
        }

        public async Task<OrthoeopyTestResult?> GetLatestResultByStudentAndTestIdAsync(int studentId, int testId)
        {
            var sql = "SELECT TOP 1 * FROM OrthoeopyTestResults WHERE StudentId = @StudentId AND OrthoeopyTestId = @TestId AND IsCompleted = 1 ORDER BY CompletedAt DESC";
            return await _db.QueryFirstOrDefaultAsync<OrthoeopyTestResult>(sql, new { StudentId = studentId, TestId = testId });
        }

        public async Task<long?> GetTotalTimeSpentByStudentAndTestIdAsync(int studentId, int testId)
        {
            var sql = @"
                SELECT SUM(DATEDIFF(SECOND, StartedAt, CompletedAt))
                FROM OrthoeopyTestResults 
                WHERE StudentId = @StudentId AND OrthoeopyTestId = @TestId AND IsCompleted = 1 AND CompletedAt IS NOT NULL";
            return await _db.QueryScalarAsync<long?>(sql, new { StudentId = studentId, TestId = testId });
        }
    }
}

