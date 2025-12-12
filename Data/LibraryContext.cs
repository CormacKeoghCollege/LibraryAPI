using Microsoft.EntityFrameworkCore;
using LibraryAPI2.Models;

namespace LibraryAPI2.Data
{
    public class LibraryContext : DbContext
    {
        public LibraryContext(DbContextOptions<LibraryContext> options) : base(options) { }
        
        public DbSet<Book> Books { get; set; } = null!;
        public DbSet<User> Users { get; set; } = null!;

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<Book>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Title).IsRequired().HasMaxLength(200);
                entity.Property(e => e.Author).IsRequired().HasMaxLength(200);
            });

            modelBuilder.Entity<User>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Email).IsRequired().HasMaxLength(100);
                entity.Property(e => e.Password).IsRequired().HasMaxLength(255);
                entity.HasIndex(e => e.Email).IsUnique();
                entity.Property(e => e.Role).HasMaxLength(20);
            });


 // Only seed data for in-memory database (local development)
            var isLambda = !string.IsNullOrEmpty(Environment.GetEnvironmentVariable("AWS_LAMBDA_FUNCTION_NAME"));
            if (!isLambda)
            {
                // Sample books
                modelBuilder.Entity<Book>(entity => entity.HasData(
                    new Book { Id = 1, Title = "The Great Gatsby", Author = "F. Scott Fitzgerald" },
                    new Book { Id = 2, Title = "Clean Code", Author = "Robert Martin" }
                ));

                // Sample users (in real apps, hash passwords!)
                modelBuilder.Entity<User>(entity => entity.HasData(
                    new User { Id = 1, Email = "admin@library.com", Password = "admin123", Role = "Admin" },
                    new User { Id = 2, Email = "librarian@library.com", Password = "lib123", Role = "Librarian" },
                    new User { Id = 3, Email = "member@library.com", Password = "mem123", Role = "Member" }
                ));
            }

        }
    }
}
