using System.Data;

namespace OnlineTutor2.Data
{
    /// <summary>
    /// Интерфейс для работы с подключением к базе данных
    /// </summary>
    public interface IDatabaseConnection : IDisposable
    {
        /// <summary>
        /// Получить открытое подключение к базе данных
        /// </summary>
        IDbConnection GetConnection();

        /// <summary>
        /// Выполнить команду и вернуть количество затронутых строк
        /// </summary>
        Task<int> ExecuteAsync(string sql, object? parameters = null, IDbTransaction? transaction = null);

        /// <summary>
        /// Выполнить запрос и вернуть одну запись
        /// </summary>
        Task<T?> QueryFirstOrDefaultAsync<T>(string sql, object? parameters = null, IDbTransaction? transaction = null) where T : class;

        /// <summary>
        /// Выполнить запрос и вернуть список записей
        /// </summary>
        Task<List<T>> QueryAsync<T>(string sql, object? parameters = null, IDbTransaction? transaction = null) where T : class;

        /// <summary>
        /// Выполнить запрос и вернуть скалярное значение
        /// </summary>
        Task<T?> QueryScalarAsync<T>(string sql, object? parameters = null, IDbTransaction? transaction = null);

        /// <summary>
        /// Начать транзакцию
        /// </summary>
        IDbTransaction BeginTransaction();

        /// <summary>
        /// Получить строку подключения
        /// </summary>
        string GetConnectionString();
    }
}



