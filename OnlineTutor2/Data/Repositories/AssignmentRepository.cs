using OnlineTutor2.Models;

namespace OnlineTutor2.Data.Repositories
{
    public class AssignmentRepository : BaseRepository<Assignment>, IAssignmentRepository
    {
        public AssignmentRepository(IDatabaseConnection db) : base(db, "Assignments")
        {
        }

        public async Task<List<Assignment>> GetByClassIdAsync(int classId)
        {
            var sql = "SELECT * FROM Assignments WHERE ClassId = @ClassId ORDER BY DueDate DESC";
            return await _db.QueryAsync<Assignment>(sql, new { ClassId = classId });
        }

        public async Task<List<Assignment>> GetByTeacherIdAsync(string teacherId)
        {
            var sql = "SELECT * FROM Assignments WHERE TeacherId = @TeacherId ORDER BY CreatedAt DESC";
            return await _db.QueryAsync<Assignment>(sql, new { TeacherId = teacherId });
        }

        public async Task<List<Assignment>> GetActiveByClassIdAsync(int classId)
        {
            var sql = "SELECT * FROM Assignments WHERE ClassId = @ClassId AND IsActive = 1 ORDER BY DueDate DESC";
            return await _db.QueryAsync<Assignment>(sql, new { ClassId = classId });
        }

        public override async Task<int> CreateAsync(Assignment entity)
        {
            var sql = @"
                INSERT INTO Assignments (
                    Title, Description, ClassId, TeacherId, CreatedAt, DueDate, MaxPoints, IsActive
                )
                OUTPUT INSERTED.Id
                VALUES (
                    @Title, @Description, @ClassId, @TeacherId, @CreatedAt, @DueDate, @MaxPoints, @IsActive
                )";
            return await _db.QueryScalarAsync<int>(sql, entity);
        }

        public override async Task<int> UpdateAsync(Assignment entity)
        {
            var sql = @"
                UPDATE Assignments 
                SET Title = @Title, Description = @Description, DueDate = @DueDate, 
                    MaxPoints = @MaxPoints, IsActive = @IsActive
                WHERE Id = @Id";
            return await _db.ExecuteAsync(sql, entity);
        }
    }
}





