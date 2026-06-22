using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using LibrarySystem.Contracts.Protos;
using LibrarySystem.Tools.Services;

namespace LibrarySystem.Tools.ViewModels;

public partial class InspectorViewModel : ObservableObject
{
    private readonly IGraphDataService _graphService;
    private readonly INotificationService _notifications;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsBookNode))]
    private GraphNode? _selectedNode;

    [ObservableProperty] private string _editTitle  = string.Empty;
    [ObservableProperty] private string _editAuthor = string.Empty;
    [ObservableProperty] private string _editYear   = string.Empty;

    // Used by XAML to disable the Author field for Author-type nodes.
    public bool IsBookNode => SelectedNode?.NodeType == "Book";

    public InspectorViewModel(IGraphDataService graphService, INotificationService notifications)
    {
        _graphService  = graphService;
        _notifications = notifications;
    }

    public void LoadNode(GraphNode node)
    {
        SelectedNode = node;

        if (node.OriginalData != null)
        {
            EditTitle  = node.OriginalData.Title;
            EditAuthor = node.OriginalData.Author;
            EditYear   = node.OriginalData.PublicationYear.ToString();
        }
        else
        {
            EditTitle  = node.Title;
            EditAuthor = string.Empty;
            EditYear   = string.Empty;
        }
    }

    [RelayCommand]
    public async Task SaveChanges()
    {
        if (SelectedNode?.OriginalData == null) return;

        try
        {
            var request = new UpdateBookRequest
            {
                Id              = SelectedNode.Id,
                Title           = EditTitle,
                Author          = EditAuthor,
                PublicationYear = int.TryParse(EditYear, out var y) ? y : 0,
                Pages           = SelectedNode.OriginalData.Pages,
                TotalCopies     = SelectedNode.OriginalData.TotalCopies
            };

            var response = await _graphService.UpdateBookAsync(request);

            SelectedNode.Title        = response.Title;
            SelectedNode.Subtitle     = $"{response.Author} · {response.PublicationYear}";
            SelectedNode.OriginalData = response;

            _notifications.ShowSuccess(
                "Changes saved",
                $"Book {SelectedNode.Id} persisted via gRPC → PostgreSQL.");
        }
        catch (Exception ex)
        {
            _notifications.ShowError("Update failed", ex.Message);
        }
    }
}
