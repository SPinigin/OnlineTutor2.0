namespace OnlineTutor2.Data.Repositories
{
    /// <summary>
    /// Базовый интерфейс репозитория
    /// </summary>
    public interface IRepository<T> where T : class
    {
        Task<T?> GetByIdAsync(int id);
        Task<List<T>> GetAllAsync();
        Task<int> CreateAsync(T entity);
        Task<int> UpdateAsync(T entity);
        Task<int> DeleteAsync(int id);
        Task<bool> ExistsAsync(int id);
    }
}





