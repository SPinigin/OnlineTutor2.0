using OnlineTutor2.Models;

namespace OnlineTutor2.Data.Repositories
{
    /// <summary>
    /// Репозиторий для работы с вариантами ответов на вопросы классических тестов
    /// </summary>
    public interface IRegularQuestionOptionRepository : IRepository<RegularQuestionOption>
    {
        Task<List<RegularQuestionOption>> GetByQuestionIdAsync(int questionId);
        Task<List<RegularQuestionOption>> GetByQuestionIdOrderedAsync(int questionId);
        Task<int> DeleteByQuestionIdAsync(int questionId);
    }
}





