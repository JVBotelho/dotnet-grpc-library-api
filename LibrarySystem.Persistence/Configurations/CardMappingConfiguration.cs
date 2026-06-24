using LibrarySystem.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace LibrarySystem.Persistence.Configurations;

public class CardMappingConfiguration : IEntityTypeConfiguration<CardMapping>
{
    public void Configure(EntityTypeBuilder<CardMapping> builder)
    {
        builder.HasKey(c => c.CardUid);

        builder.Property(c => c.CardUid)
            .IsRequired()
            .HasMaxLength(64);

        builder.HasOne(c => c.Borrower)
            .WithMany()
            .HasForeignKey(c => c.BorrowerId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
