using OnlineTutor2.Models;

namespace OnlineTutor2.Data.Repositories
{
    public interface ITestAssignmentRepository : IRepository<TestAssignment>
    {
        Task<List<TestAssignment>> GetByTeacherIdAsync(string teacherId);
        Task<List<TestAssignment>> GetByTestCategoryIdAsync(int testCategoryId);
        Task<List<TestAssignment>> GetByTeacherIdAndCategoryAsync(string teacherId, int testCategoryId);
        Task<TestAssignment?> GetByAssignmentNumberAsync(int assignmentNumber, int testCategoryId, string teacherId);
    }
}




