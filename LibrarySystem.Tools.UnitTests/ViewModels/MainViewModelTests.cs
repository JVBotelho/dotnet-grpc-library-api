using FluentAssertions;
using LibrarySystem.Contracts.Protos;
using LibrarySystem.Tools.Services;
using LibrarySystem.Tools.Services.Core;
using LibrarySystem.Tools.ViewModels;
using Moq;

namespace LibrarySystem.Tools.UnitTests.ViewModels;

public class MainViewModelTests
{
    private static MainViewModel CreateSut(IGraphDataService? graphService = null, ILogTailerService? logService = null)
    {
        var graph         = graphService ?? Mock.Of<IGraphDataService>();
        var log           = logService   ?? Mock.Of<ILogTailerService>();
        var notifications = new NotificationService();
        var inspector     = new InspectorViewModel(graph, notifications);
        var wafLogs       = new WafLogViewModel(log);
        return new MainViewModel(graph, inspector, wafLogs, notifications);
    }

    [Fact]
    public async Task LoadGraphData_Success_PopulatesNodesAndEdges()
    {
        var books = new List<BookResponse>
        {
            new() { Id = 1, Title = "The Hobbit",              Author = "Tolkien", PublicationYear = 1937 },
            new() { Id = 2, Title = "The Lord of the Rings",   Author = "Tolkien", PublicationYear = 1954 },
            new() { Id = 3, Title = "Dune",                    Author = "Herbert", PublicationYear = 1965 },
        };
        var graphMock = new Mock<IGraphDataService>();
        graphMock.Setup(x => x.GetTopologyAsync(It.IsAny<CancellationToken>()))
                 .ReturnsAsync(books);

        var sut = CreateSut(graphMock.Object);
        await sut.LoadGraphDataCommand.ExecuteAsync(null);

        // 2 unique authors + 3 books = 5 nodes; 3 edges.
        sut.Nodes.Should().HaveCount(5);
        sut.Edges.Should().HaveCount(3);
        sut.StatusMessage.Should().Contain("Nodes");
    }

    [Fact]
    public async Task LoadGraphData_Failure_ShowsErrorStatus()
    {
        var graphMock = new Mock<IGraphDataService>();
        graphMock.Setup(x => x.GetTopologyAsync(It.IsAny<CancellationToken>()))
                 .ThrowsAsync(new InvalidOperationException("Server down"));

        var sut = CreateSut(graphMock.Object);
        await sut.LoadGraphDataCommand.ExecuteAsync(null);

        sut.StatusMessage.Should().Contain("Error");
        sut.Nodes.Should().BeEmpty();
        sut.Edges.Should().BeEmpty();
    }

    [Fact]
    public void SelectNode_UpdatesInspectorSelectedNode()
    {
        var sut  = CreateSut();
        var node = new GraphNode { Id = 42, NodeType = "Book", Title = "Dune" };
        sut.Nodes.Add(node);

        sut.SelectNode(node);

        sut.Inspector.SelectedNode.Should().BeSameAs(node);
        node.IsSelected.Should().BeTrue();
    }

    [Fact]
    public void SelectNode_DeselectsPreviousNode()
    {
        var sut   = CreateSut();
        var first = new GraphNode { Id = 1, NodeType = "Book", Title = "Dune" };
        var second = new GraphNode { Id = 2, NodeType = "Book", Title = "Foundation" };
        sut.Nodes.Add(first); sut.Nodes.Add(second);

        sut.SelectNode(first);
        sut.SelectNode(second);

        first.IsSelected.Should().BeFalse();
        second.IsSelected.Should().BeTrue();
    }

    [Fact]
    public void SelectNode_HighlightsEdgesConnectedToNode()
    {
        var sut    = CreateSut();
        var author = new GraphNode { Id = 1, NodeType = "Author", Title = "Tolkien" };
        var book1  = new GraphNode { Id = 2, NodeType = "Book",   Title = "The Hobbit" };
        var book2  = new GraphNode { Id = 3, NodeType = "Book",   Title = "Silmarillion" };
        sut.Nodes.Add(author); sut.Nodes.Add(book1); sut.Nodes.Add(book2);
        var edge1 = new GraphEdge(author, book1);
        var edge2 = new GraphEdge(author, book2);
        sut.Edges.Add(edge1); sut.Edges.Add(edge2);

        sut.SelectNode(author);

        edge1.IsActive.Should().BeTrue();
        edge2.IsActive.Should().BeTrue();
    }

    [Fact]
    public void SelectNode_DeactivatesEdgesNotConnectedToNode()
    {
        var sut     = CreateSut();
        var author1 = new GraphNode { Id = 1, NodeType = "Author", Title = "Tolkien" };
        var author2 = new GraphNode { Id = 2, NodeType = "Author", Title = "Herbert" };
        var book    = new GraphNode { Id = 3, NodeType = "Book",   Title = "Dune" };
        sut.Nodes.Add(author1); sut.Nodes.Add(author2); sut.Nodes.Add(book);
        var edgeA1 = new GraphEdge(author1, book);
        var edgeA2 = new GraphEdge(author2, book);
        sut.Edges.Add(edgeA1); sut.Edges.Add(edgeA2);

        sut.SelectNode(author1);

        edgeA1.IsActive.Should().BeTrue();
        edgeA2.IsActive.Should().BeFalse();
    }
}
