using AfriPay.Domain.Staff;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AfriPay.Infrastructure.Persistence.Configurations;

public sealed class StaffMemberConfiguration : IEntityTypeConfiguration<StaffMember>
{
    public void Configure(EntityTypeBuilder<StaffMember> b)
    {
        b.ToTable("staff_members");
        b.HasKey(s => s.Id);
 
        b.Property(s => s.Name).HasColumnName("name").HasMaxLength(100).IsRequired();
        b.Property(s => s.Email).HasColumnName("email").HasMaxLength(255).IsRequired();
        b.Property(s => s.PasswordHash).HasColumnName("password_hash").HasMaxLength(100);
        b.Property(s => s.Role).HasColumnName("role")
            .HasConversion<string>().HasMaxLength(20).IsRequired();
        b.Property(s => s.Status).HasColumnName("status")
            .HasConversion<string>().HasMaxLength(15).IsRequired();
        b.Property(s => s.Department).HasColumnName("department").HasMaxLength(100);
        b.Property(s => s.RefreshToken).HasColumnName("refresh_token").HasMaxLength(200);
        b.Property(s => s.RefreshTokenExpiresAt).HasColumnName("refresh_token_expires_at");
        b.Property(s => s.CreatedAt).HasColumnName("created_at").IsRequired();
        b.Property(s => s.UpdatedAt).HasColumnName("updated_at").IsRequired();
        b.Property(s => s.LastLoginAt).HasColumnName("last_login_at");
        b.Property(s => s.CreatedBy).HasColumnName("created_by");
 
        b.HasIndex(s => s.Email).IsUnique().HasDatabaseName("idx_staff_email");
        b.HasIndex(s => s.RefreshToken)
            .HasFilter("refresh_token IS NOT NULL")
            .HasDatabaseName("idx_staff_refresh_token");
    }
}
