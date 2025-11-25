using OnlineTutor2.Models;

namespace OnlineTutor2.Data.Repositories
{
    /// <summary>
    /// Реализация репозитория для работы с тестами по орфоэпии
    /// </summary>
    public class OrthoeopyTestRepository : BaseRepository<OrthoeopyTest>, IOrthoeopyTestRepository
    {
        public OrthoeopyTestRepository(IDatabaseConnection db) : base(db, "OrthoeopyTests")
        {
        }

        public async Task<List<OrthoeopyTest>> GetByTeacherIdAsync(string teacherId)
        {
            var sql = "SELECT * FROM OrthoeopyTests WHERE TeacherId = @TeacherId ORDER BY CreatedAt DESC";
            return await _db.QueryAsync<OrthoeopyTest>(sql, new { TeacherId = teacherId });
        }

        public async Task<List<OrthoeopyTest>> GetActiveByTeacherIdAsync(string teacherId)
        {
            var sql = "SELECT * FROM OrthoeopyTests WHERE TeacherId = @TeacherId AND IsActive = 1 ORDER BY CreatedAt DESC";
            return await _db.QueryAsync<OrthoeopyTest>(sql, new { TeacherId = teacherId });
        }

        public async Task<List<OrthoeopyTest>> GetRecentActiveByTeacherIdAsync(string teacherId, int count = 20)
        {
            var sql = $"SELECT TOP {count} * FROM OrthoeopyTests WHERE TeacherId = @TeacherId AND IsActive = 1 ORDER BY CreatedAt DESC";
            return await _db.QueryAsync<OrthoeopyTest>(sql, new { TeacherId = teacherId });
        }

        public async Task<int> GetCountByTeacherIdAsync(string teacherId)
        {
            var sql = "SELECT COUNT(*) FROM OrthoeopyTests WHERE TeacherId = @TeacherId";
            var result = await _db.QueryScalarAsync<int?>(sql, new { TeacherId = teacherId });
            return result ?? 0;
        }

        public async Task<OrthoeopyTest?> GetWithQuestionsAsync(int id)
        {
            var sql = "SELECT * FROM OrthoeopyTests WHERE Id = @Id";
            return await _db.QueryFirstOrDefaultAsync<OrthoeopyTest>(sql, new { Id = id });
        }

        public override async Task<int> CreateAsync(OrthoeopyTest entity)
        {
            var sql = @"
                INSERT INTO OrthoeopyTests (
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

        public override async Task<int> UpdateAsync(OrthoeopyTest entity)
        {
            var sql = @"
                UPDATE OrthoeopyTests 
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

