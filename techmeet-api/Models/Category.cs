namespace techmeet_api.Models
{
    public class Category
    {
        public int Id { get; set; }
        public string Name { get; set; }

        // Navigation property for related events
        public ICollection<Event> Events { get; set; }
    }
}