namespace techmeet_api.Controller
{
    using System.Collections.Generic;
    using System.Linq;
    using System.Security.Claims;
    using System.Threading.Tasks;
    using Microsoft.AspNetCore.Authorization;
    using Microsoft.AspNetCore.Mvc;
    using Microsoft.EntityFrameworkCore;
    using techmeet_api.Data;
    using techmeet_api.Models;

    [ApiController]
    [Route("[controller]")]
    public class NotificationsController : ControllerBase
    {
        private readonly ApplicationDbContext _context;

        public NotificationsController(ApplicationDbContext context)
        {
            _context = context;
        }

        [Authorize(Roles = "user, vip, admin")]
        [HttpGet]
        public async Task<ActionResult<IEnumerable<Notification>>> GetNotifications()
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var notifications = await _context.Notifications
                .Where(n => n.UserId == userId && n.IsRead == false)
                .OrderByDescending(n => n.CreatedAt)
                .ToListAsync();

            return notifications;
        }


        [Authorize(Roles = "user, vip, admin")]
        [HttpPut("{id}")]
        public async Task<IActionResult> MarkNotificationAsRead(int id)
        {
            var notification = await _context.Notifications
                .Where(n => n.Id == id)
                .FirstOrDefaultAsync();

            if (notification == null)
            {
                return NotFound();
            }

            notification.IsRead = true;
            _context.Notifications.Update(notification);
            await _context.SaveChangesAsync();

            return NoContent();
        }
    }

}