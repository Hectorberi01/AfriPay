using AfriPay.Domain.Employees;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AfriPay.Infrastructure.Persistence.Configurations;

public sealed class EmployeeConfiguration : IEntityTypeConfiguration<Employee>
{
    public void Configure(EntityTypeBuilder<Employee> b)
    {
        b.ToTable("employees");
        b.HasKey(e => e.Id);
 
        b.Property(e => e.MerchantId).HasColumnName("merchant_id").IsRequired();
        b.Property(e => e.Name).HasColumnName("name").HasMaxLength(100).IsRequired();
        b.Property(e => e.Email).HasColumnName("email").HasMaxLength(255).IsRequired();
 
        b.Property(e => e.PasswordHash)
            .HasColumnName("password_hash").HasMaxLength(100);
 
        b.Property(e => e.Role)
            .HasColumnName("role")
            .HasConversion<string>()
            .HasMaxLength(20)
            .IsRequired();
 
        b.Property(e => e.Status)
            .HasColumnName("status")
            .HasConversion<string>()
            .HasMaxLength(15)
            .IsRequired();
 
        b.Property(e => e.RefreshToken)
            .HasColumnName("refresh_token").HasMaxLength(200);
 
        b.Property(e => e.RefreshTokenExpiresAt)
            .HasColumnName("refresh_token_expires_at");
 
        b.Property(e => e.CreatedAt).HasColumnName("created_at").IsRequired();
        b.Property(e => e.UpdatedAt).HasColumnName("updated_at").IsRequired();
        b.Property(e => e.LastLoginAt).HasColumnName("last_login_at");
 
        // Email unique par marchand
        b.HasIndex(e => new { e.MerchantId, e.Email })
            .IsUnique()
            .HasDatabaseName("idx_employees_merchant_email");
 
        // Global email index pour le login
        b.HasIndex(e => e.Email)
            .HasDatabaseName("idx_employees_email");
 
        b.HasIndex(e => e.RefreshToken)
            .HasFilter("refresh_token IS NOT NULL")
            .HasDatabaseName("idx_employees_refresh_token");
    }
}