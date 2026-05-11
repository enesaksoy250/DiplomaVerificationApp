using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace DiplomaVerificationApp.Data;

public sealed class ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
    : IdentityDbContext<ApplicationUser>(options)
{
    public DbSet<University> Universities => Set<University>();

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        builder.Entity<University>(entity =>
        {
            entity.HasKey(university => university.Id);
            entity.Property(university => university.Name).HasMaxLength(200).IsRequired();
            entity.HasIndex(university => university.Name).IsUnique();
        });

        builder.Entity<ApplicationUser>(entity =>
        {
            entity.Property(user => user.UniversityId).HasMaxLength(64);
            entity.Property(user => user.StudentIdentifier).HasMaxLength(128);
        });
    }
}
