using System;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.SignalR;

namespace RagChatbotSystem.Presentation.Hubs
{
    public class NotificationHub : Hub
    {
        public override async Task OnConnectedAsync()
        {
            var userIdVal = Context.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (!string.IsNullOrEmpty(userIdVal) && Guid.TryParse(userIdVal, out var userGuid))
            {
                await Groups.AddToGroupAsync(Context.ConnectionId, $"user_{userGuid}");
            }
            await base.OnConnectedAsync();
        }

        public async Task RegisterUser(string userId)
        {
            if (Guid.TryParse(userId, out var userGuid))
            {
                await Groups.AddToGroupAsync(Context.ConnectionId, $"user_{userGuid}");
            }
        }
    }
}
