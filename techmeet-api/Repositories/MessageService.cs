namespace techmeet_api.Repositories
{
    using System;
    using techmeet_api.Data;
    using System.Threading.Tasks;
    using techmeet_api.Models;
    using Microsoft.EntityFrameworkCore;
    using System.Collections.Generic;
    using System.Linq;
    using Microsoft.AspNetCore.Http.HttpResults;

    public class ChatMessageDTO
    {
        public int Id { get; set; }
        public string? Content { get; set; }
        public DateTime CreatedAt { get; set; }
        public bool IsRead { get; set; }
        public string? ReceiverId { get; set; }
        public string? SenderId { get; set; }
        public string? ReceiverNickname { get; set; }
        public string? SenderNickname { get; set; }
        public string? ReceiverImageUrl { get; set; }
        public string? SenderImageUrl { get; set; }
    }
    public interface IMessageService
    {
        Task<ChatMessageDTO> SaveMessage(string senderId, string receiverId, string content);

        Task<IEnumerable<ChatMessageDTO>> GetMessagesForUser(string userId, string receiverId);
        Task MarkMessagesAsRead(string userId, string receiverId);
        Task<IEnumerable<ChatMessageDTO>> GetAllMessages(string userId);
    }

    public class MessageService : IMessageService
    {
        private readonly ApplicationDbContext _context;
        public MessageService(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<ChatMessageDTO> SaveMessage(string senderId, string receiverId, string content)
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

            _context.Entry(newMessage).Reference(c => c.Receiver).Load();
            _context.Entry(newMessage).Reference(c => c.Sender).Load();

            var messageDTO = new ChatMessageDTO
            {
                Id = newMessage.Id,
                Content = newMessage.Content,
                CreatedAt = newMessage.CreatedAt,
                IsRead = newMessage.IsRead,
                SenderId = newMessage.SenderId,
                ReceiverId = newMessage.Receiver.Id,
                ReceiverNickname = newMessage.Receiver.Nickname,
                SenderNickname = newMessage.Sender.Nickname,
                ReceiverImageUrl = newMessage.Receiver.ProfilePhotoUrl,
                SenderImageUrl = newMessage.Sender.ProfilePhotoUrl
            };

            return messageDTO;
        }

        // Get all messages
        public async Task<IEnumerable<ChatMessageDTO>> GetAllMessages(string userId)
        {
            var messages = await _context.ChatMessages
                .Include(c => c.Receiver)
                .Include(c => c.Sender)
                .Where(c => c.SenderId == userId || c.ReceiverId == userId)
                .Select(c => new ChatMessageDTO
                {
                    Id = c.Id,
                    Content = c.Content,
                    CreatedAt = c.CreatedAt,
                    IsRead = c.IsRead,
                    SenderId = c.SenderId,
                    ReceiverId = c.Receiver.Id,
                    ReceiverNickname = c.Receiver.Nickname,
                    SenderNickname = c.Sender.Nickname,
                    ReceiverImageUrl = c.Receiver.ProfilePhotoUrl,
                    SenderImageUrl = c.Sender.ProfilePhotoUrl
                })
                .ToListAsync();
            return messages;
        }

        public async Task<IEnumerable<ChatMessageDTO>> GetMessagesForUser(string userId, string receiverId)
        {
            var messages = await _context.ChatMessages
                .Include(c => c.Receiver)
                .Include(c => c.Sender)
                .Where(c => c.SenderId == userId || c.ReceiverId == userId)
                .Select(c => new ChatMessageDTO
                {
                    Id = c.Id,
                    Content = c.Content,
                    CreatedAt = c.CreatedAt,
                    IsRead = c.IsRead,
                    SenderId = c.SenderId,
                    ReceiverId = c.Receiver.Id,
                    ReceiverNickname = c.Receiver.Nickname,
                    SenderNickname = c.Sender.Nickname,
                    ReceiverImageUrl = c.Receiver.ProfilePhotoUrl,
                    SenderImageUrl = c.Sender.ProfilePhotoUrl
                })
                .ToListAsync();
            return messages;
        }

        // Mark messages as read
        public async Task MarkMessagesAsRead(string userId, string receiverId)
        {
            var messages = await _context.ChatMessages
                .Where(c => c.SenderId == receiverId && c.ReceiverId == userId && c.IsRead == false)
                .ToListAsync();

            foreach (var message in messages)
            {
                message.IsRead = true;
            }

            await _context.SaveChangesAsync();
        }
    }
}