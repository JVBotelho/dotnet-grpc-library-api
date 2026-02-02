using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using Microsoft.Xaml.Behaviors;
using LibrarySystem.Tools.ViewModels;

namespace LibrarySystem.Tools.Behaviors;

public class NodeDragBehavior : Behavior<FrameworkElement>
{
    private bool _isDragging;
    private Point _mouseStartPosition;
    private FrameworkElement? _parentCanvas;

    protected override void OnAttached()
    {
        base.OnAttached();
        AssociatedObject.PreviewMouseLeftButtonDown += OnMouseDown;
        AssociatedObject.PreviewMouseLeftButtonUp += OnMouseUp;
        AssociatedObject.PreviewMouseMove += OnMouseMove;
    }

    protected override void OnDetaching()
    {
        base.OnDetaching();
        AssociatedObject.PreviewMouseLeftButtonDown -= OnMouseDown;
        AssociatedObject.PreviewMouseLeftButtonUp -= OnMouseUp;
        AssociatedObject.PreviewMouseMove -= OnMouseMove;
    }

    private void OnMouseDown(object sender, MouseButtonEventArgs e)
    {
        if (AssociatedObject.DataContext is GraphNode node)
        {
            var window = Application.Current.MainWindow;
            if (window?.DataContext is MainViewModel mainVm)
            {
                mainVm.SelectNodeCommand.Execute(node);
            }
        }

        _parentCanvas ??= FindParent<Canvas>(AssociatedObject);
        _isDragging = true;
        _mouseStartPosition = e.GetPosition(_parentCanvas);
        AssociatedObject.CaptureMouse();
    }

    private void OnMouseUp(object sender, MouseButtonEventArgs e)
    {
        _isDragging = false;
        AssociatedObject.ReleaseMouseCapture();
    }

    private void OnMouseMove(object sender, MouseEventArgs e)
    {
        if (!_isDragging || _parentCanvas == null) return;

        var currentPosition = e.GetPosition(_parentCanvas);
        var offsetX = currentPosition.X - _mouseStartPosition.X;
        var offsetY = currentPosition.Y - _mouseStartPosition.Y;

        // 2. Atualiza o ViewModel (Data Binding Reverso)
        if (AssociatedObject.DataContext is GraphNode node)
        {
            node.X += offsetX;
            node.Y += offsetY;
        }

        // 3. Reseta a posição de referência para o próximo frame
        _mouseStartPosition = currentPosition;
    }

    // Helper para achar o Canvas na árvore visual
    private static T? FindParent<T>(DependencyObject child) where T : DependencyObject
    {
        while (true)
        {
            var parentObject = VisualTreeHelper.GetParent(child);
            if (parentObject == null) return null;
            if (parentObject is T parent) return parent;
            child = parentObject;
        }
    }
}