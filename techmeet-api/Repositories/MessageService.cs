namespace techmeet_api.Repositories
{
    using System;
    using techmeet_api.Data;
    using System.Threading.Tasks;
    using techmeet_api.Models;

    public interface IMessageService
    {
        Task SaveOfflineMessage(string senderId, string receiverId, string message);
    }

    public class MessageService : IMessageService
    {
        private readonly ApplicationDbContext _context;

        public MessageService(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task SaveOfflineMessage(string senderId, string receiverId, string message)
        {
            var offlineMessage = new OfflineMessage
            {
                SenderId = senderId,
                ReceiverId = receiverId,
                Message = message,
                CreatedAt = DateTime.UtcNow,
                IsRead = false
            };

            _context.OfflineMessages.Add(offlineMessage);
            await _context.SaveChangesAsync();
        }
    }
}