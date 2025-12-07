using FluentAssertions;
using LibrarySystem.Domain.Entities;

namespace LibrarySystem.Domain.UnitTests;

public class BookTests
{
    [Fact]
    public void BorrowCopy_WhenCopiesAvailable_ShouldAddLendingActivity()
    {
        // Arrange
        var book = new Book("Title", "Author", 2000, 100, 1); // 1 Cópia
        var borrower = new Borrower("User", "email");

        // Act
        var lending = book.BorrowCopy(borrower);

        // Assert
        lending.Should().NotBeNull();
        book.LendingActivities.Should().Contain(lending);
        lending.BorrowedDate.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(1));
    }

    [Fact]
    public void BorrowCopy_WhenNoCopiesAvailable_ShouldThrowInvalidOperationException()
    {
        // Arrange
        var book = new Book("Title", "Author", 2000, 100, 1); // 1 Cópia Total
        var borrower1 = new Borrower("User1", "email1");
        var borrower2 = new Borrower("User2", "email2");

        // Act 
        book.BorrowCopy(borrower1);

        // Assert
        Action act = () => book.BorrowCopy(borrower2);

        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*No copies available*");
    }
}