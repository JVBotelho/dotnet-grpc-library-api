using FluentAssertions;
using LibrarySystem.Domain.Entities;

namespace LibrarySystem.Domain.UnitTests;

public class BookTests
{
    [Fact]
    public void BorrowCopy_WhenCopiesAvailable_ShouldAddLendingActivity()
    {
        var book = new Book("Title", "Author", 2000, 100, 1); // 1 Copy
        var borrower = new Borrower("User", "email");
        var lending = book.BorrowCopy(borrower);
        lending.Should().NotBeNull();
        book.LendingActivities.Should().Contain(lending);
        lending.BorrowedDate.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(1));
    }

    [Fact]
    public void BorrowCopy_WhenNoCopiesAvailable_ShouldThrowInvalidOperationException()
    {
        var book = new Book("Title", "Author", 2000, 100, 1); // 1 Total Copy
        var borrower1 = new Borrower("User1", "email1");
        var borrower2 = new Borrower("User2", "email2");
        book.BorrowCopy(borrower1);
        Action act = () => book.BorrowCopy(borrower2);

        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*No copies available*");
    }

    [Fact]
    public void ReturnCopy_WhenLendingExists_ShouldMarkAsReturned()
    {
        var book = new Book("Title", "Author", 2000, 100, 1);
        var borrower = new Borrower("User", "email");
        var lending = book.BorrowCopy(borrower);

        book.ReturnCopy(lending.Id);

        lending.ReturnedDate.Should().NotBeNull();
    }

    [Fact]
    public void ReturnCopy_WhenLendingNotFound_ShouldThrowArgumentException()
    {
        var book = new Book("Title", "Author", 2000, 100, 1);
        
        Action act = () => book.ReturnCopy(999);

        act.Should().Throw<ArgumentException>()
            .WithMessage("*Lending record not found*");
    }

    [Fact]
    public void UpdateDetails_WithValidData_ShouldUpdateProperties()
    {
        var book = new Book("Title", "Author", 2000, 100, 1);

        book.UpdateDetails("New Title", "New Author", 2024, 200, 5);

        book.Title.Should().Be("New Title");
        book.Author.Should().Be("New Author");
        book.PublicationYear.Should().Be(2024);
        book.Pages.Should().Be(200);
        book.TotalCopies.Should().Be(5);
    }

    [Theory]
    [InlineData("", "Author", 2000, 100, 1)]
    [InlineData("Title", "", 2000, 100, 1)]
    [InlineData("Title", "Author", 0, 100, 1)]
    [InlineData("Title", "Author", 2000, 0, 1)]
    [InlineData("Title", "Author", 2000, 100, -1)]
    public void Constructor_WithInvalidData_ShouldThrowArgumentException(string title, string author, int year, int pages, int totalCopies)
    {
        Action act = () => new Book(title, author, year, pages, totalCopies);

        act.Should().Throw<ArgumentException>();
    }
}

