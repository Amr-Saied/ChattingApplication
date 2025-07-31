using System.Security.Claims;
using System.Threading.Tasks;
using ChattingApplicationProject.DTO;
using ChattingApplicationProject.Hubs;
using ChattingApplicationProject.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;

namespace ChattingApplicationProject.Controllers
{
    [ApiController]
    [Route("[controller]")]
    [Authorize]
    public class MessageController : ControllerBase
    {
        private readonly IMessageService _messageService;
        private readonly IHubContext<MessageHub> _hubContext;

        public MessageController(IMessageService messageService, IHubContext<MessageHub> hubContext)
        {
            _messageService = messageService;
            _hubContext = hubContext;
        }

        [HttpGet("conversations")]
        public async Task<IActionResult> GetConversations()
        {
            var currentUserId = GetCurrentUserId();
            if (currentUserId == 0)
                return Unauthorized();

            var conversations = await _messageService.GetConversations(currentUserId);
            return Ok(conversations);
        }

        [HttpGet("{otherUserId}")]
        public async Task<IActionResult> GetMessages(int otherUserId)
        {
            var currentUserId = GetCurrentUserId();
            if (currentUserId == 0)
                return Unauthorized();

            var messages = await _messageService.GetMessages(currentUserId, otherUserId);
            return Ok(messages);
        }

        [HttpPost]
        public async Task<IActionResult> SendMessage([FromBody] CreateMessageDto messageDto)
        {
            var currentUserId = GetCurrentUserId();
            if (currentUserId == 0)
                return Unauthorized();

            if (string.IsNullOrWhiteSpace(messageDto.Content))
                return BadRequest("Message content cannot be empty");

            try
            {
                var message = await _messageService.SendMessage(
                    currentUserId,
                    messageDto.RecipientId,
                    messageDto.Content,
                    messageDto.Emoji
                );

                // Notify ONLY the recipient via SignalR
                await _hubContext
                    .Clients.User(message.RecipientId.ToString())
                    .SendAsync(
                        "ReceiveMessage",
                        new
                        {
                            Id = message.Id,
                            SenderId = message.SenderId,
                            SenderUsername = message.SenderUsername,
                            RecipientId = message.RecipientId,
                            RecipientUsername = message.RecipientUsername,
                            Content = message.Content,
                            Emoji = message.Emoji,
                            MessageSent = message.MessageSent,
                            DateRead = message.DateRead
                        }
                    );

                return Ok(message);
            }
            catch (ArgumentException ex)
            {
                return BadRequest(ex.Message);
            }
        }

        [HttpPut("{messageId}/read")]
        public async Task<IActionResult> MarkAsRead(int messageId)
        {
            var currentUserId = GetCurrentUserId();
            if (currentUserId == 0)
                return Unauthorized();

            var result = await _messageService.MarkAsRead(messageId, currentUserId);
            if (result)
            {
                // Notify ONLY the sender via SignalR that message was read
                var message = await _messageService.GetMessage(messageId);
                if (message != null)
                {
                    await _hubContext
                        .Clients.User(message.SenderId.ToString())
                        .SendAsync("MessageRead", messageId, currentUserId);
                }
                return Ok(new { success = true });
            }

            return BadRequest("Failed to mark message as read");
        }

        [HttpDelete("{messageId}")]
        public async Task<IActionResult> DeleteMessage(int messageId)
        {
            var currentUserId = GetCurrentUserId();
            if (currentUserId == 0)
                return Unauthorized();

            var result = await _messageService.DeleteMessage(messageId, currentUserId);
            if (result)
                return Ok(new { success = true });

            return BadRequest("Failed to delete message");
        }

        [HttpGet("unread-count")]
        public async Task<IActionResult> GetUnreadCount()
        {
            var currentUserId = GetCurrentUserId();
            if (currentUserId == 0)
                return Unauthorized();

            var count = await _messageService.GetUnreadCount(currentUserId);
            return Ok(new { unreadCount = count });
        }

        private int GetCurrentUserId()
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            return int.TryParse(userIdClaim, out int userId) ? userId : 0;
        }
    }
}
