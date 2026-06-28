using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using CommunityToolkit.Mvvm.Messaging;
using Microsoft.Xaml.Behaviors;
using LibrarySystem.Tools.Messages;
using LibrarySystem.Tools.ViewModels;

namespace LibrarySystem.Tools.Behaviors;

public class NodeDragBehavior : Behavior<FrameworkElement>
{
    private bool _isDragging;
    private Point _mouseStartPosition;
    private Canvas? _parentCanvas;

    protected override void OnAttached()
    {
        base.OnAttached();
        AssociatedObject.PreviewMouseLeftButtonDown += OnMouseDown;
        AssociatedObject.PreviewMouseLeftButtonUp   += OnMouseUp;
        AssociatedObject.PreviewMouseMove           += OnMouseMove;
    }

    protected override void OnDetaching()
    {
        base.OnDetaching();
        AssociatedObject.PreviewMouseLeftButtonDown -= OnMouseDown;
        AssociatedObject.PreviewMouseLeftButtonUp   -= OnMouseUp;
        AssociatedObject.PreviewMouseMove           -= OnMouseMove;
    }

    private void OnMouseDown(object sender, MouseButtonEventArgs e)
    {
        if (AssociatedObject.DataContext is GraphNode node)
            WeakReferenceMessenger.Default.Send(new NodeSelectedMessage(node));

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

        var current = e.GetPosition(_parentCanvas);
        var delta   = current - _mouseStartPosition;

        if (AssociatedObject.DataContext is GraphNode node)
        {
            node.X += delta.X;
            node.Y += delta.Y;
        }

        _mouseStartPosition = current;
    }

    private static T? FindParent<T>(DependencyObject child) where T : DependencyObject
    {
        while (true)
        {
            var parent = VisualTreeHelper.GetParent(child);
            if (parent == null) return null;
            if (parent is T t) return t;
            child = parent;
        }
    }
}
