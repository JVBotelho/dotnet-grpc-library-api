using FluentAssertions;
using LibrarySystem.Contracts.Protos;
using LibrarySystem.Tools.Services;
using LibrarySystem.Tools.ViewModels;
using Moq;

namespace LibrarySystem.Tools.UnitTests.ViewModels;

public class InspectorViewModelTests
{
    private static InspectorViewModel CreateSut(
        IGraphDataService? graphService = null,
        INotificationService? notifications = null,
        Mock<ITelemetryService>? telemetryMock = null)
    {
        if (telemetryMock == null)
        {
            telemetryMock = new Mock<ITelemetryService>();
            telemetryMock.Setup(x => x.WatchDeviceFramesAsync(It.IsAny<string?>(), It.IsAny<CancellationToken>()))
                         .Returns(AsyncEnumerable.Empty<LibrarySystem.Contracts.Protos.DeviceFrame>());
        }

        return new InspectorViewModel(
            graphService ?? Mock.Of<IGraphDataService>(),
            notifications ?? Mock.Of<INotificationService>(),
            telemetryMock.Object);
    }

    [Fact]
    public void LoadNode_WithBook_PopulatesAllEditFields()
    {
        var sut  = CreateSut();
        var node = new GraphNode
        {
            Id = 42, NodeType = "Book", Title = "Dune",
            OriginalData = new BookResponse { Id = 42, Title = "Dune", Author = "Herbert", PublicationYear = 1965 }
        };

        sut.LoadNode(node);

        sut.EditTitle.Should().Be("Dune");
        sut.EditAuthor.Should().Be("Herbert");
        sut.EditYear.Should().Be("1965");
        sut.IsBookNode.Should().BeTrue();
        sut.SelectedNode.Should().BeSameAs(node);
    }

    [Fact]
    public void LoadNode_WithAuthor_SetsOnlyTitle()
    {
        var sut  = CreateSut();
        var node = new GraphNode { Id = 99, NodeType = "Author", Title = "Tolkien" };

        sut.LoadNode(node);

        sut.EditTitle.Should().Be("Tolkien");
        sut.EditAuthor.Should().BeEmpty();
        sut.EditYear.Should().BeEmpty();
        sut.IsBookNode.Should().BeFalse();
    }

    [Fact]
    public async Task SaveChanges_Success_ShowsSuccessNotification()
    {
        var updated = new BookResponse { Id = 1, Title = "Dune Messiah", Author = "Herbert", PublicationYear = 1969 };
        var graphMock = new Mock<IGraphDataService>();
        graphMock.Setup(x => x.UpdateBookAsync(It.IsAny<UpdateBookRequest>(), It.IsAny<CancellationToken>()))
                 .ReturnsAsync(updated);
        var notifMock = new Mock<INotificationService>();

        var sut  = CreateSut(graphMock.Object, notifMock.Object);
        var node = new GraphNode
        {
            Id = 1, NodeType = "Book", Title = "Dune",
            OriginalData = new BookResponse { Id = 1, Title = "Dune", Author = "Herbert", PublicationYear = 1965 }
        };
        sut.LoadNode(node);
        sut.EditTitle = "Dune Messiah";

        await sut.SaveChangesCommand.ExecuteAsync(null);

        notifMock.Verify(x => x.ShowSuccess(It.IsAny<string>(), It.IsAny<string>()), Times.Once);
        node.Title.Should().Be("Dune Messiah");
    }

    [Fact]
    public async Task SaveChanges_Failure_ShowsErrorNotification()
    {
        var graphMock = new Mock<IGraphDataService>();
        graphMock.Setup(x => x.UpdateBookAsync(It.IsAny<UpdateBookRequest>(), It.IsAny<CancellationToken>()))
                 .ThrowsAsync(new Exception("Network error"));
        var notifMock = new Mock<INotificationService>();

        var sut  = CreateSut(graphMock.Object, notifMock.Object);
        var node = new GraphNode
        {
            Id = 1, NodeType = "Book", Title = "Dune",
            OriginalData = new BookResponse { Id = 1, Title = "Dune", Author = "Herbert", PublicationYear = 1965 }
        };
        sut.LoadNode(node);

        await sut.SaveChangesCommand.ExecuteAsync(null);

        notifMock.Verify(x => x.ShowError(It.IsAny<string>(), It.IsAny<string>()), Times.Once);
    }

    [Fact]
    public async Task SaveChanges_WithNoSelectedNode_DoesNotCallService()
    {
        var graphMock = new Mock<IGraphDataService>();
        var sut = CreateSut(graphMock.Object);

        await sut.SaveChangesCommand.ExecuteAsync(null);

        graphMock.Verify(
            x => x.UpdateBookAsync(It.IsAny<UpdateBookRequest>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task SaveChanges_WithAuthorNode_DoesNotCallService()
    {
        var graphMock = new Mock<IGraphDataService>();
        var sut  = CreateSut(graphMock.Object);
        var node = new GraphNode { Id = 99, NodeType = "Author", Title = "Tolkien" };
        sut.LoadNode(node); // OriginalData is null for Author nodes

        await sut.SaveChangesCommand.ExecuteAsync(null);

        graphMock.Verify(
            x => x.UpdateBookAsync(It.IsAny<UpdateBookRequest>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }
}
