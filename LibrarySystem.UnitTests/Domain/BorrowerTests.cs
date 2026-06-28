using FluentAssertions;
using LibrarySystem.Domain.Entities;

namespace LibrarySystem.Domain.UnitTests;

public class BorrowerTests
{
    [Fact]
    public void Constructor_WithValidData_ShouldCreateBorrower()
    {
        var borrower = new Borrower("John Doe", "john@example.com");

        borrower.Name.Should().Be("John Doe");
        borrower.Email.Should().Be("john@example.com");
        borrower.LendingActivities.Should().BeEmpty();
    }

    [Theory]
    [InlineData("", "email@example.com")]
    [InlineData("Name", "")]
    [InlineData(null, "email@example.com")]
    [InlineData("Name", null)]
    public void Constructor_WithInvalidData_ShouldThrowArgumentException(string? name, string? email)
    {
        Action act = () => new Borrower(name!, email!);

        act.Should().Throw<ArgumentException>();
    }
}
