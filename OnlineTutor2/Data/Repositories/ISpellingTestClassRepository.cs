using OnlineTutor2.Models;

namespace OnlineTutor2.Data.Repositories
{
    public interface ISpellingTestClassRepository : IRepository<SpellingTestClass>
    {
        Task<List<SpellingTestClass>> GetByTestIdAsync(int testId);
        Task<List<SpellingTestClass>> GetByClassIdAsync(int classId);
        Task<int> DeleteByTestIdAsync(int testId);
        Task<List<int>> GetClassIdsByTestIdAsync(int testId);
    }
}

