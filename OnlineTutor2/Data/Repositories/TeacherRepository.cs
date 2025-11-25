using OnlineTutor2.Models;

namespace OnlineTutor2.Data.Repositories
{
    /// <summary>
    /// Реализация репозитория для работы с учителями
    /// </summary>
    public class TeacherRepository : BaseRepository<Teacher>, ITeacherRepository
    {
        public TeacherRepository(IDatabaseConnection db) : base(db, "Teachers")
        {
        }

        public async Task<Teacher?> GetByUserIdAsync(string userId)
        {
            var sql = "SELECT * FROM Teachers WHERE UserId = @UserId";
            return await _db.QueryFirstOrDefaultAsync<Teacher>(sql, new { UserId = userId });
        }

        public async Task<Teacher?> GetWithUserAsync(int id)
        {
            var sql = @"
                SELECT t.* 
                FROM Teachers t
                WHERE t.Id = @Id";
            return await _db.QueryFirstOrDefaultAsync<Teacher>(sql, new { Id = id });
        }

        public async Task<List<Teacher>> GetAllWithUserAsync()
        {
            var sql = "SELECT * FROM Teachers ORDER BY CreatedAt DESC";
            return await _db.QueryAsync<Teacher>(sql);
        }

        public override async Task<int> CreateAsync(Teacher entity)
        {
            var sql = @"
                INSERT INTO Teachers (UserId, Subject, Education, Experience, CreatedAt, IsApproved)
                OUTPUT INSERTED.Id
                VALUES (@UserId, @Subject, @Education, @Experience, @CreatedAt, @IsApproved)";
            
            var id = await _db.QueryScalarAsync<int>(sql, new
            {
                entity.UserId,
                entity.Subject,
                entity.Education,
                entity.Experience,
                entity.CreatedAt,
                entity.IsApproved
            });
            return id;
        }

        public override async Task<int> UpdateAsync(Teacher entity)
        {
            var sql = @"
                UPDATE Teachers 
                SET Subject = @Subject, 
                    Education = @Education, 
                    Experience = @Experience, 
                    IsApproved = @IsApproved
                WHERE Id = @Id";

            return await _db.ExecuteAsync(sql, new
            {
                entity.Id,
                entity.Subject,
                entity.Education,
                entity.Experience,
                entity.IsApproved
            });
        }
    }
}





