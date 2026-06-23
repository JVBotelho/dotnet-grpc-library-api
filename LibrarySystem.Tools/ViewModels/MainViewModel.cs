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
    private void DismissToast() => Notifications.Dismiss();

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

            // Group books by author, preserving first-seen insertion order
            var authorOrder   = new List<string>();
            var booksByAuthor = new Dictionary<string, List<int>>();
            for (int i = 0; i < books.Count; i++)
            {
                var author = books[i].Author;
                if (!booksByAuthor.ContainsKey(author))
                {
                    authorOrder.Add(author);
                    booksByAuthor[author] = new List<int>();
                }
                booksByAuthor[author].Add(i);
            }

            // Hierarchical deterministic layout
            const int AuthorX      = 80;
            const int AuthorYStart  = 60;
            const int AuthorYStep   = 130;
            const int BookXBase     = 380;
            const int BookXStep     = 240;
            const int BookYStep     = 120;

            var authorNodes = new Dictionary<string, GraphNode>();
            int syntheticId = 0;
            int authorIdx   = 0;
            int nextBookY   = AuthorYStart;

            foreach (var authorName in authorOrder)
            {
                int authorY = Math.Clamp(AuthorYStart + authorIdx * AuthorYStep, 20, 2800);

                // Negative IDs for synthetic author nodes — never collide with domain book IDs.
                var authorNode = new GraphNode
                {
                    Id       = -(++syntheticId),
                    Title    = authorName,
                    Subtitle = "Author",
                    NodeType = "Author",
                    X        = AuthorX,
                    Y        = authorY,
                };
                Nodes.Add(authorNode);
                authorNodes[authorName] = authorNode;

                int bookColIdx = 0;
                int bookX      = BookXBase;
                int bookY      = Math.Max(authorY, nextBookY);

                foreach (var idx in booksByAuthor[authorName])
                {
                    // Snake to the next column when the canvas bottom is exceeded
                    if (bookY > 2800)
                    {
                        bookColIdx++;
                        bookX = BookXBase + bookColIdx * BookXStep;
                        bookY = authorY;
                    }

                    var book = books[idx];
                    var bookNode = new GraphNode
                    {
                        Id           = book.Id,
                        Title        = book.Title,
                        Subtitle     = $"{book.Author} · {book.PublicationYear}",
                        NodeType     = "Book",
                        X            = Math.Clamp(bookX, 20, 2800),
                        Y            = Math.Clamp(bookY, 20, 2800),
                        OriginalData = book
                    };
                    Nodes.Add(bookNode);
                    Edges.Add(new GraphEdge(authorNode, bookNode));

                    bookY += BookYStep;
                }

                nextBookY = bookY;
                authorIdx++;
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
