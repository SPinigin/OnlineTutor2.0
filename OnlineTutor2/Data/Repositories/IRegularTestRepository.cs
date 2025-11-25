using OnlineTutor2.Models;

namespace OnlineTutor2.Data.Repositories
{
    /// <summary>
    /// Репозиторий для работы с классическими тестами
    /// </summary>
    public interface IRegularTestRepository : IRepository<RegularTest>
    {
        Task<List<RegularTest>> GetByTeacherIdAsync(string teacherId);
        Task<List<RegularTest>> GetActiveByTeacherIdAsync(string teacherId);
        Task<List<RegularTest>> GetRecentActiveByTeacherIdAsync(string teacherId, int count = 20);
        Task<RegularTest?> GetWithQuestionsAsync(int id);
        Task<RegularTest?> GetWithResultsAsync(int id);
        Task<bool> IsActiveAsync(int id);
        Task<int> GetCountByTeacherIdAsync(string teacherId);
    }
}

