using System;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;

namespace ITHunterview.Service.Hubs
{
    [Authorize]
    public class NotificationHub : Hub
    {
        public override async Task OnConnectedAsync()
        {
            var userIdStr = Context.User?.FindFirstValue("userId") ?? Context.User?.FindFirstValue(ClaimTypes.NameIdentifier);
            if (!string.IsNullOrEmpty(userIdStr))
            {
                // Group connections by userId so we can send targeted notifications
                await Groups.AddToGroupAsync(Context.ConnectionId, userIdStr);
            }
            
            await base.OnConnectedAsync();
        }

        public override async Task OnDisconnectedAsync(Exception? exception)
        {
            var userIdStr = Context.User?.FindFirstValue("userId") ?? Context.User?.FindFirstValue(ClaimTypes.NameIdentifier);
            if (!string.IsNullOrEmpty(userIdStr))
            {
                await Groups.RemoveFromGroupAsync(Context.ConnectionId, userIdStr);
            }
            
            await base.OnDisconnectedAsync(exception);
        }
    }
}
