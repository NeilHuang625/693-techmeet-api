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
        public DbSet<Attendance> Attendances { get; set; }
        public DbSet<Waitlist> Waitlists { get; set; }
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options)
        {
            ChangeTracker.LazyLoadingEnabled = true;
        }

        protected override void OnModelCreating(ModelBuilder builder)
        {
            base.OnModelCreating(builder);

            // Define relationships between User, Event and Attendance
            builder.Entity<User>()
                .HasMany(u => u.Events)
                .WithOne(e => e.User)
                .HasForeignKey(e => e.UserId)
                .OnDelete(DeleteBehavior.Restrict);

            // Define relationships between User and Attendance
            builder.Entity<Attendance>()
                .HasKey(a => new { a.UserId, a.EventId });

            builder.Entity<Attendance>()
                .HasOne(a => a.User)
                .WithMany(u => u.Attendances)
                .HasForeignKey(a => a.UserId)
                .OnDelete(DeleteBehavior.NoAction);

            builder.Entity<Attendance>()
                .HasOne(a => a.Event)
                .WithMany(e => e.Attendances)
                .HasForeignKey(a => a.EventId)
                .OnDelete(DeleteBehavior.NoAction);

            // Define relationships between User, Event, and Waitlist
            builder.Entity<Waitlist>()
                .HasKey(w => new { w.UserId, w.EventId });

            builder.Entity<Waitlist>()
                .HasOne(w => w.User)
                .WithMany(u => u.Waitlists)
                .HasForeignKey(w => w.UserId)
                .OnDelete(DeleteBehavior.NoAction);

            builder.Entity<Waitlist>()
                .HasOne(w => w.Event)
                .WithMany(e => e.Waitlists)
                .HasForeignKey(w => w.EventId)
                .OnDelete(DeleteBehavior.NoAction);

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