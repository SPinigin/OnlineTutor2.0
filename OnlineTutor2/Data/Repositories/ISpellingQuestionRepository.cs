using OnlineTutor2.Models;

namespace OnlineTutor2.Data.Repositories
{
    public interface ISpellingQuestionRepository : IRepository<SpellingQuestion>
    {
        Task<List<SpellingQuestion>> GetByTestIdAsync(int testId);
        Task<List<SpellingQuestion>> GetByTestIdOrderedAsync(int testId);
        Task<int> GetCountByTestIdAsync(int testId);
    }
}




