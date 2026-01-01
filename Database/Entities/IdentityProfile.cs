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
    public string? Language { get; set; }
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
        
        builder.Property(p => p.Context)
            .HasConversion<string>()
            .IsRequired()
            .HasMaxLength(100);
        
        builder.Property(p => p.Language)
            .HasMaxLength(10);
        
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

        builder.HasIndex(p => new { p.PersonId, p.Context, p.Language, p.IsDefaultForContext });
    }
}