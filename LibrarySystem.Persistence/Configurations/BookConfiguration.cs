using LibrarySystem.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace LibrarySystem.Persistence.Configurations;

internal sealed class BookConfiguration : IEntityTypeConfiguration<Book>
{
    public void Configure(EntityTypeBuilder<Book> builder)
    {
        builder.HasKey(b => b.Id);

        builder.Property(b => b.Title)
            .IsRequired()
            .HasMaxLength(200);

        builder.Property(b => b.Author)
            .IsRequired()
            .HasMaxLength(100);

        // DDD Magic: Maps the private backing field for the collection
        builder.Metadata.FindNavigation(nameof(Book.LendingActivities))!
            .SetPropertyAccessMode(PropertyAccessMode.Field);

        // Relationships
        builder.HasMany(b => b.LendingActivities)
            .WithOne(l => l.Book)
            .HasForeignKey(l => l.BookId)
            .OnDelete(DeleteBehavior.Restrict); // Shift Left: Prevent accidental data loss
    }
}