using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using OnlineTutor2.Models;

namespace OnlineTutor2.Hubs
{
    [Authorize(Roles = ApplicationRoles.Teacher)]
    public class TestAnalyticsHub : Hub
    {
        public async Task JoinTestAnalytics(int testId, string testType)
        {
            var groupName = $"{testType}_test_{testId}";
            await Groups.AddToGroupAsync(Context.ConnectionId, groupName);
        }

        public async Task LeaveTestAnalytics(int testId, string testType)
        {
            var groupName = $"{testType}_test_{testId}";
            await Groups.RemoveFromGroupAsync(Context.ConnectionId, groupName);
        }

        public override async Task OnDisconnectedAsync(Exception? exception)
        {
            await base.OnDisconnectedAsync(exception);
        }
    }
}
