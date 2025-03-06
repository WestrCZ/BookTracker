using BookTracker.Data.Entities;
using Microsoft.EntityFrameworkCore;

namespace BookTracker.Data;

public class AppDbContext(DbContextOptions options) : DbContext(options)
{
    public DbSet<Book> Books { get; set; } = null!;
}
