using System.Collections.ObjectModel;
using System.Windows.Data;
using CommunityToolkit.Mvvm.ComponentModel;
using Grpc.Core;
using LibrarySystem.Contracts.Protos; // Assuming this is where WafLogEntry lives
using Google.Protobuf.WellKnownTypes;

namespace LibrarySystem.Tools.ViewModels;

// 1. Inherit from ObservableObject (The Toolkit base)
public partial class WafLogViewModel : ObservableObject, IDisposable
{
    private readonly Security.SecurityClient _client;
    private CancellationTokenSource? _cts;
    
    // 2. Thread-Safe Collection
    // We use a lock object to ensure the UI doesn't crash if gRPC returns data fast.
    private readonly object _lock = new();

    public ObservableCollection<WafLogEntry> Logs { get; } = new();

    // 3. Modern Property Declaration
    // [ObservableProperty] automatically generates:
    // - public bool IsMonitoring { get; set; }
    // - OnPropertyChanged("IsMonitoring")
    // - OnIsMonitoringChanged(value) hook
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(MonitoringButtonText))] // 4. Links dependencies
    private bool _isMonitoring;

    [ObservableProperty]
    private string _statusText = "Security Stream Offline";

    // Computed property for the UI text
    public string MonitoringButtonText => IsMonitoring ? "Stop Monitoring" : "Start Monitoring";

    public WafLogViewModel(Security.SecurityClient client)
    {
        _client = client;
        // AppSec/Stability: Allow cross-thread updates to the collection safely
        BindingOperations.EnableCollectionSynchronization(Logs, _lock);
    }

    // 5. The State Hook (Replaces the conflicting Command)
    // This runs automatically whenever IsMonitoring changes (via Toggle or Code)
    partial void OnIsMonitoringChanged(bool value)
    {
        if (value)
        {
            // Fire-and-forget strictly handled inside StartMonitoring
            _ = StartMonitoringAsync();
        }
        else
        {
            StopMonitoring();
        }
    }

    private async Task StartMonitoringAsync()
    {
        // Availability: Always recreate CTS to avoid using a disposed token
        _cts = new CancellationTokenSource();
        StatusText = "Connecting to WAF Core...";

        try
        {
            using var stream = _client.WatchWafLogs(new Empty(), cancellationToken: _cts.Token);
            StatusText = "⚡ LIVE STREAMING";

            await foreach (var log in stream.ResponseStream.ReadAllAsync(_cts.Token))
            {
                lock (_lock)
                {
                    Logs.Insert(0, log);

                    // AppSec (Availability): Hard limit to prevent Memory Exhaustion / UI Freeze
                    // If an attacker floods the WAF, the tool must not crash.
                    if (Logs.Count > 100) Logs.RemoveAt(Logs.Count - 1);
                }
            }
        }
        catch (RpcException ex) when (ex.StatusCode == StatusCode.Cancelled)
        {
            StatusText = "Stream Stopped.";
        }
        catch (Exception ex)
        {
            // Error Handling: Don't just dump ex.Message if it contains sensitive info, 
            // but for connection errors, it is usually safe.
            StatusText = $"Connection Lost: {ex.Message}";
            
            // Reset state without triggering the loop again
            _isMonitoring = false; 
            OnPropertyChanged(nameof(IsMonitoring));
            OnPropertyChanged(nameof(MonitoringButtonText));
        }
    }

    private void StopMonitoring()
    {
        _cts?.Cancel();
        _cts?.Dispose();
        _cts = null;
        StatusText = "Stream Paused";
    }

    // Cleanup to prevent memory leaks if the Window closes while streaming
    public void Dispose()
    {
        StopMonitoring();
        GC.SuppressFinalize(this);
    }
}