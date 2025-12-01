using Database.Entities;
using Microsoft.EntityFrameworkCore;

namespace Database;

public class IdentiverseDbContext : DbContext
{
    public IdentiverseDbContext(DbContextOptions<IdentiverseDbContext> options) : base(options)
    {
        
    }

    public DbSet<Person> Persons { get; set; } = null!;

    protected override void OnModelCreating(ModelBuilder model)
    {
        base.OnModelCreating(model);
        model.ApplyConfiguration(new PersonEntityConfiguration());
    }
}