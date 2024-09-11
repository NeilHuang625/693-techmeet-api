using techmeet_api.Models;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;


namespace techmeet_api.Data
{

    public class ApplicationDbContext : IdentityDbContext<User>
    {
        public DbSet<Event> Events { get; set; }
        public DbSet<RevokedToken> RevokedTokens { get; set; }
        public DbSet<Category> Categories { get; set; }
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options)
        {
            ChangeTracker.LazyLoadingEnabled = true;
        }

        protected override void OnModelCreating(ModelBuilder builder)
        {
            base.OnModelCreating(builder);

            // Define relationships between User and Event
            builder.Entity<User>()
                .HasMany(u => u.Events)
                .WithOne(e => e.User)
                .HasForeignKey(e => e.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            // Seed data for the Category table
            builder.Entity<Category>().HasData(
                new Category { Id = 1, Name = "Web Development" },
                new Category { Id = 2, Name = "AI & Machine Learning" },
                new Category { Id = 3, Name = "Cloud Computing" },
                new Category { Id = 4, Name = "DevOps" },
                new Category { Id = 5, Name = "Cybersecurity" },
                new Category { Id = 6, Name = "Data Science" },
                new Category { Id = 7, Name = "Mobile Development" }
            );
        }

    }
}