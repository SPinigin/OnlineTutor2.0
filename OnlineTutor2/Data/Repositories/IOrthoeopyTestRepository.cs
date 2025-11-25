using OnlineTutor2.Models;

namespace OnlineTutor2.Data.Repositories
{
    /// <summary>
    /// Репозиторий для работы с тестами по орфоэпии
    /// </summary>
    public interface IOrthoeopyTestRepository : IRepository<OrthoeopyTest>
    {
        Task<List<OrthoeopyTest>> GetByTeacherIdAsync(string teacherId);
        Task<List<OrthoeopyTest>> GetActiveByTeacherIdAsync(string teacherId);
        Task<List<OrthoeopyTest>> GetRecentActiveByTeacherIdAsync(string teacherId, int count = 20);
        Task<OrthoeopyTest?> GetWithQuestionsAsync(int id);
        Task<int> GetCountByTeacherIdAsync(string teacherId);
    }
}

