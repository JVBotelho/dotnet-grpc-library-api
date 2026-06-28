using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using LibrarySystem.Contracts.Protos;
using LibrarySystem.Tools.Services;

namespace LibrarySystem.Tools.ViewModels;

public partial class InspectorViewModel : ObservableObject, IDisposable
{
    private readonly IGraphDataService _graphService;
    private readonly INotificationService _notifications;
    private readonly CancellationTokenSource _telemetryCts = new();

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsBookNode))]
    private GraphNode? _selectedNode;

    [ObservableProperty] private string _editTitle  = string.Empty;
    [ObservableProperty] private string _editAuthor = string.Empty;
    [ObservableProperty] private string _editYear   = string.Empty;

    [ObservableProperty] private double _beltMotorTempC;
    [ObservableProperty] private double _scannerRpm;
    [ObservableProperty] private bool _safetyDoorClosed;
    [ObservableProperty] private uint _faultFlags;

    // Used by XAML to disable the Author field for Author-type nodes.
    public bool IsBookNode => SelectedNode?.NodeType == "Book";

    public InspectorViewModel(IGraphDataService graphService, INotificationService notifications, ITelemetryService telemetryService)
    {
        _graphService  = graphService;
        _notifications = notifications;
        
        // Start background telemetry watcher
        _ = WatchTelemetryAsync(telemetryService, _telemetryCts.Token);
    }

    private async Task WatchTelemetryAsync(ITelemetryService telemetryService, CancellationToken ct)
    {
        try
        {
            await foreach (var frame in telemetryService.WatchDeviceFramesAsync(null, ct))
            {
                // Dispatch to UI thread
                System.Windows.Application.Current.Dispatcher.Invoke(() =>
                {
                    BeltMotorTempC = frame.BeltMotorTempC;
                    ScannerRpm = frame.ScannerRpm;
                    SafetyDoorClosed = frame.SafetyDoorClosed;
                    FaultFlags = frame.FaultFlags;
                });
            }
        }
        catch (OperationCanceledException)
        {
            // normal shutdown
        }
        catch (Exception ex)
        {
            _notifications.ShowError("Telemetry disconnected", ex.Message);
        }
    }

    public void Dispose() => _telemetryCts.Cancel();

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
            if (!int.TryParse(EditYear, out var year) || year < 1)
            {
                _notifications.ShowError("Validation failed", "Publication year must be a positive number.");
                return;
            }

            var request = new UpdateBookRequest
            {
                Id              = SelectedNode.Id,
                Title           = EditTitle,
                Author          = EditAuthor,
                PublicationYear = year,
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
