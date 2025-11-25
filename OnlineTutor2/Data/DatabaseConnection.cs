using System.Data;
using Microsoft.Data.SqlClient;
using Logger = NLog.Logger;

namespace OnlineTutor2.Data
{
    /// <summary>
    /// Реализация подключения к базе данных через ADO.NET
    /// </summary>
    public class DatabaseConnection : IDatabaseConnection
    {
        private readonly string _connectionString;
        private readonly Logger _logger;
        private IDbConnection? _connection;
        private IDbTransaction? _currentTransaction;

        public DatabaseConnection(IConfiguration configuration, Logger logger)
        {
            _connectionString = configuration.GetConnectionString("DefaultConnection") 
                ?? throw new InvalidOperationException("Connection string 'DefaultConnection' not found.");
            _logger = logger;
        }

        public IDbConnection GetConnection()
        {
            if (_connection == null || _connection.State != ConnectionState.Open)
            {
                _connection = new SqlConnection(_connectionString);
                _connection.Open();
            }
            return _connection;
        }

        public async Task<int> ExecuteAsync(string sql, object? parameters = null, IDbTransaction? transaction = null)
        {
            try
            {
                var connection = transaction?.Connection ?? GetConnection();
                using var command = connection.CreateCommand();
                command.CommandText = sql;
                command.Transaction = transaction ?? _currentTransaction;

                if (parameters != null)
                {
                    AddParameters(command, parameters);
                }

                if (command is SqlCommand sqlCommand)
                {
                    return await sqlCommand.ExecuteNonQueryAsync();
                }
                else
                {
                    return command.ExecuteNonQuery();
                }
            }
            catch (Exception ex)
            {
                _logger.Error(ex, "Ошибка выполнения команды SQL: {Sql}", sql);
                throw;
            }
        }

        public async Task<T?> QueryFirstOrDefaultAsync<T>(string sql, object? parameters = null, IDbTransaction? transaction = null) where T : class
        {
            try
            {
                var connection = transaction?.Connection ?? GetConnection();
                using var command = connection.CreateCommand();
                command.CommandText = sql;
                command.Transaction = transaction ?? _currentTransaction;

                if (parameters != null)
                {
                    AddParameters(command, parameters);
                }

                if (command is SqlCommand sqlCommand)
                {
                    using var reader = await sqlCommand.ExecuteReaderAsync();
                    if (await reader.ReadAsync())
                    {
                        return MapToObject<T>(reader);
                    }
                }
                else
                {
                    using var reader = command.ExecuteReader();
                    if (reader.Read())
                    {
                        return MapToObject<T>(reader);
                    }
                }
                return null;
            }
            catch (Exception ex)
            {
                _logger.Error(ex, "Ошибка выполнения запроса SQL: {Sql}", sql);
                throw;
            }
        }

        public async Task<List<T>> QueryAsync<T>(string sql, object? parameters = null, IDbTransaction? transaction = null) where T : class
        {
            try
            {
                var connection = transaction?.Connection ?? GetConnection();
                using var command = connection.CreateCommand();
                command.CommandText = sql;
                command.Transaction = transaction ?? _currentTransaction;

                if (parameters != null)
                {
                    AddParameters(command, parameters);
                }

                var results = new List<T>();
                
                if (command is SqlCommand sqlCommand)
                {
                    using var reader = await sqlCommand.ExecuteReaderAsync();
                    while (await reader.ReadAsync())
                    {
                        var item = MapToObject<T>(reader);
                        if (item != null)
                        {
                            results.Add(item);
                        }
                    }
                }
                else
                {
                    using var reader = command.ExecuteReader();
                    while (reader.Read())
                    {
                        var item = MapToObject<T>(reader);
                        if (item != null)
                        {
                            results.Add(item);
                        }
                    }
                }
                return results;
            }
            catch (Exception ex)
            {
                _logger.Error(ex, "Ошибка выполнения запроса SQL: {Sql}", sql);
                throw;
            }
        }

        public async Task<T?> QueryScalarAsync<T>(string sql, object? parameters = null, IDbTransaction? transaction = null)
        {
            try
            {
                var connection = transaction?.Connection ?? GetConnection();
                using var command = connection.CreateCommand();
                command.CommandText = sql;
                command.Transaction = transaction ?? _currentTransaction;

                if (parameters != null)
                {
                    AddParameters(command, parameters);
                }

                object? result = null;

                if (command is SqlCommand sqlCommand)
                {
                    // Для OUTPUT INSERTED.Id нужно использовать ExecuteReader
                    if (sql.Contains("OUTPUT INSERTED", StringComparison.OrdinalIgnoreCase))
                    {
                        using var reader = await sqlCommand.ExecuteReaderAsync();
                        if (await reader.ReadAsync())
                        {
                            result = reader[0];
                        }
                    }
                    else
                    {
                        result = await sqlCommand.ExecuteScalarAsync();
                    }
                }
                else
                {
                    result = command.ExecuteScalar();
                }

                if (result == null || result == DBNull.Value)
                {
                    return default(T);
                }

                return (T)Convert.ChangeType(result, typeof(T));
            }
            catch (Exception ex)
            {
                _logger.Error(ex, "Ошибка выполнения скалярного запроса SQL: {Sql}", sql);
                throw;
            }
        }

        public IDbTransaction BeginTransaction()
        {
            if (_currentTransaction != null)
            {
                throw new InvalidOperationException("Transaction already started");
            }

            _currentTransaction = GetConnection().BeginTransaction();
            return _currentTransaction;
        }

        public string GetConnectionString()
        {
            return _connectionString;
        }

        private void AddParameters(IDbCommand command, object parameters)
        {
            if (command is not SqlCommand sqlCommand)
            {
                throw new InvalidOperationException("Only SqlCommand is supported");
            }

            // Поддержка IDictionary<string, object> (например, ExpandoObject)
            if (parameters is IDictionary<string, object> dict)
            {
                foreach (var kvp in dict)
                {
                    var parameter = sqlCommand.Parameters.AddWithValue($"@{kvp.Key}", kvp.Value ?? DBNull.Value);
                    parameter.ParameterName = $"@{kvp.Key}";
                }
                return;
            }

            // Поддержка обычных объектов через рефлексию
            var properties = parameters.GetType().GetProperties();
            foreach (var property in properties)
            {
                var value = property.GetValue(parameters);
                var parameter = sqlCommand.Parameters.AddWithValue($"@{property.Name}", value ?? DBNull.Value);
                parameter.ParameterName = $"@{property.Name}";
            }
        }

        private T? MapToObject<T>(IDataReader reader) where T : class
        {
            var obj = Activator.CreateInstance<T>();
            var properties = typeof(T).GetProperties();

            for (int i = 0; i < reader.FieldCount; i++)
            {
                var columnName = reader.GetName(i);
                var property = properties.FirstOrDefault(p => 
                    string.Equals(p.Name, columnName, StringComparison.OrdinalIgnoreCase));

                if (property != null && property.CanWrite)
                {
                    var value = reader[i];
                    if (value != DBNull.Value)
                    {
                        try
                        {
                            if (property.PropertyType.IsEnum && value is int intValue)
                            {
                                property.SetValue(obj, Enum.ToObject(property.PropertyType, intValue));
                            }
                            else if (property.PropertyType == typeof(DateTime) && value is DateTime dateValue)
                            {
                                property.SetValue(obj, dateValue);
                            }
                            else if (property.PropertyType == typeof(DateTime?) && value is DateTime dateValueNullable)
                            {
                                property.SetValue(obj, dateValueNullable);
                            }
                            else
                            {
                                property.SetValue(obj, Convert.ChangeType(value, property.PropertyType));
                            }
                        }
                        catch (Exception ex)
                        {
                            _logger.Warn(ex, "Не удалось установить значение для свойства {PropertyName} из колонки {ColumnName}", 
                                property.Name, columnName);
                        }
                    }
                }
            }

            return obj;
        }

        public void Dispose()
        {
            _currentTransaction?.Dispose();
            _connection?.Dispose();
        }
    }
}
