using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using techmeet_api.Data;
using techmeet_api.Models;
using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using Microsoft.EntityFrameworkCore;

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

    }
}