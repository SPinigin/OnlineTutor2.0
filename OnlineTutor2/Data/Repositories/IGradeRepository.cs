using OnlineTutor2.Models;

namespace OnlineTutor2.Data.Repositories
{
    public interface IGradeRepository : IRepository<Grade>
    {
        Task<List<Grade>> GetByStudentIdAsync(int studentId);
        Task<List<Grade>> GetByAssignmentIdAsync(int assignmentId);
        Task<Grade?> GetByStudentAndAssignmentIdAsync(int studentId, int assignmentId);
    }
}





