using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using ChattingApplicationProject.DTO;

namespace ChattingApplicationProject.Interfaces
{
    public interface IMessageService
    {
        // Get all conversations for current user
        Task<List<ConversationDto>> GetConversations(int currentUserId);

        // Get messages between two users
        Task<List<MessageDto>> GetMessages(int currentUserId, int otherUserId);

        // Send a message
        Task<MessageDto> SendMessage(int senderId, int recipientId, string content);

        // Mark message as read
        Task<bool> MarkAsRead(int messageId, int currentUserId);

        // Delete message
        Task<bool> DeleteMessage(int messageId, int currentUserId);

        // Get unread message count
        Task<int> GetUnreadCount(int currentUserId);
    }
}
