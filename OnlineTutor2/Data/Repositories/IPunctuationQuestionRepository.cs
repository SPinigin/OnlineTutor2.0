using OnlineTutor2.Models;

namespace OnlineTutor2.Data.Repositories
{
    public interface IPunctuationQuestionRepository : IRepository<PunctuationQuestion>
    {
        Task<List<PunctuationQuestion>> GetByTestIdAsync(int testId);
        Task<List<PunctuationQuestion>> GetByTestIdOrderedAsync(int testId);
        Task<int> GetCountByTestIdAsync(int testId);
    }
}




