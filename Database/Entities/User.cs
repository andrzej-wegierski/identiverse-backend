using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Database.Entities;

public class User
{
    public int Id { get; init; }
    public string Username { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    
    public string PasswordHash { get; set; } = string.Empty;
    public string PasswordSalt { get; set; } = string.Empty;
    
    public string Role { get; set; } = "User";
    
    public int? PersonId { get; set; }
    public Person? Person { get; set; }
    
    public DateTime CreatedAt { get; init; }
    public DateTime UpdatedAt { get; set; }
    public DateTime? LastLoginAt { get; set; }
}

public class UserEntityConfiguration : IEntityTypeConfiguration<User>
{
    public void Configure(EntityTypeBuilder<User> builder)
    {
        builder.HasKey(u => u.Id);

        builder.Property(u => u.Username).IsRequired().HasMaxLength(100);
        builder.Property(u => u.Email).IsRequired().HasMaxLength(200);
        builder.Property(u => u.PasswordHash).IsRequired();
        builder.Property(u => u.PasswordSalt).IsRequired();
        builder.Property(u => u.Role).IsRequired().HasMaxLength(20);
        builder.Property(u => u.CreatedAt).IsRequired();
        builder.Property(u => u.UpdatedAt).IsRequired();
        
        builder.HasIndex(u => u.Username).IsUnique();
        builder.HasIndex(u => u.Email).IsUnique();
        builder.HasIndex(u => u.Role);
        
        builder.HasOne(u => u.Person)
            .WithOne(p => p.User)
            .HasForeignKey<User>(u => u.PersonId)
            .OnDelete(DeleteBehavior.SetNull);
        
        builder.HasIndex(u => u.PersonId).IsUnique();
    }
}