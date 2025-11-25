using OnlineTutor2.Models;

namespace OnlineTutor2.Data.Repositories
{
    public interface IOrthoeopyTestClassRepository : IRepository<OrthoeopyTestClass>
    {
        Task<List<OrthoeopyTestClass>> GetByTestIdAsync(int testId);
        Task<List<OrthoeopyTestClass>> GetByClassIdAsync(int classId);
        Task<int> DeleteByTestIdAsync(int testId);
        Task<List<int>> GetClassIdsByTestIdAsync(int testId);
    }
}

