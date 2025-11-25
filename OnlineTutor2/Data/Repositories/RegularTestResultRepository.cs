using OnlineTutor2.Data;
using OnlineTutor2.Models;

namespace OnlineTutor2.Data.Repositories
{
    public class RegularTestResultRepository : BaseRepository<RegularTestResult>, IRegularTestResultRepository
    {
        public RegularTestResultRepository(IDatabaseConnection db) : base(db, "RegularTestResults")
        {
        }

        public async Task<List<RegularTestResult>> GetByTestIdAsync(int testId)
        {
            var sql = "SELECT * FROM RegularTestResults WHERE RegularTestId = @TestId ORDER BY StartedAt DESC";
            return await _db.QueryAsync<RegularTestResult>(sql, new { TestId = testId });
        }

        public async Task<List<RegularTestResult>> GetByStudentIdAsync(int studentId)
        {
            var sql = "SELECT * FROM RegularTestResults WHERE StudentId = @StudentId ORDER BY StartedAt DESC";
            return await _db.QueryAsync<RegularTestResult>(sql, new { StudentId = studentId });
        }

        public async Task<List<RegularTestResult>> GetCompletedByStudentIdAsync(int studentId)
        {
            var sql = "SELECT * FROM RegularTestResults WHERE StudentId = @StudentId AND CompletedAt IS NOT NULL ORDER BY StartedAt DESC";
            return await _db.QueryAsync<RegularTestResult>(sql, new { StudentId = studentId });
        }

        public async Task<int> GetTotalCompletedCountByStudentIdAsync(int studentId)
        {
            var sql = "SELECT COUNT(*) FROM RegularTestResults WHERE StudentId = @StudentId AND CompletedAt IS NOT NULL";
            var result = await _db.QueryScalarAsync<int?>(sql, new { StudentId = studentId });
            return result ?? 0;
        }

        public async Task<int> GetTotalScoreByStudentIdAsync(int studentId)
        {
            var sql = "SELECT ISNULL(SUM(Score), 0) FROM RegularTestResults WHERE StudentId = @StudentId AND CompletedAt IS NOT NULL";
            var result = await _db.QueryScalarAsync<int?>(sql, new { StudentId = studentId });
            return result ?? 0;
        }

        public async Task<double> GetAveragePercentageByStudentIdAsync(int studentId)
        {
            var sql = "SELECT AVG(CAST(Percentage AS FLOAT)) FROM RegularTestResults WHERE StudentId = @StudentId AND CompletedAt IS NOT NULL";
            var result = await _db.QueryScalarAsync<double?>(sql, new { StudentId = studentId });
            return result ?? 0;
        }

        public async Task<RegularTestResult?> GetByTestAndStudentIdAsync(int testId, int studentId)
        {
            var sql = "SELECT * FROM RegularTestResults WHERE RegularTestId = @TestId AND StudentId = @StudentId ORDER BY AttemptNumber DESC";
            return await _db.QueryFirstOrDefaultAsync<RegularTestResult>(sql, new { TestId = testId, StudentId = studentId });
        }

        public async Task<int> GetCountByTestIdAsync(int testId)
        {
            var sql = "SELECT COUNT(*) FROM RegularTestResults WHERE RegularTestId = @TestId";
            var result = await _db.QueryScalarAsync<int?>(sql, new { TestId = testId });
            return result ?? 0;
        }

        public async Task<List<RegularTestResult>> GetByTeacherIdWithDetailsAsync(string teacherId)
        {
            var sql = @"
                SELECT TOP 50
                    rtr.*,
                    rt.Id as Test_Id, rt.Title as Test_Title, rt.TeacherId as Test_TeacherId,
                    s.Id as Student_Id, s.UserId as Student_UserId,
                    u.Id as User_Id, u.FirstName as User_FirstName, u.LastName as User_LastName
                FROM RegularTestResults rtr
                INNER JOIN RegularTests rt ON rtr.RegularTestId = rt.Id
                INNER JOIN Students s ON rtr.StudentId = s.Id
                INNER JOIN AspNetUsers u ON s.UserId = u.Id
                WHERE rt.TeacherId = @TeacherId
                ORDER BY COALESCE(rtr.CompletedAt, rtr.StartedAt) DESC";
            
            return await _db.QueryAsync<RegularTestResult>(sql, new { TeacherId = teacherId });
        }

        public async Task<RegularTestResult?> GetByIdWithDetailsAsync(int id, string teacherId)
        {
            var sql = @"
                SELECT 
                    rtr.*,
                    rt.Id as Test_Id, rt.Title as Test_Title, rt.TeacherId as Test_TeacherId,
                    s.Id as Student_Id, s.UserId as Student_UserId,
                    u.Id as User_Id, u.FirstName as User_FirstName, u.LastName as User_LastName
                FROM RegularTestResults rtr
                INNER JOIN RegularTests rt ON rtr.RegularTestId = rt.Id
                INNER JOIN Students s ON rtr.StudentId = s.Id
                INNER JOIN AspNetUsers u ON s.UserId = u.Id
                WHERE rtr.Id = @Id AND rt.TeacherId = @TeacherId";
            
            return await _db.QueryFirstOrDefaultAsync<RegularTestResult>(sql, new { Id = id, TeacherId = teacherId });
        }

        public override async Task<int> CreateAsync(RegularTestResult entity)
        {
            var sql = @"
                INSERT INTO RegularTestResults (
                    RegularTestId, StudentId, StartedAt, CompletedAt, Score, MaxScore, 
                    Percentage, IsCompleted, AttemptNumber
                )
                OUTPUT INSERTED.Id
                VALUES (
                    @RegularTestId, @StudentId, @StartedAt, @CompletedAt, @Score, @MaxScore,
                    @Percentage, @IsCompleted, @AttemptNumber
                )";
            
            var id = await _db.QueryScalarAsync<int>(sql, new
            {
                entity.RegularTestId,
                entity.StudentId,
                entity.StartedAt,
                entity.CompletedAt,
                entity.Score,
                entity.MaxScore,
                entity.Percentage,
                entity.IsCompleted,
                entity.AttemptNumber
            });
            return id;
        }

        public override async Task<int> UpdateAsync(RegularTestResult entity)
        {
            var sql = @"
                UPDATE RegularTestResults 
                SET CompletedAt = @CompletedAt,
                    Score = @Score,
                    MaxScore = @MaxScore,
                    Percentage = @Percentage,
                    IsCompleted = @IsCompleted
                WHERE Id = @Id";

            return await _db.ExecuteAsync(sql, new
            {
                entity.Id,
                entity.CompletedAt,
                entity.Score,
                entity.MaxScore,
                entity.Percentage,
                entity.IsCompleted
            });
        }

        // Analytics methods
        public async Task<List<int>> GetDistinctStudentIdsByTestIdAsync(int testId)
        {
            var sql = "SELECT DISTINCT StudentId AS Id FROM RegularTestResults WHERE RegularTestId = @TestId";
            var results = await _db.QueryAsync<IntId>(sql, new { TestId = testId });
            return results.Select(r => r.Id).ToList();
        }

        public async Task<int> GetDistinctStudentsCountByTestIdAsync(int testId)
        {
            var sql = "SELECT COUNT(DISTINCT StudentId) FROM RegularTestResults WHERE RegularTestId = @TestId";
            var result = await _db.QueryScalarAsync<int?>(sql, new { TestId = testId });
            return result ?? 0;
        }

        public async Task<int> GetCompletedStudentsCountByTestIdAsync(int testId)
        {
            var sql = "SELECT COUNT(DISTINCT StudentId) FROM RegularTestResults WHERE RegularTestId = @TestId AND IsCompleted = 1";
            var result = await _db.QueryScalarAsync<int?>(sql, new { TestId = testId });
            return result ?? 0;
        }

        public async Task<int> GetInProgressStudentsCountByTestIdAsync(int testId)
        {
            var sql = "SELECT COUNT(DISTINCT StudentId) FROM RegularTestResults WHERE RegularTestId = @TestId AND IsCompleted = 0";
            var result = await _db.QueryScalarAsync<int?>(sql, new { TestId = testId });
            return result ?? 0;
        }

        public async Task<List<RegularTestResult>> GetCompletedByTestIdAsync(int testId)
        {
            var sql = "SELECT * FROM RegularTestResults WHERE RegularTestId = @TestId AND IsCompleted = 1";
            return await _db.QueryAsync<RegularTestResult>(sql, new { TestId = testId });
        }

        public async Task<List<RegularTestResult>> GetInProgressByTestIdAsync(int testId)
        {
            var sql = "SELECT * FROM RegularTestResults WHERE RegularTestId = @TestId AND IsCompleted = 0";
            return await _db.QueryAsync<RegularTestResult>(sql, new { TestId = testId });
        }

        public async Task<double> GetAverageScoreByTestIdAsync(int testId)
        {
            var sql = "SELECT AVG(CAST(Score AS FLOAT)) FROM RegularTestResults WHERE RegularTestId = @TestId AND IsCompleted = 1";
            var result = await _db.QueryScalarAsync<double?>(sql, new { TestId = testId });
            return result ?? 0;
        }

        public async Task<double> GetAveragePercentageByTestIdAsync(int testId)
        {
            var sql = "SELECT AVG(CAST(Percentage AS FLOAT)) FROM RegularTestResults WHERE RegularTestId = @TestId AND IsCompleted = 1";
            var result = await _db.QueryScalarAsync<double?>(sql, new { TestId = testId });
            return result ?? 0;
        }

        public async Task<int> GetHighestScoreByTestIdAsync(int testId)
        {
            var sql = "SELECT MAX(Score) FROM RegularTestResults WHERE RegularTestId = @TestId AND IsCompleted = 1";
            var result = await _db.QueryScalarAsync<int?>(sql, new { TestId = testId });
            return result ?? 0;
        }

        public async Task<int> GetLowestScoreByTestIdAsync(int testId)
        {
            var sql = "SELECT MIN(Score) FROM RegularTestResults WHERE RegularTestId = @TestId AND IsCompleted = 1";
            var result = await _db.QueryScalarAsync<int?>(sql, new { TestId = testId });
            return result ?? 0;
        }

        public async Task<DateTime?> GetFirstCompletionByTestIdAsync(int testId)
        {
            var sql = "SELECT MIN(CompletedAt) FROM RegularTestResults WHERE RegularTestId = @TestId AND IsCompleted = 1";
            return await _db.QueryScalarAsync<DateTime?>(sql, new { TestId = testId });
        }

        public async Task<DateTime?> GetLastCompletionByTestIdAsync(int testId)
        {
            var sql = "SELECT MAX(CompletedAt) FROM RegularTestResults WHERE RegularTestId = @TestId AND IsCompleted = 1";
            return await _db.QueryScalarAsync<DateTime?>(sql, new { TestId = testId });
        }

        public async Task<double?> GetAverageCompletionTimeByTestIdAsync(int testId)
        {
            var sql = @"
                SELECT AVG(CAST(DATEDIFF(SECOND, StartedAt, CompletedAt) AS FLOAT))
                FROM RegularTestResults 
                WHERE RegularTestId = @TestId AND IsCompleted = 1 AND CompletedAt IS NOT NULL";
            return await _db.QueryScalarAsync<double?>(sql, new { TestId = testId });
        }

        public async Task<Dictionary<string, int>> GetGradeDistributionByTestIdAsync(int testId)
        {
            var excellentSql = "SELECT COUNT(*) FROM RegularTestResults WHERE RegularTestId = @TestId AND IsCompleted = 1 AND Percentage >= 80";
            var goodSql = "SELECT COUNT(*) FROM RegularTestResults WHERE RegularTestId = @TestId AND IsCompleted = 1 AND Percentage >= 60 AND Percentage < 80";
            var satisfactorySql = "SELECT COUNT(*) FROM RegularTestResults WHERE RegularTestId = @TestId AND IsCompleted = 1 AND Percentage >= 40 AND Percentage < 60";
            var unsatisfactorySql = "SELECT COUNT(*) FROM RegularTestResults WHERE RegularTestId = @TestId AND IsCompleted = 1 AND Percentage < 40";

            return new Dictionary<string, int>
            {
                ["Отлично (80-100%)"] = await _db.QueryScalarAsync<int>(excellentSql, new { TestId = testId }),
                ["Хорошо (60-79%)"] = await _db.QueryScalarAsync<int>(goodSql, new { TestId = testId }),
                ["Удовлетворительно (40-59%)"] = await _db.QueryScalarAsync<int>(satisfactorySql, new { TestId = testId }),
                ["Неудовлетворительно (0-39%)"] = await _db.QueryScalarAsync<int>(unsatisfactorySql, new { TestId = testId })
            };
        }

        public async Task<List<RegularTestResult>> GetByStudentAndTestIdAsync(int studentId, int testId)
        {
            var sql = "SELECT * FROM RegularTestResults WHERE StudentId = @StudentId AND RegularTestId = @TestId";
            return await _db.QueryAsync<RegularTestResult>(sql, new { StudentId = studentId, TestId = testId });
        }

        public async Task<List<RegularTestResult>> GetCompletedByStudentAndTestIdAsync(int studentId, int testId)
        {
            var sql = "SELECT * FROM RegularTestResults WHERE StudentId = @StudentId AND RegularTestId = @TestId AND IsCompleted = 1";
            return await _db.QueryAsync<RegularTestResult>(sql, new { StudentId = studentId, TestId = testId });
        }

        public async Task<bool> HasCompletedByStudentAndTestIdAsync(int studentId, int testId)
        {
            var sql = "SELECT COUNT(*) FROM RegularTestResults WHERE StudentId = @StudentId AND RegularTestId = @TestId AND IsCompleted = 1";
            var count = await _db.QueryScalarAsync<int>(sql, new { StudentId = studentId, TestId = testId });
            return count > 0;
        }

        public async Task<bool> IsInProgressByStudentAndTestIdAsync(int studentId, int testId)
        {
            var sql = "SELECT COUNT(*) FROM RegularTestResults WHERE StudentId = @StudentId AND RegularTestId = @TestId AND IsCompleted = 0";
            var count = await _db.QueryScalarAsync<int>(sql, new { StudentId = studentId, TestId = testId });
            return count > 0;
        }

        public async Task<RegularTestResult?> GetBestResultByStudentAndTestIdAsync(int studentId, int testId)
        {
            var sql = "SELECT TOP 1 * FROM RegularTestResults WHERE StudentId = @StudentId AND RegularTestId = @TestId AND IsCompleted = 1 ORDER BY Percentage DESC";
            return await _db.QueryFirstOrDefaultAsync<RegularTestResult>(sql, new { StudentId = studentId, TestId = testId });
        }

        public async Task<RegularTestResult?> GetLatestResultByStudentAndTestIdAsync(int studentId, int testId)
        {
            var sql = "SELECT TOP 1 * FROM RegularTestResults WHERE StudentId = @StudentId AND RegularTestId = @TestId AND IsCompleted = 1 ORDER BY CompletedAt DESC";
            return await _db.QueryFirstOrDefaultAsync<RegularTestResult>(sql, new { StudentId = studentId, TestId = testId });
        }

        public async Task<long?> GetTotalTimeSpentByStudentAndTestIdAsync(int studentId, int testId)
        {
            var sql = @"
                SELECT SUM(DATEDIFF(SECOND, StartedAt, CompletedAt))
                FROM RegularTestResults 
                WHERE StudentId = @StudentId AND RegularTestId = @TestId AND IsCompleted = 1 AND CompletedAt IS NOT NULL";
            return await _db.QueryScalarAsync<long?>(sql, new { StudentId = studentId, TestId = testId });
        }
    }
}

