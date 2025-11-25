using OnlineTutor2.Models;

namespace OnlineTutor2.Data.Repositories
{
    public class GradeRepository : BaseRepository<Grade>, IGradeRepository
    {
        public GradeRepository(IDatabaseConnection db) : base(db, "Grades")
        {
        }

        public async Task<List<Grade>> GetByStudentIdAsync(int studentId)
        {
            var sql = "SELECT * FROM Grades WHERE StudentId = @StudentId ORDER BY GradedAt DESC";
            return await _db.QueryAsync<Grade>(sql, new { StudentId = studentId });
        }

        public async Task<List<Grade>> GetByAssignmentIdAsync(int assignmentId)
        {
            var sql = "SELECT * FROM Grades WHERE AssignmentId = @AssignmentId ORDER BY GradedAt DESC";
            return await _db.QueryAsync<Grade>(sql, new { AssignmentId = assignmentId });
        }

        public async Task<Grade?> GetByStudentAndAssignmentIdAsync(int studentId, int assignmentId)
        {
            var sql = "SELECT * FROM Grades WHERE StudentId = @StudentId AND AssignmentId = @AssignmentId";
            return await _db.QueryFirstOrDefaultAsync<Grade>(sql, new { StudentId = studentId, AssignmentId = assignmentId });
        }

        public override async Task<int> CreateAsync(Grade entity)
        {
            var sql = @"
                INSERT INTO Grades (
                    StudentId, AssignmentId, TestId, Points, MaxPoints, Percentage, 
                    Comments, GradedAt, TeacherId
                )
                OUTPUT INSERTED.Id
                VALUES (
                    @StudentId, @AssignmentId, @TestId, @Points, @MaxPoints, @Percentage,
                    @Comments, @GradedAt, @TeacherId
                )";
            return await _db.QueryScalarAsync<int>(sql, entity);
        }

        public override async Task<int> UpdateAsync(Grade entity)
        {
            var sql = @"
                UPDATE Grades 
                SET Points = @Points, MaxPoints = @MaxPoints, Percentage = @Percentage,
                    Comments = @Comments
                WHERE Id = @Id";
            return await _db.ExecuteAsync(sql, entity);
        }
    }
}





