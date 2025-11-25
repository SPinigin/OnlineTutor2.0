using OnlineTutor2.Models;

namespace OnlineTutor2.Data.Repositories
{
    public interface IOrthoeopyQuestionRepository : IRepository<OrthoeopyQuestion>
    {
        Task<List<OrthoeopyQuestion>> GetByTestIdAsync(int testId);
        Task<List<OrthoeopyQuestion>> GetByTestIdOrderedAsync(int testId);
        Task<int> GetCountByTestIdAsync(int testId);
    }
}




