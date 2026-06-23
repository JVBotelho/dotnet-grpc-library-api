namespace LibrarySystem.Tools.Services;

public enum ToastTone { Info, Success, Warning, Error }

public record ToastNotification(ToastTone Tone, string Title, string Message);

public interface INotificationService
{
    void ShowSuccess(string title, string message);
    void ShowError(string title, string message);
    void ShowInfo(string title, string message);
}
