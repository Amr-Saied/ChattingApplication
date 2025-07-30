using System.Security.Claims;
using ChattingApplicationProject.DTO;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;

namespace ChattingApplicationProject.Hubs
{
    [Authorize]
    public class MessageHub : Hub
    {
        private static readonly Dictionary<string, string> UserConnections = new();

        public override async Task OnConnectedAsync()
        {
            var userId = GetCurrentUserId();
            if (userId > 0)
            {
                UserConnections[userId.ToString()] = Context.ConnectionId;
                await Clients.All.SendAsync("UserConnected", userId);
            }
            await base.OnConnectedAsync();
        }

        public override async Task OnDisconnectedAsync(Exception? exception)
        {
            var userId = GetCurrentUserId();
            if (userId > 0)
            {
                UserConnections.Remove(userId.ToString());
                await Clients.All.SendAsync("UserDisconnected", userId);
            }
            await base.OnDisconnectedAsync(exception);
        }

        public async Task SendMessage(int recipientId, string content)
        {
            var senderId = GetCurrentUserId();
            if (senderId == 0)
                return;

            // Send to recipient if online
            if (UserConnections.TryGetValue(recipientId.ToString(), out var connectionId))
            {
                await Clients
                    .Client(connectionId)
                    .SendAsync(
                        "ReceiveMessage",
                        new
                        {
                            SenderId = senderId,
                            Content = content,
                            MessageSent = DateTime.UtcNow
                        }
                    );
            }

            // Send back to sender for confirmation
            await Clients.Caller.SendAsync(
                "MessageSent",
                new
                {
                    RecipientId = recipientId,
                    Content = content,
                    MessageSent = DateTime.UtcNow
                }
            );
        }

        public async Task Typing(int recipientId)
        {
            var senderId = GetCurrentUserId();
            if (senderId == 0)
                return;

            if (UserConnections.TryGetValue(recipientId.ToString(), out var connectionId))
            {
                await Clients.Client(connectionId).SendAsync("UserTyping", senderId);
            }
        }

        public async Task StopTyping(int recipientId)
        {
            var senderId = GetCurrentUserId();
            if (senderId == 0)
                return;

            if (UserConnections.TryGetValue(recipientId.ToString(), out var connectionId))
            {
                await Clients.Client(connectionId).SendAsync("UserStoppedTyping", senderId);
            }
        }

        public async Task MarkAsRead(int messageId, int senderId)
        {
            var currentUserId = GetCurrentUserId();
            if (currentUserId == 0)
                return;

            // Notify the sender that their message was read
            if (UserConnections.TryGetValue(senderId.ToString(), out var connectionId))
            {
                await Clients
                    .Client(connectionId)
                    .SendAsync("MessageRead", messageId, currentUserId);
            }
        }

        public async Task JoinUserGroup(int userId)
        {
            await Groups.AddToGroupAsync(Context.ConnectionId, $"user_{userId}");
        }

        public async Task LeaveUserGroup(int userId)
        {
            await Groups.RemoveFromGroupAsync(Context.ConnectionId, $"user_{userId}");
        }

        private int GetCurrentUserId()
        {
            var userIdClaim = Context.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            return int.TryParse(userIdClaim, out int userId) ? userId : 0;
        }
    }
}
