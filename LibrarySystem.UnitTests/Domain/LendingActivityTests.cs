using FluentAssertions;
using LibrarySystem.Domain.Entities;

namespace LibrarySystem.Domain.UnitTests;

public class LendingActivityTests
{
    [Fact]
    public void MarkAsReturned_WhenNotReturned_ShouldSetReturnedDate()
    {
        var book = new Book("Title", "Author", 2000, 100, 1);
        var borrower = new Borrower("User", "email");
        var lending = book.BorrowCopy(borrower);

        lending.MarkAsReturned();

        lending.ReturnedDate.Should().NotBeNull();
        lending.ReturnedDate.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(1));
    }

    [Fact]
    public void MarkAsReturned_WhenAlreadyReturned_ShouldThrowInvalidOperationException()
    {
        var book = new Book("Title", "Author", 2000, 100, 1);
        var borrower = new Borrower("User", "email");
        var lending = book.BorrowCopy(borrower);
        lending.MarkAsReturned();

        Action act = () => lending.MarkAsReturned();

        act.Should().Throw<InvalidOperationException>()
            .WithMessage("Book is already returned.");
    }
}
