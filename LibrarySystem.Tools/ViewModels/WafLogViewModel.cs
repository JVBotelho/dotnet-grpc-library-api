using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Data;
using CommunityToolkit.Mvvm.ComponentModel;
using LibrarySystem.Contracts.Protos;
using LibrarySystem.Tools.Services;

namespace LibrarySystem.Tools.ViewModels;

public partial class WafLogViewModel : ObservableObject, IDisposable
{
    private readonly ILogTailerService _logService;
    private CancellationTokenSource? _cts;
    private readonly object _lock = new();

    public ObservableCollection<WafLogEntry> Logs { get; } = new();

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(MonitoringButtonText))]
    private bool _isMonitoring;

    [ObservableProperty]
    private string _statusText = "Security Stream Offline";

    public string MonitoringButtonText => IsMonitoring ? "Stop Monitoring" : "Start Monitoring";

    public WafLogViewModel(ILogTailerService logService)
    {
        _logService = logService;
        // Guard: BindingOperations requires the WPF infrastructure.
        // In unit test hosts there is no running Application instance.
        if (Application.Current != null)
            BindingOperations.EnableCollectionSynchronization(Logs, _lock);
    }

    internal Task? CurrentStreamTask { get; private set; }

    partial void OnIsMonitoringChanged(bool value)
    {
        if (value) CurrentStreamTask = StartMonitoringAsync();
        else StopMonitoring();
    }

    private async Task StartMonitoringAsync()
    {
        _cts = new CancellationTokenSource();
        StatusText = "Connecting to WAF Core...";

        try
        {
            StatusText = "LIVE STREAMING";
            await foreach (var log in _logService.WatchAsync(_cts.Token))
            {
                lock (_lock)
                {
                    Logs.Insert(0, log);
                    if (Logs.Count > 100) Logs.RemoveAt(Logs.Count - 1);
                }
            }
            StatusText = "Stream Paused";
        }
        catch (OperationCanceledException)
        {
            StatusText = "Stream Paused";
        }
        catch (Exception ex)
        {
            // IsMonitoring = false triggers StopMonitoring() which sets "Stream Paused".
            // Set the error message AFTER so it is not overwritten.
            IsMonitoring = false;
            StatusText = $"Connection Lost: {ex.Message}";
        }
    }

    private void StopMonitoring()
    {
        _cts?.Cancel();
        _cts?.Dispose();
        _cts = null;
        StatusText = "Stream Paused";
    }

    public void Dispose()
    {
        StopMonitoring();
        GC.SuppressFinalize(this);
    }
}
