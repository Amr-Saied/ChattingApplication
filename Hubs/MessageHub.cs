using System.Security.Claims;
using ChattingApplicationProject.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;

namespace ChattingApplicationProject.Hubs
{
    [Authorize]
    public class MessageHub : Hub
    {
        private static readonly Dictionary<string, string> UserConnections = new();
        private static readonly Dictionary<int, HashSet<string>> OnlineUsers = new();
        private readonly DataContext _context;

        public MessageHub(DataContext context)
        {
            _context = context;
        }

        public override async Task OnConnectedAsync()
        {
            var userId = Context.UserIdentifier;
            if (int.TryParse(userId, out int userIdInt))
            {
                // Track this connection for the user
                if (!OnlineUsers.ContainsKey(userIdInt))
                {
                    OnlineUsers[userIdInt] = new HashSet<string>();
                    // Only notify if this is the first connection for this user
                    await Clients.All.SendAsync("UserOnline", userIdInt);
                }

                OnlineUsers[userIdInt].Add(Context.ConnectionId);
                UserConnections[userIdInt.ToString()] = Context.ConnectionId;

                // Send updated online users list to all clients
                await Clients.All.SendAsync("OnlineUsersUpdate", GetOnlineUserIds());
            }

            await base.OnConnectedAsync();
        }

        public override async Task OnDisconnectedAsync(Exception exception)
        {
            var userId = Context.UserIdentifier;
            if (int.TryParse(userId, out int userIdInt))
            {
                // Remove this specific connection
                if (OnlineUsers.ContainsKey(userIdInt))
                {
                    OnlineUsers[userIdInt].Remove(Context.ConnectionId);

                    // Only mark user as offline if no connections remain
                    if (OnlineUsers[userIdInt].Count == 0)
                    {
                        OnlineUsers.Remove(userIdInt);
                        await Clients.All.SendAsync("UserOffline", userIdInt);
                    }
                }

                UserConnections.Remove(userIdInt.ToString());

                // Send updated online users list
                await Clients.All.SendAsync("OnlineUsersUpdate", GetOnlineUserIds());
            }

            await base.OnDisconnectedAsync(exception);
        }

        // Helper method to get current online user IDs
        private List<int> GetOnlineUserIds()
        {
            return OnlineUsers.Keys.ToList();
        }

        public async Task SendMessage(int recipientId, string content)
        {
            // NOTE: This method is no longer used for sending messages
            // Messages are now sent via MessageController which handles both DB storage and SignalR broadcast
            // This method remains for backward compatibility but doesn't send duplicate SignalR messages

            var senderId = GetCurrentUserId();
            if (senderId == 0)
                return;

            Console.WriteLine(
                $"⚠️ SendMessage called on Hub - this should use MessageController instead"
            );
            Console.WriteLine($"Sender: {senderId}, Recipient: {recipientId}, Content: {content}");

            // Don't send SignalR messages here anymore - MessageController handles it
            // This prevents duplicate messages with different structures
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

        private async Task<string> GetCurrentUserName()
        {
            var userId = GetCurrentUserId();
            if (userId == 0)
                return "Unknown User";

            try
            {
                var user = await _context.Users.FindAsync(userId);
                return user?.UserName ?? $"User {userId}";
            }
            catch
            {
                return $"User {userId}";
            }
        }
    }
}
