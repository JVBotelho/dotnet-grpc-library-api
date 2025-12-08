using LibrarySystem.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace LibrarySystem.Persistence.Configurations;

internal sealed class BorrowerConfiguration : IEntityTypeConfiguration<Borrower>
{
    public void Configure(EntityTypeBuilder<Borrower> builder)
    {
        builder.HasKey(b => b.Id);

        builder.Property(b => b.Name)
            .IsRequired()
            .HasMaxLength(150);

        builder.Property(b => b.Email)
            .IsRequired()
            .HasMaxLength(150);

        // Unique Index for Email (Shift Left: Database constraints enforce integrity)
        builder.HasIndex(b => b.Email).IsUnique();

        builder.Metadata.FindNavigation(nameof(Borrower.LendingActivities))!
            .SetPropertyAccessMode(PropertyAccessMode.Field);
    }
}