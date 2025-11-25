using OnlineTutor2.Models;

namespace OnlineTutor2.Data.Repositories
{
    /// <summary>
    /// Репозиторий для работы с тестами по орфографии
    /// </summary>
    public interface ISpellingTestRepository : IRepository<SpellingTest>
    {
        Task<List<SpellingTest>> GetByTeacherIdAsync(string teacherId);
        Task<List<SpellingTest>> GetActiveByTeacherIdAsync(string teacherId);
        Task<List<SpellingTest>> GetRecentActiveByTeacherIdAsync(string teacherId, int count = 20);
        Task<SpellingTest?> GetWithQuestionsAsync(int id);
        Task<int> GetCountByTeacherIdAsync(string teacherId);
    }
}

