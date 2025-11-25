using OnlineTutor2.Models;

namespace OnlineTutor2.Data.Repositories
{
    /// <summary>
    /// Репозиторий для работы с учителями
    /// </summary>
    public interface ITeacherRepository : IRepository<Teacher>
    {
        Task<Teacher?> GetByUserIdAsync(string userId);
        Task<Teacher?> GetWithUserAsync(int id);
        Task<List<Teacher>> GetAllWithUserAsync();
    }
}





