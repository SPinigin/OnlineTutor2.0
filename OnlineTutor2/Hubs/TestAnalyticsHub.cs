using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using OnlineTutor2.Models;

namespace OnlineTutor2.Hubs
{
    [Authorize(Roles = ApplicationRoles.Teacher)]
    public class TestAnalyticsHub : Hub
    {
        private readonly ILogger<TestAnalyticsHub> _logger;

        public TestAnalyticsHub(ILogger<TestAnalyticsHub> logger)
        {
            _logger = logger;
        }

        public override async Task OnConnectedAsync()
        {
            var userId = Context.User?.Identity?.Name;
            _logger.LogInformation("SignalR: Пользователь {UserId} подключился. ConnectionId: {ConnectionId}",
                userId, Context.ConnectionId);
            await base.OnConnectedAsync();
        }

        // Присоединение к группе конкретного теста (для страницы аналитики теста)
        public async Task JoinTestAnalytics(int testId, string testType)
        {
            var groupName = $"{testType}_test_{testId}";
            await Groups.AddToGroupAsync(Context.ConnectionId, groupName);

            _logger.LogInformation("SignalR: ConnectionId {ConnectionId} присоединился к группе теста {GroupName}",
                Context.ConnectionId, groupName);
        }

        public async Task LeaveTestAnalytics(int testId, string testType)
        {
            var groupName = $"{testType}_test_{testId}";
            await Groups.RemoveFromGroupAsync(Context.ConnectionId, groupName);

            _logger.LogInformation("SignalR: ConnectionId {ConnectionId} покинул группу теста {GroupName}",
                Context.ConnectionId, groupName);
        }

        // Присоединение к глобальной группе учителя (для Dashboard)
        public async Task JoinTeacherDashboard(string teacherId)
        {
            var groupName = $"teacher_{teacherId}";
            await Groups.AddToGroupAsync(Context.ConnectionId, groupName);

            _logger.LogInformation("SignalR: ConnectionId {ConnectionId} присоединился к Dashboard учителя {TeacherId}",
                Context.ConnectionId, teacherId);
        }

        public async Task LeaveTeacherDashboard(string teacherId)
        {
            var groupName = $"teacher_{teacherId}";
            await Groups.RemoveFromGroupAsync(Context.ConnectionId, groupName);

            _logger.LogInformation("SignalR: ConnectionId {ConnectionId} покинул Dashboard учителя {TeacherId}",
                Context.ConnectionId, teacherId);
        }

        public override async Task OnDisconnectedAsync(Exception? exception)
        {
            var userId = Context.User?.Identity?.Name;
            _logger.LogInformation("SignalR: Пользователь {UserId} отключился. ConnectionId: {ConnectionId}",
                userId, Context.ConnectionId);

            if (exception != null)
            {
                _logger.LogError(exception, "SignalR: Ошибка при отключении");
            }

            await base.OnDisconnectedAsync(exception);
        }
    }
}
