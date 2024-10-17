namespace techmeet_api.Hubs
{
    using Microsoft.AspNetCore.SignalR;
    using Microsoft.AspNetCore.Authorization;
    using System.Threading.Tasks;
    using techmeet_api.Repositories;
    using System;
    using System.Collections.Concurrent;

    [Authorize]
    public class ChatHub : Hub
    {
        private readonly IMessageService _messageService;
        private static ConcurrentDictionary<string, bool> _connectedUsers = new ConcurrentDictionary<string, bool>();

        public override async Task OnConnectedAsync()
        {
            var userId = Context.UserIdentifier;
            if (userId == null) return;
            _connectedUsers.TryAdd(userId, true);
            await base.OnConnectedAsync();
        }

        public override async Task OnDisconnectedAsync(Exception? exception)
        {
            var userId = Context.UserIdentifier;
            if (userId == null) return;
            _connectedUsers.TryRemove(userId, out _);
            await base.OnDisconnectedAsync(exception);
        }

        public ChatHub(IMessageService messageService)
        {
            _messageService = messageService;
        }

        public async Task SendMessageToUser(string receiverId, string message) // "SendMessageToUser" method called from frontend
        {
            var senderId = Context.UserIdentifier;
            if (senderId == null) return;
            // Save chat records to DB 
            var newMessage = await _messageService.SaveMessage(senderId, receiverId, message);
            await Clients.User(receiverId).SendAsync("ReceiveMessage", newMessage);
            await Clients.User(senderId).SendAsync("ReceiveMessage", newMessage);
        }
    }
}