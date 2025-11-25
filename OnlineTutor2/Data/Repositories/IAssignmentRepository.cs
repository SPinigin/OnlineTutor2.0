using OnlineTutor2.Models;

namespace OnlineTutor2.Data.Repositories
{
    public interface IAssignmentRepository : IRepository<Assignment>
    {
        Task<List<Assignment>> GetByClassIdAsync(int classId);
        Task<List<Assignment>> GetByTeacherIdAsync(string teacherId);
        Task<List<Assignment>> GetActiveByClassIdAsync(int classId);
    }
}





