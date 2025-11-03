using Microsoft.EntityFrameworkCore;
using OnlineTutor2.Data;
using OnlineTutor2.Models;

namespace OnlineTutor2.Services
{
    public class AuditLogService : IAuditLogService
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<AuditLogService> _logger;

        public AuditLogService(
            ApplicationDbContext context,
            ILogger<AuditLogService> logger)
        {
            _context = context;
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

                _context.AuditLogs.Add(auditLog);
                await _context.SaveChangesAsync();

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
            var query = _context.AuditLogs
                .Include(al => al.User)
                .AsQueryable();

            if (fromDate.HasValue)
                query = query.Where(al => al.CreatedAt >= fromDate.Value);

            if (toDate.HasValue)
                query = query.Where(al => al.CreatedAt <= toDate.Value);

            if (!string.IsNullOrEmpty(userId))
                query = query.Where(al => al.UserId == userId);

            if (!string.IsNullOrEmpty(action))
                query = query.Where(al => al.Action == action);

            if (!string.IsNullOrEmpty(entityType))
                query = query.Where(al => al.EntityType == entityType);

            return await query
                .OrderByDescending(al => al.CreatedAt)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();
        }

        public async Task<int> GetLogsCountAsync(
            DateTime? fromDate = null,
            DateTime? toDate = null,
            string? userId = null,
            string? action = null,
            string? entityType = null)
        {
            var query = _context.AuditLogs.AsQueryable();

            if (fromDate.HasValue)
                query = query.Where(al => al.CreatedAt >= fromDate.Value);

            if (toDate.HasValue)
                query = query.Where(al => al.CreatedAt <= toDate.Value);

            if (!string.IsNullOrEmpty(userId))
                query = query.Where(al => al.UserId == userId);

            if (!string.IsNullOrEmpty(action))
                query = query.Where(al => al.Action == action);

            if (!string.IsNullOrEmpty(entityType))
                query = query.Where(al => al.EntityType == entityType);

            return await query.CountAsync();
        }

        public async Task<List<AuditLog>> GetUserLogsAsync(string userId, int count = 10)
        {
            return await _context.AuditLogs
                .Include(al => al.User)
                .Where(al => al.UserId == userId)
                .OrderByDescending(al => al.CreatedAt)
                .Take(count)
                .ToListAsync();
        }

        public async Task ClearOldLogsAsync(int daysToKeep = 90)
        {
            var cutoffDate = DateTime.Now.AddDays(-daysToKeep);
            var oldLogs = await _context.AuditLogs
                .Where(al => al.CreatedAt < cutoffDate)
                .ToListAsync();

            if (oldLogs.Any())
            {
                _context.AuditLogs.RemoveRange(oldLogs);
                await _context.SaveChangesAsync();

                _logger.LogInformation(
                    "Cleared {Count} audit logs older than {Days} days",
                    oldLogs.Count, daysToKeep);
            }
        }
    }
}
