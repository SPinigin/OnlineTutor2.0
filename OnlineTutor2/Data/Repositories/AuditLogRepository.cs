using System.Dynamic;
using OnlineTutor2.Models;

namespace OnlineTutor2.Data.Repositories
{
    public class AuditLogRepository : BaseRepository<AuditLog>, IAuditLogRepository
    {
        public AuditLogRepository(IDatabaseConnection db) : base(db, "AuditLogs")
        {
        }

        public async Task<List<AuditLog>> GetByUserIdAsync(string userId)
        {
            var sql = "SELECT * FROM AuditLogs WHERE UserId = @UserId ORDER BY CreatedAt DESC";
            return await _db.QueryAsync<AuditLog>(sql, new { UserId = userId });
        }

        public async Task<List<AuditLog>> GetByEntityTypeAsync(string entityType)
        {
            var sql = "SELECT * FROM AuditLogs WHERE EntityType = @EntityType ORDER BY CreatedAt DESC";
            return await _db.QueryAsync<AuditLog>(sql, new { EntityType = entityType });
        }

        public async Task<List<AuditLog>> GetRecentAsync(int count)
        {
            var sql = $"SELECT TOP {count} * FROM AuditLogs ORDER BY CreatedAt DESC";
            return await _db.QueryAsync<AuditLog>(sql);
        }

        public async Task<List<AuditLog>> GetFilteredAsync(
            DateTime? fromDate = null,
            DateTime? toDate = null,
            string? userId = null,
            string? action = null,
            string? entityType = null,
            int page = 1,
            int pageSize = 50)
        {
            var conditions = new List<string>();
            var parameters = new ExpandoObject() as IDictionary<string, object>;

            if (fromDate.HasValue)
            {
                conditions.Add("CreatedAt >= @FromDate");
                parameters["FromDate"] = fromDate.Value;
            }

            if (toDate.HasValue)
            {
                var endOfDay = toDate.Value.Date.AddDays(1).AddTicks(-1);
                conditions.Add("CreatedAt <= @ToDate");
                parameters["ToDate"] = endOfDay;
            }

            if (!string.IsNullOrEmpty(userId))
            {
                conditions.Add("UserId = @UserId");
                parameters["UserId"] = userId;
            }

            if (!string.IsNullOrEmpty(action))
            {
                conditions.Add("Action = @Action");
                parameters["Action"] = action;
            }

            if (!string.IsNullOrEmpty(entityType))
            {
                conditions.Add("EntityType = @EntityType");
                parameters["EntityType"] = entityType;
            }

            var whereClause = conditions.Any() ? "WHERE " + string.Join(" AND ", conditions) : "";
            var offset = (page - 1) * pageSize;

            var sql = $@"
                SELECT * FROM AuditLogs
                {whereClause}
                ORDER BY CreatedAt DESC
                OFFSET {offset} ROWS
                FETCH NEXT {pageSize} ROWS ONLY";

            return await _db.QueryAsync<AuditLog>(sql, parameters);
        }

        public async Task<int> GetFilteredCountAsync(
            DateTime? fromDate = null,
            DateTime? toDate = null,
            string? userId = null,
            string? action = null,
            string? entityType = null)
        {
            var conditions = new List<string>();
            var parameters = new ExpandoObject() as IDictionary<string, object>;

            if (fromDate.HasValue)
            {
                conditions.Add("CreatedAt >= @FromDate");
                parameters["FromDate"] = fromDate.Value;
            }

            if (toDate.HasValue)
            {
                var endOfDay = toDate.Value.Date.AddDays(1).AddTicks(-1);
                conditions.Add("CreatedAt <= @ToDate");
                parameters["ToDate"] = endOfDay;
            }

            if (!string.IsNullOrEmpty(userId))
            {
                conditions.Add("UserId = @UserId");
                parameters["UserId"] = userId;
            }

            if (!string.IsNullOrEmpty(action))
            {
                conditions.Add("Action = @Action");
                parameters["Action"] = action;
            }

            if (!string.IsNullOrEmpty(entityType))
            {
                conditions.Add("EntityType = @EntityType");
                parameters["EntityType"] = entityType;
            }

            var whereClause = conditions.Any() ? "WHERE " + string.Join(" AND ", conditions) : "";

            var sql = $"SELECT COUNT(*) FROM AuditLogs {whereClause}";
            var result = await _db.QueryScalarAsync<int?>(sql, parameters);
            return result ?? 0;
        }

        public async Task<List<AuditLog>> GetUserLogsAsync(string userId, int count = 10)
        {
            var sql = $"SELECT TOP {count} * FROM AuditLogs WHERE UserId = @UserId ORDER BY CreatedAt DESC";
            return await _db.QueryAsync<AuditLog>(sql, new { UserId = userId });
        }

        public async Task<int> DeleteOldLogsAsync(int daysToKeep = 90)
        {
            var cutoffDate = DateTime.Now.AddDays(-daysToKeep);
            var sql = "DELETE FROM AuditLogs WHERE CreatedAt < @CutoffDate";
            return await _db.ExecuteAsync(sql, new { CutoffDate = cutoffDate });
        }

        public override async Task<int> CreateAsync(AuditLog entity)
        {
            var sql = @"
                INSERT INTO AuditLogs (
                    UserId, UserName, Action, EntityType, EntityId, Details, IpAddress, CreatedAt
                )
                OUTPUT INSERTED.Id
                VALUES (
                    @UserId, @UserName, @Action, @EntityType, @EntityId, @Details, @IpAddress, @CreatedAt
                )";
            return await _db.QueryScalarAsync<int>(sql, entity);
        }
    }
}

