using OnlineTutor2.Models;

namespace OnlineTutor2.Data.Repositories
{
    /// <summary>
    /// Репозиторий для работы с тестами по пунктуации
    /// </summary>
    public interface IPunctuationTestRepository : IRepository<PunctuationTest>
    {
        Task<List<PunctuationTest>> GetByTeacherIdAsync(string teacherId);
        Task<List<PunctuationTest>> GetActiveByTeacherIdAsync(string teacherId);
        Task<List<PunctuationTest>> GetRecentActiveByTeacherIdAsync(string teacherId, int count = 20);
        Task<PunctuationTest?> GetWithQuestionsAsync(int id);
        Task<int> GetCountByTeacherIdAsync(string teacherId);
    }
}

