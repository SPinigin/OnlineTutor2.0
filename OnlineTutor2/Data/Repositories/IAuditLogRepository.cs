using OnlineTutor2.Models;

namespace OnlineTutor2.Data.Repositories
{
    public interface IAuditLogRepository : IRepository<AuditLog>
    {
        Task<List<AuditLog>> GetByUserIdAsync(string userId);
        Task<List<AuditLog>> GetByEntityTypeAsync(string entityType);
        Task<List<AuditLog>> GetRecentAsync(int count);
        Task<List<AuditLog>> GetFilteredAsync(
            DateTime? fromDate = null,
            DateTime? toDate = null,
            string? userId = null,
            string? action = null,
            string? entityType = null,
            int page = 1,
            int pageSize = 50);
        Task<int> GetFilteredCountAsync(
            DateTime? fromDate = null,
            DateTime? toDate = null,
            string? userId = null,
            string? action = null,
            string? entityType = null);
        Task<List<AuditLog>> GetUserLogsAsync(string userId, int count = 10);
        Task<int> DeleteOldLogsAsync(int daysToKeep = 90);
    }
}

