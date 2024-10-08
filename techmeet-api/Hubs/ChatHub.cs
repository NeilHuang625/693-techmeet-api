namespace techmeet_api.Hubs
{
    using Microsoft.AspNetCore.SignalR;
    using Microsoft.AspNetCore.Authorization;
    using System.Threading.Tasks;
    using techmeet_api.Repositories;
    using System.Security.Claims;
    using Microsoft.AspNetCore.Http;
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
            _connectedUsers.TryAdd(userId, true);
            await base.OnConnectedAsync();
        }

        public override async Task OnDisconnectedAsync(Exception exception)
        {
            var userId = Context.UserIdentifier;
            _connectedUsers.TryRemove(userId, out _);
            await base.OnDisconnectedAsync(exception);
        }

        public ChatHub(IMessageService messageService)
        {
            _messageService = messageService;
        }

        public async Task SendMessageToUser(string userId, string message)
        {
            if (IsUserConnected(userId))
            {
                await Clients.User(userId).SendAsync("ReceiveMessage", message);
            }
            else
            {
                await _messageService.SaveOfflineMessage(Context.UserIdentifier, userId, message);
            }
        }

        private bool IsUserConnected(string userId)
        {
            return _connectedUsers.ContainsKey(userId);
        }
    }
}