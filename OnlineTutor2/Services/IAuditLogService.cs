using OnlineTutor2.Models;

namespace OnlineTutor2.Services
{
    public interface IAuditLogService
    {
        Task LogActionAsync(
            string userId,
            string userName,
            string action,
            string entityType,
            string? entityId = null,
            string? details = null,
            string? ipAddress = null);

        Task<List<AuditLog>> GetLogsAsync(
            DateTime? fromDate = null,
            DateTime? toDate = null,
            string? userId = null,
            string? action = null,
            string? entityType = null,
            int page = 1,
            int pageSize = 50);

        Task<int> GetLogsCountAsync(
            DateTime? fromDate = null,
            DateTime? toDate = null,
            string? userId = null,
            string? action = null,
            string? entityType = null);

        Task<List<AuditLog>> GetUserLogsAsync(string userId, int count = 10);
        Task ClearOldLogsAsync(int daysToKeep = 90);
    }
}
