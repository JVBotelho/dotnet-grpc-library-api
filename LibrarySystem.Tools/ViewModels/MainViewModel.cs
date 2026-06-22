using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using LibrarySystem.Tools.Messages;
using LibrarySystem.Tools.Services;
using LibrarySystem.Tools.Services.Core;

namespace LibrarySystem.Tools.ViewModels;

public partial class MainViewModel : ObservableObject
{
    private readonly IGraphDataService _graphService;
    private GraphNode? _currentlySelectedNode;
    private readonly Random _random = new();

    [ObservableProperty] private ObservableCollection<GraphNode> _nodes = new();
    [ObservableProperty] private ObservableCollection<GraphEdge> _edges = new();
    [ObservableProperty] private InspectorViewModel _inspector;
    [ObservableProperty] private WafLogViewModel _wafLogs;
    [ObservableProperty] private string _statusMessage = "Ready";

    // Exposed for XAML toast binding (concrete type — ObservableObject).
    public NotificationService Notifications { get; }

    public MainViewModel(
        IGraphDataService graphService,
        InspectorViewModel inspector,
        WafLogViewModel wafLogs,
        NotificationService notifications)
    {
        _graphService = graphService;
        _inspector    = inspector;
        _wafLogs      = wafLogs;
        Notifications = notifications;

        WeakReferenceMessenger.Default.Register<NodeSelectedMessage>(
            this, (_, msg) => SelectNode(msg.Node));
    }

    public void SelectNode(GraphNode node)
    {
        if (_currentlySelectedNode != null)
            _currentlySelectedNode.IsSelected = false;

        node.IsSelected = true;
        _currentlySelectedNode = node;
        Inspector.LoadNode(node);

        foreach (var edge in Edges)
            edge.IsActive = edge.Source == node || edge.Target == node;
    }

    [RelayCommand]
    public async Task LoadGraphData()
    {
        try
        {
            StatusMessage = "Fetching topology from gRPC Core...";
            _currentlySelectedNode = null;
            Inspector.SelectedNode = null;

            var books = await _graphService.GetTopologyAsync();

            Nodes.Clear();
            Edges.Clear();

            var authorNodes = new Dictionary<string, GraphNode>();
            var authorIndex = 0;

            foreach (var book in books)
            {
                if (!authorNodes.TryGetValue(book.Author, out var authorNode))
                {
                    // Negative IDs for synthetic author nodes — never collide with domain book IDs.
                    authorNode = new GraphNode
                    {
                        Id       = -(++authorIndex),
                        Title    = book.Author,
                        Subtitle = "Author",
                        NodeType = "Author",
                        X        = _random.Next(50, 300),
                        Y        = _random.Next(50, 500),
                    };
                    Nodes.Add(authorNode);
                    authorNodes[book.Author] = authorNode;
                }

                var bookNode = new GraphNode
                {
                    Id           = book.Id,
                    Title        = book.Title,
                    Subtitle     = $"{book.Author} · {book.PublicationYear}",
                    NodeType     = "Book",
                    X            = _random.Next(400, 1100),
                    Y            = _random.Next(50, 600),
                    OriginalData = book
                };
                Nodes.Add(bookNode);
                Edges.Add(new GraphEdge(authorNode, bookNode));
            }

            StatusMessage = $"Topology Loaded: {Nodes.Count} Nodes, {Edges.Count} Connections.";
            Notifications.ShowInfo(
                "Topology refreshed",
                $"{Nodes.Count} nodes · {Edges.Count} connections.");
        }
        catch (Exception ex)
        {
            StatusMessage = "Connection Error. Is Docker running? (port 5001)";
            Notifications.ShowError(
                "Connection failed",
                $"Is Docker running? ({ex.Message})");
        }
    }
}
