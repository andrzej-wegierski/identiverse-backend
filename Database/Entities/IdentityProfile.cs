using Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Database.Entities;

public class IdentityProfile
{
    public int Id { get; init; }
    public int PersonId { get; init; }
    public string DisplayName { get; set; } = string.Empty;
    public IdentityContext Context { get; set; }
    
    public DateTime? BirthDate { get; set; }
    public string? Title { get; set; }
    public string? Email { get; set; }
    public string? Phone { get; set; }
    public string? Address { get; set; }
    
    public bool IsDefaultForContext { get; set; }
    public DateTime CreatedAt { get; init; }
    public DateTime UpdatedAt { get; set; }

    public Person Person { get; init; } = null!; 
}

public class IdentityProfileEntityConfiguration : IEntityTypeConfiguration<IdentityProfile>
{
    public void Configure(EntityTypeBuilder<IdentityProfile> builder)
    {
        builder.HasKey(p => p.Id);
        
        builder.Property(p => p.Id).ValueGeneratedOnAdd();

        builder.Property(p => p.DisplayName)
            .IsRequired()
            .HasMaxLength(200);
        
        builder.Property(p => p.Title)
            .HasMaxLength(50);
        builder.Property(p => p.Email)
            .HasMaxLength(255);
        builder.Property(p => p.Phone)
            .HasMaxLength(50);
        builder.Property(p => p.Address)
            .HasMaxLength(500);
        
        builder.Property(p => p.Context)
            .HasConversion<string>()
            .IsRequired()
            .HasMaxLength(100);
        
        builder.Property(p => p.IsDefaultForContext)
            .HasDefaultValue(false);
        
        builder.Property(p => p.CreatedAt)
            .IsRequired();
        
        builder.Property(p => p.UpdatedAt)
            .IsRequired();
        
        builder.HasOne(p => p.Person)
            .WithMany(p => p.IdentityProfiles)
            .HasForeignKey(p => p.PersonId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(p => new { p.PersonId, p.Context })
            .IsUnique()
            .HasFilter("\"IsDefaultForContext\" = true");
    }
}