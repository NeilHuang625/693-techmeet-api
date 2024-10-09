namespace techmeet_api.Repositories
{
    using System;
    using techmeet_api.Data;
    using System.Threading.Tasks;
    using techmeet_api.Models;

    public interface IMessageService
    {
        Task SaveMessage(string senderId, string receiverId, string content);
    }

    public class MessageService : IMessageService
    {
        private readonly ApplicationDbContext _context;

        public MessageService(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task SaveMessage(string senderId, string receiverId, string content)
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
        }
    }
}