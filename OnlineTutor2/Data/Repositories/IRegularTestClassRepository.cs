using OnlineTutor2.Models;

namespace OnlineTutor2.Data.Repositories
{
    public interface IRegularTestClassRepository : IRepository<RegularTestClass>
    {
        Task<List<RegularTestClass>> GetByTestIdAsync(int testId);
        Task<List<RegularTestClass>> GetByClassIdAsync(int classId);
        Task<int> DeleteByTestIdAsync(int testId);
        Task<int> DeleteByTestAndClassIdAsync(int testId, int classId);
        Task<List<int>> GetClassIdsByTestIdAsync(int testId);
    }
}

