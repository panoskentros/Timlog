using Microsoft.EntityFrameworkCore;
using Timlog.Domain.Entities;
using Timlog.Domain.ValueObjects;

namespace Timlog.Infrastructure.Data;

public class TimlogDbContext : DbContext
{
    public DbSet<Invoice> Invoices => Set<Invoice>();
    public DbSet<TenantCredential> TenantCredentials => Set<TenantCredential>();
    public TimlogDbContext(DbContextOptions<TimlogDbContext> options) : base(options)
    {
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        
        modelBuilder.Entity<TenantCredential>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => e.TelegramChatId).IsUnique();
            entity.HasIndex(e => e.IssuerAfm);
        });
        
        modelBuilder.Entity<Invoice>(entity =>
        {
            entity.HasKey(e => e.Id);
    
            entity.Property(e => e.IssuerAfm)
                .HasConversion(
                    afm => afm.Value,
                    value => Afm.Create(value))
                .HasMaxLength(20)
                .IsRequired();

            entity.Property(e => e.ClientAfm)
                .HasConversion(
                    afm => afm.Value,
                    value => Afm.Create(value))
                .HasMaxLength(20)
                .IsRequired();
            
            entity.Property(e => e.AadeMark).HasMaxLength(20);

            entity.HasIndex(e => e.IssuerAfm);
            entity.HasIndex(e => e.ClientAfm);
            entity.HasIndex(e => e.IssueDate);
            entity.HasIndex(e => e.AadeMark).IsUnique();
        });
    }
}