using LibrarySystem.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace LibrarySystem.Persistence.Configurations;

internal sealed class LendingActivityConfiguration : IEntityTypeConfiguration<LendingActivity>
{
    public void Configure(EntityTypeBuilder<LendingActivity> builder)
    {
        builder.HasKey(l => l.Id);

        builder.Property(l => l.BorrowedDate)
            .IsRequired();
            
    }
}