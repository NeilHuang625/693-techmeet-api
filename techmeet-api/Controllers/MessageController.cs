using Microsoft.AspNetCore.Mvc;
using techmeet_api.Repositories;
using Microsoft.AspNetCore.Authorization;
using System.Security.Claims;

namespace techmeet_api.Controllers
{

    [ApiController]
    [Route("[controller]")]
    public class MessageController : ControllerBase
    {
        private readonly IMessageService _messageService;

        public MessageController(IMessageService messageService)
        {
            _messageService = messageService;
        }

        [Authorize]
        [HttpGet]
        public async Task<IActionResult> GetAllMessages()
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (userId == null) return Unauthorized();
            var messages = await _messageService.GetAllMessages(userId);
            return Ok(messages);
        }

        [Authorize]
        [HttpGet("{receiverId}")]
        public async Task<IActionResult> GetMessagesForUser(string receiverId)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (userId == null) return Unauthorized();
            var messages = await _messageService.GetMessagesForUser(userId, receiverId);
            return Ok(messages);
        }

        [Authorize]
        [HttpPut("{receiverId}")]
        public async Task<IActionResult> MarkMessagesAsRead(string receiverId)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (userId == null) return Unauthorized();
            await _messageService.MarkMessagesAsRead(userId, receiverId);
            return Ok();
        }
    }
}