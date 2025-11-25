using OnlineTutor2.Data.Repositories;

namespace OnlineTutor2.Data.Repositories
{
    /// <summary>
    /// Базовый репозиторий с общими методами
    /// </summary>
    public abstract class BaseRepository<T> : IRepository<T> where T : class
    {
        protected readonly IDatabaseConnection _db;
        protected readonly string _tableName;
        protected readonly string _idColumn;

        protected BaseRepository(IDatabaseConnection db, string tableName, string idColumn = "Id")
        {
            _db = db;
            _tableName = tableName;
            _idColumn = idColumn;
        }

        public virtual async Task<T?> GetByIdAsync(int id)
        {
            var sql = $"SELECT * FROM {_tableName} WHERE {_idColumn} = @Id";
            return await _db.QueryFirstOrDefaultAsync<T>(sql, new { Id = id });
        }

        public virtual async Task<List<T>> GetAllAsync()
        {
            var sql = $"SELECT * FROM {_tableName}";
            return await _db.QueryAsync<T>(sql);
        }

        public virtual async Task<int> CreateAsync(T entity)
        {
            var properties = typeof(T).GetProperties()
                .Where(p => p.Name != _idColumn && p.CanWrite)
                .ToList();

            var columns = string.Join(", ", properties.Select(p => p.Name));
            var values = string.Join(", ", properties.Select(p => $"@{p.Name}"));

            var sql = $"INSERT INTO {_tableName} ({columns}) OUTPUT INSERTED.{_idColumn} VALUES ({values})";
            
            var id = await _db.QueryScalarAsync<int>(sql, entity);
            return id;
        }

        public virtual async Task<int> UpdateAsync(T entity)
        {
            var properties = typeof(T).GetProperties()
                .Where(p => p.Name != _idColumn && p.CanWrite)
                .ToList();

            var setClause = string.Join(", ", properties.Select(p => $"{p.Name} = @{p.Name}"));
            var sql = $"UPDATE {_tableName} SET {setClause} WHERE {_idColumn} = @{_idColumn}";

            return await _db.ExecuteAsync(sql, entity);
        }

        public virtual async Task<int> DeleteAsync(int id)
        {
            var sql = $"DELETE FROM {_tableName} WHERE {_idColumn} = @Id";
            return await _db.ExecuteAsync(sql, new { Id = id });
        }

        public virtual async Task<bool> ExistsAsync(int id)
        {
            var sql = $"SELECT COUNT(1) FROM {_tableName} WHERE {_idColumn} = @Id";
            var count = await _db.QueryScalarAsync<int>(sql, new { Id = id });
            return count > 0;
        }
    }
}





