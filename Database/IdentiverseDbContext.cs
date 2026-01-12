using Database.Entities;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace Database;

public class IdentiverseDbContext : IdentityDbContext<ApplicationUser, IdentityRole<int>, int>
{
    public IdentiverseDbContext(DbContextOptions<IdentiverseDbContext> options) : base(options)
    {
        
    }

    public DbSet<Person> Persons { get; set; } = null!;
    public DbSet<IdentityProfile> IdentityProfiles { get; set; } = null!;

    protected override void OnModelCreating(ModelBuilder model)
    {
        base.OnModelCreating(model);
        
        foreach (var entityType in model.Model.GetEntityTypes())
        {
            var properties = entityType.GetProperties()
                .Where(p => p.ClrType == typeof(DateTime) || p.ClrType == typeof(DateTime?));

            foreach (var property in properties)
            {
                property.SetValueConverter(new Microsoft.EntityFrameworkCore.Storage.ValueConversion.ValueConverter<DateTime, DateTime>(
                    v => v.Kind == DateTimeKind.Unspecified ? DateTime.SpecifyKind(v, DateTimeKind.Utc) : v.ToUniversalTime(),
                    v => DateTime.SpecifyKind(v, DateTimeKind.Utc)));
            }
        }
        
        model.ApplyConfiguration(new PersonEntityConfiguration());
        model.ApplyConfiguration(new IdentityProfileEntityConfiguration());
        
        model.Entity<ApplicationUser>(b =>
        {
            b.HasOne(u => u.Person)
                .WithOne()
                .HasForeignKey<ApplicationUser>(u => u.PersonId)
                .OnDelete(DeleteBehavior.SetNull);
            
            b.HasIndex(u => u.PersonId)
                .IsUnique();
        });
    }
}