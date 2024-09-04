using Microsoft.AspNetCore.Identity;

namespace techmeet_api.Models
{
    public class User : IdentityUser
    {
        public string? Nickname { get; set; }
        public DateTime? VIPExpirationDate { get; set; }

        // add a navigation property to the Event class
        public virtual ICollection<Event> Events { get; set; }
    }
}