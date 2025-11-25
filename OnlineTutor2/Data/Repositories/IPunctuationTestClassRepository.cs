using OnlineTutor2.Models;

namespace OnlineTutor2.Data.Repositories
{
    public interface IPunctuationTestClassRepository : IRepository<PunctuationTestClass>
    {
        Task<List<PunctuationTestClass>> GetByTestIdAsync(int testId);
        Task<List<PunctuationTestClass>> GetByClassIdAsync(int classId);
        Task<int> DeleteByTestIdAsync(int testId);
        Task<List<int>> GetClassIdsByTestIdAsync(int testId);
    }
}

