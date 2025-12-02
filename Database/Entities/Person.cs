using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Database.Entities;

public class Person
{
    public int Id { get; set; }
    public Guid ExternalId { get; init; }
    public string FirstName { get; set; } = null!;
    public string LastName { get; set; } = null!;
    public string? PreferredName { get; set; }
    public DateTime CreatedAt { get; init; }
    public DateTime UpdatedAt { get; set; }
}

public class PersonEntityConfiguration : IEntityTypeConfiguration<Person>
{
    public void Configure(EntityTypeBuilder<Person> builder)
    {
        builder.HasKey(p => p.Id);
        builder.Property(p => p.Id)
            .ValueGeneratedOnAdd();
        
        builder.Property(p => p.ExternalId)
            .IsRequired();
        builder.HasIndex(p => p.ExternalId)
            .IsUnique();

        builder.Property(p => p.FirstName)
            .IsRequired()
            .HasMaxLength(100);
        
        builder.Property(p => p.LastName)
            .IsRequired()
            .HasMaxLength(100);
        
        builder.Property(p => p.PreferredName)
            .HasMaxLength(100);
        
        builder.Property(p => p.CreatedAt)
            .IsRequired()
            .HasColumnType("timestamp without time zone")
            .HasDefaultValueSql("(now() at time zone 'utc')");
        
        builder.Property(p => p.UpdatedAt)
            .IsRequired()
            .HasColumnType("timestamp without time zone")
            .HasDefaultValueSql("(now() at time zone 'utc')");
    }
}