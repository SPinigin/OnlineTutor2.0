using OnlineTutor2.Models;

namespace OnlineTutor2.Data.Repositories
{
    public class TestAssignmentRepository : BaseRepository<TestAssignment>, ITestAssignmentRepository
    {
        public TestAssignmentRepository(IDatabaseConnection db) : base(db, "TestAssignments")
        {
        }

        public async Task<List<TestAssignment>> GetByTeacherIdAsync(string teacherId)
        {
            var sql = "SELECT * FROM TestAssignments WHERE TeacherId = @TeacherId ORDER BY AssignmentNumber ASC";
            return await _db.QueryAsync<TestAssignment>(sql, new { TeacherId = teacherId });
        }

        public async Task<List<TestAssignment>> GetByTestCategoryIdAsync(int testCategoryId)
        {
            var sql = "SELECT * FROM TestAssignments WHERE TestCategoryId = @TestCategoryId ORDER BY AssignmentNumber ASC";
            return await _db.QueryAsync<TestAssignment>(sql, new { TestCategoryId = testCategoryId });
        }

        public async Task<List<TestAssignment>> GetByTeacherIdAndCategoryAsync(string teacherId, int testCategoryId)
        {
            var sql = @"SELECT * FROM TestAssignments 
                       WHERE TeacherId = @TeacherId AND TestCategoryId = @TestCategoryId 
                       ORDER BY AssignmentNumber ASC";
            return await _db.QueryAsync<TestAssignment>(sql, new { TeacherId = teacherId, TestCategoryId = testCategoryId });
        }

        public async Task<TestAssignment?> GetByAssignmentNumberAsync(int assignmentNumber, int testCategoryId, string teacherId)
        {
            var sql = @"SELECT * FROM TestAssignments 
                       WHERE AssignmentNumber = @AssignmentNumber 
                       AND TestCategoryId = @TestCategoryId 
                       AND TeacherId = @TeacherId";
            return await _db.QueryFirstOrDefaultAsync<TestAssignment>(sql, new 
            { 
                AssignmentNumber = assignmentNumber, 
                TestCategoryId = testCategoryId, 
                TeacherId = teacherId 
            });
        }

        public override async Task<int> CreateAsync(TestAssignment entity)
        {
            var sql = @"
                INSERT INTO TestAssignments (
                    Title, Description, AssignmentNumber, TestCategoryId, TeacherId, IsActive, CreatedAt
                )
                OUTPUT INSERTED.Id
                VALUES (
                    @Title, @Description, @AssignmentNumber, @TestCategoryId, @TeacherId, @IsActive, @CreatedAt
                )";
            return await _db.QueryScalarAsync<int>(sql, new
            {
                entity.Title,
                entity.Description,
                entity.AssignmentNumber,
                entity.TestCategoryId,
                entity.TeacherId,
                entity.IsActive,
                entity.CreatedAt
            });
        }

        public override async Task<int> UpdateAsync(TestAssignment entity)
        {
            var sql = @"
                UPDATE TestAssignments 
                SET Title = @Title, Description = @Description, AssignmentNumber = @AssignmentNumber, 
                    IsActive = @IsActive
                WHERE Id = @Id";
            return await _db.ExecuteAsync(sql, entity);
        }
    }
}

