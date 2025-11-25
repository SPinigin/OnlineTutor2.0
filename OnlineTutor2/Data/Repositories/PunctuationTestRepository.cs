using OnlineTutor2.Models;

namespace OnlineTutor2.Data.Repositories
{
    /// <summary>
    /// Реализация репозитория для работы с тестами по пунктуации
    /// </summary>
    public class PunctuationTestRepository : BaseRepository<PunctuationTest>, IPunctuationTestRepository
    {
        public PunctuationTestRepository(IDatabaseConnection db) : base(db, "PunctuationTests")
        {
        }

        public async Task<List<PunctuationTest>> GetByTeacherIdAsync(string teacherId)
        {
            var sql = "SELECT * FROM PunctuationTests WHERE TeacherId = @TeacherId ORDER BY CreatedAt DESC";
            return await _db.QueryAsync<PunctuationTest>(sql, new { TeacherId = teacherId });
        }

        public async Task<List<PunctuationTest>> GetActiveByTeacherIdAsync(string teacherId)
        {
            var sql = "SELECT * FROM PunctuationTests WHERE TeacherId = @TeacherId AND IsActive = 1 ORDER BY CreatedAt DESC";
            return await _db.QueryAsync<PunctuationTest>(sql, new { TeacherId = teacherId });
        }

        public async Task<List<PunctuationTest>> GetRecentActiveByTeacherIdAsync(string teacherId, int count = 20)
        {
            var sql = $"SELECT TOP {count} * FROM PunctuationTests WHERE TeacherId = @TeacherId AND IsActive = 1 ORDER BY CreatedAt DESC";
            return await _db.QueryAsync<PunctuationTest>(sql, new { TeacherId = teacherId });
        }

        public async Task<int> GetCountByTeacherIdAsync(string teacherId)
        {
            var sql = "SELECT COUNT(*) FROM PunctuationTests WHERE TeacherId = @TeacherId";
            var result = await _db.QueryScalarAsync<int?>(sql, new { TeacherId = teacherId });
            return result ?? 0;
        }

        public async Task<PunctuationTest?> GetWithQuestionsAsync(int id)
        {
            var sql = "SELECT * FROM PunctuationTests WHERE Id = @Id";
            return await _db.QueryFirstOrDefaultAsync<PunctuationTest>(sql, new { Id = id });
        }

        public override async Task<int> CreateAsync(PunctuationTest entity)
        {
            var sql = @"
                INSERT INTO PunctuationTests (
                    Title, Description, TeacherId, TestCategoryId, TimeLimit, MaxAttempts,
                    StartDate, EndDate, ShowHints, ShowCorrectAnswers, IsActive, CreatedAt
                )
                OUTPUT INSERTED.Id
                VALUES (
                    @Title, @Description, @TeacherId, @TestCategoryId, @TimeLimit, @MaxAttempts,
                    @StartDate, @EndDate, @ShowHints, @ShowCorrectAnswers, @IsActive, @CreatedAt
                )";
            
            var id = await _db.QueryScalarAsync<int>(sql, new
            {
                entity.Title,
                entity.Description,
                entity.TeacherId,
                entity.TestCategoryId,
                entity.TimeLimit,
                entity.MaxAttempts,
                entity.StartDate,
                entity.EndDate,
                entity.ShowHints,
                entity.ShowCorrectAnswers,
                entity.IsActive,
                entity.CreatedAt
            });
            return id;
        }

        public override async Task<int> UpdateAsync(PunctuationTest entity)
        {
            var sql = @"
                UPDATE PunctuationTests 
                SET Title = @Title,
                    Description = @Description,
                    TestCategoryId = @TestCategoryId,
                    TimeLimit = @TimeLimit,
                    MaxAttempts = @MaxAttempts,
                    StartDate = @StartDate,
                    EndDate = @EndDate,
                    ShowHints = @ShowHints,
                    ShowCorrectAnswers = @ShowCorrectAnswers,
                    IsActive = @IsActive
                WHERE Id = @Id";

            return await _db.ExecuteAsync(sql, new
            {
                entity.Id,
                entity.Title,
                entity.Description,
                entity.TestCategoryId,
                entity.TimeLimit,
                entity.MaxAttempts,
                entity.StartDate,
                entity.EndDate,
                entity.ShowHints,
                entity.ShowCorrectAnswers,
                entity.IsActive
            });
        }
    }
}

