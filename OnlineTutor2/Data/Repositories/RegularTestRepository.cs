using OnlineTutor2.Models;

namespace OnlineTutor2.Data.Repositories
{
    /// <summary>
    /// Реализация репозитория для работы с классическими тестами
    /// </summary>
    public class RegularTestRepository : BaseRepository<RegularTest>, IRegularTestRepository
    {
        public RegularTestRepository(IDatabaseConnection db) : base(db, "RegularTests")
        {
        }

        public async Task<List<RegularTest>> GetByTeacherIdAsync(string teacherId)
        {
            var sql = "SELECT * FROM RegularTests WHERE TeacherId = @TeacherId ORDER BY CreatedAt DESC";
            return await _db.QueryAsync<RegularTest>(sql, new { TeacherId = teacherId });
        }

        public async Task<List<RegularTest>> GetActiveByTeacherIdAsync(string teacherId)
        {
            var sql = "SELECT * FROM RegularTests WHERE TeacherId = @TeacherId AND IsActive = 1 ORDER BY CreatedAt DESC";
            return await _db.QueryAsync<RegularTest>(sql, new { TeacherId = teacherId });
        }

        public async Task<List<RegularTest>> GetRecentActiveByTeacherIdAsync(string teacherId, int count = 20)
        {
            var sql = $"SELECT TOP {count} * FROM RegularTests WHERE TeacherId = @TeacherId AND IsActive = 1 ORDER BY CreatedAt DESC";
            return await _db.QueryAsync<RegularTest>(sql, new { TeacherId = teacherId });
        }

        public async Task<int> GetCountByTeacherIdAsync(string teacherId)
        {
            var sql = "SELECT COUNT(*) FROM RegularTests WHERE TeacherId = @TeacherId";
            var result = await _db.QueryScalarAsync<int?>(sql, new { TeacherId = teacherId });
            return result ?? 0;
        }

        public async Task<RegularTest?> GetWithQuestionsAsync(int id)
        {
            var sql = @"
                SELECT t.* 
                FROM RegularTests t
                WHERE t.Id = @Id";
            return await _db.QueryFirstOrDefaultAsync<RegularTest>(sql, new { Id = id });
        }

        public async Task<RegularTest?> GetWithResultsAsync(int id)
        {
            var sql = @"
                SELECT t.* 
                FROM RegularTests t
                WHERE t.Id = @Id";
            return await _db.QueryFirstOrDefaultAsync<RegularTest>(sql, new { Id = id });
        }

        public async Task<bool> IsActiveAsync(int id)
        {
            var sql = "SELECT IsActive FROM RegularTests WHERE Id = @Id";
            var result = await _db.QueryScalarAsync<bool?>(sql, new { Id = id });
            return result ?? false;
        }

        public override async Task<int> CreateAsync(RegularTest entity)
        {
            var sql = @"
                INSERT INTO RegularTests (
                    Title, Description, TeacherId, TestCategoryId, TimeLimit, MaxAttempts,
                    StartDate, EndDate, ShowHints, ShowCorrectAnswers, IsActive, CreatedAt, Type
                )
                OUTPUT INSERTED.Id
                VALUES (
                    @Title, @Description, @TeacherId, @TestCategoryId, @TimeLimit, @MaxAttempts,
                    @StartDate, @EndDate, @ShowHints, @ShowCorrectAnswers, @IsActive, @CreatedAt, @Type
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
                entity.CreatedAt,
                Type = (int)entity.Type
            });
            return id;
        }

        public override async Task<int> UpdateAsync(RegularTest entity)
        {
            var sql = @"
                UPDATE RegularTests 
                SET Title = @Title,
                    Description = @Description,
                    TestCategoryId = @TestCategoryId,
                    TimeLimit = @TimeLimit,
                    MaxAttempts = @MaxAttempts,
                    StartDate = @StartDate,
                    EndDate = @EndDate,
                    ShowHints = @ShowHints,
                    ShowCorrectAnswers = @ShowCorrectAnswers,
                    IsActive = @IsActive,
                    Type = @Type
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
                entity.IsActive,
                Type = (int)entity.Type
            });
        }
    }
}

