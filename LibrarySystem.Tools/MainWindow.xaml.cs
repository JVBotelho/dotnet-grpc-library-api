using System.Windows;

namespace LibrarySystem.Tools;

public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();
    }

    // View-only concern: toggle window maximise state.
    private void OnExpandClick(object sender, RoutedEventArgs e) =>
        WindowState = WindowState == WindowState.Normal
            ? WindowState.Maximized
            : WindowState.Normal;
}