using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using techmeet_api.Data;
using techmeet_api.Models;
using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;

namespace techmeet_api.Controllers
{
    [Authorize(Roles = "vip,admin")]
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
                // Convert the start and end times to UTC
                StartTime = model.StartTime.ToUniversalTime(),
                EndTime = model.EndTime.ToUniversalTime(),
                MaxAttendees = model.MaxAttendees,
                Promoted = model.Promoted,
                UserId = model.UserId,
                CategoryId = model.CategoryId,
                CurrentAttendees = 0
            };

            var blobServiceClient = new BlobServiceClient(_configuration["BLOB_STORAGE_CONNECTION_STRING"]);
            var containerClient = blobServiceClient.GetBlobContainerClient("uploads"); // Create a container called "uploads"
            await containerClient.CreateIfNotExistsAsync(); // Create the container if it doesn't exist

            var uniqueFileName = Guid.NewGuid().ToString() + "_" + model.ImageFile.FileName;
            var blobClient = containerClient.GetBlobClient(uniqueFileName); // Create a blob client for the image file

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

    }
}