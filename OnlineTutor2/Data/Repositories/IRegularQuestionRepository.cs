using OnlineTutor2.Models;

namespace OnlineTutor2.Data.Repositories
{
    /// <summary>
    /// Репозиторий для работы с вопросами классических тестов
    /// </summary>
    public interface IRegularQuestionRepository : IRepository<RegularQuestion>
    {
        Task<List<RegularQuestion>> GetByTestIdAsync(int testId);
        Task<List<RegularQuestion>> GetByTestIdOrderedAsync(int testId);
        Task<int> GetCountByTestIdAsync(int testId);
    }
}





