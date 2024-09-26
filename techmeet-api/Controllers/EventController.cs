using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using techmeet_api.Data;
using techmeet_api.Models;
using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace techmeet_api.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class EventController : ControllerBase
    {
        private readonly ApplicationDbContext _context;
        private readonly IConfiguration _configuration;

        public EventController(ApplicationDbContext context, IConfiguration configuration)
        {
            _context = context;
            _configuration = configuration;
        }
        // Define a Data Transfer Object (DTO) for receiving data from the front end
        public class EventDTO
        {
            public string Title { get; set; }
            public string Description { get; set; }
            public string Location { get; set; }
            public string City { get; set; }
            public DateTime StartTime { get; set; }
            public DateTime EndTime { get; set; }
            public IFormFile ImageFile { get; set; }
            public int MaxAttendees { get; set; }
            public bool Promoted { get; set; }
            public string UserId { get; set; }
            public int CategoryId { get; set; }
        };

        [Authorize(Roles = "vip,admin")]
        [HttpPost]
        public async Task<IActionResult> CreateEvent([FromForm] EventDTO model)
        {
            var newEvent = new Event
            {
                Title = model.Title,
                Description = model.Description,
                Location = model.Location,
                City = model.City,
                // Convert the start and end times to UTC
                StartTime = model.StartTime.ToUniversalTime(),
                EndTime = model.EndTime.ToUniversalTime(),
                MaxAttendees = model.MaxAttendees,
                Promoted = model.Promoted,
                UserId = model.UserId,
                CategoryId = model.CategoryId,
                CurrentAttendees = 0
            };
            // Create a blob service client to interact with the Azure blob storage
            var blobServiceClient = new BlobServiceClient(_configuration["BLOB_STORAGE_CONNECTION_STRING"]);
            var containerClient = blobServiceClient.GetBlobContainerClient("uploads"); // Create a container called "uploads"
            await containerClient.CreateIfNotExistsAsync(); // Create the container if it doesn't exist
            // Generate a unique file name for the image
            var uniqueFileName = Guid.NewGuid().ToString() + "_" + model.ImageFile.FileName;
            // Create a blob client for the image file
            var blobClient = containerClient.GetBlobClient(uniqueFileName);

            // Upload the image to the Azure blob storage
            using (var stream = model.ImageFile.OpenReadStream())
            {
                await blobClient.UploadAsync(stream, overwrite: true);
            }

            newEvent.ImageUrl = blobClient.Uri.AbsoluteUri;

            _context.Events.Add(newEvent);
            await _context.SaveChangesAsync();

            return Ok(newEvent);
        }

        [HttpGet]
        public async Task<IActionResult> GetAllEvents()
        {
            var events = await _context.Events.Include(e => e.Category).Include(e => e.User).Select(e => new
            {
                e.Id,
                e.Title,
                e.Description,
                e.Location,
                e.City,
                e.StartTime,
                e.EndTime,
                e.ImageUrl,
                e.MaxAttendees,
                e.CurrentAttendees,
                e.Promoted,
                e.UserId,
                e.CategoryId,
                Category = e.Category.Name,
                User = e.User.Nickname
            }).ToListAsync();

            return Ok(events);
        }

        [Authorize(Roles = "user,vip,admin")]
        [HttpGet("{UserId}")]
        public async Task<IActionResult> GetEventsByUser(string UserId)
        {
            // Events the user is attending
            var attendingEvents = await _context.Attendances.Where(a => a.UserId == UserId).Select(a => a.EventId).ToListAsync();
            // Events the user is on the waitlist for
            var waitlistedEvents = await _context.Waitlists.Where(w => w.UserId == UserId).Select(w => w.EventId).ToListAsync();

            return Ok(new { attendingEvents, waitlistedEvents });
        }

        [Authorize(Roles = "user,vip, admin")]
        [HttpPost("attend/{EventId}")]
        public async Task<IActionResult> AttendEvent(int EventId)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var user = await _context.Users.FindAsync(userId);
            var @event = await _context.Events.FindAsync(EventId);

            if (@event == null)
            {
                return NotFound("Event not found");
            }

            if (@event.CurrentAttendees >= @event.MaxAttendees)
            {
                return BadRequest("Event is full");
            }

            var attendance = new Attendance
            {
                UserId = userId,
                EventId = EventId,
                AttendedAt = DateTime.UtcNow,
            };

            _context.Attendances.Add(attendance);
            @event.CurrentAttendees++;
            await _context.SaveChangesAsync();

            return Ok(new { CurrentAttendees = @event.CurrentAttendees });
        }

        [Authorize(Roles = "user, vip, admin")]
        [HttpDelete("withdraw/{EventId}")]
        public async Task<IActionResult> WithdrawEvent(int EventId)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var attendance = await _context.Attendances.FirstOrDefaultAsync(a => a.UserId == userId && a.EventId == EventId);
            var @event = await _context.Events.FindAsync(EventId);

            if (attendance == null)
            {
                return NotFound("Attendance not found");
            }

            _context.Attendances.Remove(attendance);
            @event.CurrentAttendees--;

            // Find earliest waitlist record if there are users on the waitlist
            var waitlist = await _context.Waitlists.Where(w => w.EventId == EventId).OrderBy(w => w.AddedAt).FirstOrDefaultAsync();
            if (waitlist != null)
            {
                // Remove the user from the waitlist
                _context.Waitlists.Remove(waitlist);

                // Add the user to the attendance list
                var newAttendance = new Attendance
                {
                    UserId = waitlist.UserId,
                    EventId = EventId,
                    AttendedAt = DateTime.UtcNow
                };
                _context.Attendances.Add(newAttendance);
                @event.CurrentAttendees++;
            }


            await _context.SaveChangesAsync();

            return Ok(new { CurrentAttendees = @event.CurrentAttendees });
        }

        [Authorize(Roles = "user, vip, admin")]
        [HttpPost("waitlist/{EventId}")]
        public async Task<IActionResult> AddToWaitlist(int EventId)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var @event = await _context.Events.FindAsync(EventId);

            if (@event == null)
            {
                return NotFound("Event not found");
            }

            if (@event.CurrentAttendees < @event.MaxAttendees)
            {
                return BadRequest("Event is not full");
            }
            else
            {
                var waitlist = new Waitlist
                {
                    UserId = userId,
                    EventId = EventId,
                    AddedAt = DateTime.UtcNow
                };

                _context.Waitlists.Add(waitlist);
                await _context.SaveChangesAsync();

                return Ok();
            }
        }

        [Authorize(Roles = "vip,admin")]
        [HttpDelete("{EventId}")]
        public async Task<IActionResult> DeleteEvent(int EventId)
        {
            var @event = await _context.Events.FindAsync(EventId);

            if (@event == null)
            {
                return NotFound("Event Not Found");
            }

            // Find and delete all related Attendance records
            var attendances = await _context.Attendances.Where(a => a.EventId == EventId).ToListAsync();
            _context.Attendances.RemoveRange(attendances);

            // Find and delete all related Waitlist records
            var waitlists = await _context.Waitlists.Where(w => w.EventId == EventId).ToListAsync();
            _context.Waitlists.RemoveRange(waitlists);

            _context.Events.Remove(@event);
            await _context.SaveChangesAsync();

            return Ok();
        }

    }
}