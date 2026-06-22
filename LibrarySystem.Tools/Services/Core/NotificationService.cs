using CommunityToolkit.Mvvm.ComponentModel;

namespace LibrarySystem.Tools.Services.Core;

public partial class NotificationService : ObservableObject, INotificationService
{
    [ObservableProperty]
    private ToastNotification? _activeToast;

    private CancellationTokenSource? _dismissCts;

    public void ShowSuccess(string title, string message) =>
        Show(new ToastNotification(ToastTone.Success, title, message), 3200);

    public void ShowError(string title, string message) =>
        Show(new ToastNotification(ToastTone.Error, title, message), 5000);

    public void ShowInfo(string title, string message) =>
        Show(new ToastNotification(ToastTone.Info, title, message), 2600);

    public void Dismiss() => ActiveToast = null;

    private void Show(ToastNotification toast, int durationMs)
    {
        _dismissCts?.Cancel();
        _dismissCts?.Dispose();
        _dismissCts = new CancellationTokenSource();
        ActiveToast = toast;
        _ = DismissAfterAsync(durationMs, _dismissCts.Token);
    }

    private async Task DismissAfterAsync(int ms, CancellationToken ct)
    {
        try { await Task.Delay(ms, ct); ActiveToast = null; }
        catch (OperationCanceledException) { }
    }
}
