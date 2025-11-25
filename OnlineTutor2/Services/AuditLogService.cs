using OnlineTutor2.Data.Repositories;
using OnlineTutor2.Models;

namespace OnlineTutor2.Services
{
    public class AuditLogService : IAuditLogService
    {
        private readonly IAuditLogRepository _auditLogRepository;
        private readonly ILogger<AuditLogService> _logger;

        public AuditLogService(
            IAuditLogRepository auditLogRepository,
            ILogger<AuditLogService> logger)
        {
            _auditLogRepository = auditLogRepository;
            _logger = logger;
        }

        public async Task LogActionAsync(
            string userId,
            string userName,
            string action,
            string entityType,
            string? entityId = null,
            string? details = null,
            string? ipAddress = null)
        {
            try
            {
                var auditLog = new AuditLog
                {
                    UserId = userId,
                    UserName = userName,
                    Action = action,
                    EntityType = entityType,
                    EntityId = entityId,
                    Details = details,
                    IpAddress = ipAddress,
                    CreatedAt = DateTime.Now
                };

                await _auditLogRepository.CreateAsync(auditLog);

                _logger.LogInformation(
                    "Audit log created: {Action} on {EntityType} by {UserName}",
                    action, entityType, userName);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to create audit log");
            }
        }

        public async Task<List<AuditLog>> GetLogsAsync(
            DateTime? fromDate = null,
            DateTime? toDate = null,
            string? userId = null,
            string? action = null,
            string? entityType = null,
            int page = 1,
            int pageSize = 50)
        {
            return await _auditLogRepository.GetFilteredAsync(
                fromDate, toDate, userId, action, entityType, page, pageSize);
        }

        public async Task<int> GetLogsCountAsync(
            DateTime? fromDate = null,
            DateTime? toDate = null,
            string? userId = null,
            string? action = null,
            string? entityType = null)
        {
            return await _auditLogRepository.GetFilteredCountAsync(
                fromDate, toDate, userId, action, entityType);
        }

        public async Task<List<AuditLog>> GetUserLogsAsync(string userId, int count = 10)
        {
            return await _auditLogRepository.GetUserLogsAsync(userId, count);
        }

        public async Task ClearOldLogsAsync(int daysToKeep = 90)
        {
            var deletedCount = await _auditLogRepository.DeleteOldLogsAsync(daysToKeep);

            if (deletedCount > 0)
            {
                _logger.LogInformation(
                    "Cleared {Count} audit logs older than {Days} days",
                    deletedCount, daysToKeep);
            }
        }
    }
}
