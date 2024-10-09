namespace techmeet_api.Repositories
{
    using System;
    using techmeet_api.Data;
    using System.Threading.Tasks;
    using techmeet_api.Models;
    using Microsoft.EntityFrameworkCore;
    using System.Collections.Generic;
    using System.Linq;
    public class ChatMessageDTO
    {
        public int Id { get; set; }
        public string? Content { get; set; }
        public DateTime CreatedAt { get; set; }
        public bool IsRead { get; set; }
        public string? ReceiverId { get; set; }
        public string? ReceiverNickname { get; set; }
    }
    public interface IMessageService
    {
        Task<ChatMessage> SaveMessage(string senderId, string receiverId, string content);

        Task<IEnumerable<ChatMessageDTO>> GetMessagesForUser(string userId);
    }

    public class MessageService : IMessageService
    {
        private readonly ApplicationDbContext _context;

        public MessageService(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<ChatMessage> SaveMessage(string senderId, string receiverId, string content)
        {
            var newMessage = new ChatMessage
            {
                SenderId = senderId,
                ReceiverId = receiverId,
                Content = content,
                CreatedAt = DateTime.UtcNow,
                IsRead = false
            };

            _context.ChatMessages.Add(newMessage);
            await _context.SaveChangesAsync();

            return newMessage;
        }

        public async Task<IEnumerable<ChatMessageDTO>> GetMessagesForUser(string userId)
        {
            var messages = await _context.ChatMessages.Where(c => c.SenderId == userId).Select(c => new ChatMessageDTO
            {
                Id = c.Id,
                Content = c.Content,
                CreatedAt = c.CreatedAt,
                IsRead = c.IsRead,
                ReceiverId = c.Receiver.Id,
                ReceiverNickname = c.Receiver.Nickname
            }).ToListAsync();
            return messages;
        }
    }
}