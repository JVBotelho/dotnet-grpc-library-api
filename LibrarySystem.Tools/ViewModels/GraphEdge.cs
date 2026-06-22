using System.ComponentModel;
using CommunityToolkit.Mvvm.ComponentModel;

namespace LibrarySystem.Tools.ViewModels;

public partial class GraphEdge : ObservableObject
{
    private readonly GraphNode _source;
    private readonly GraphNode _target;

    [ObservableProperty] private double _x1;
    [ObservableProperty] private double _y1;
    [ObservableProperty] private double _x2;
    [ObservableProperty] private double _y2;

    // Lights up when either endpoint node is selected.
    [ObservableProperty] private bool _isActive;

    public GraphNode Source => _source;
    public GraphNode Target => _target;

    public GraphEdge(GraphNode source, GraphNode target)
    {
        _source = source;
        _target = target;
        _source.PropertyChanged += OnNodeChanged;
        _target.PropertyChanged += OnNodeChanged;
        UpdateCoordinates();
    }

    private void OnNodeChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName is nameof(GraphNode.X) or nameof(GraphNode.Y))
            UpdateCoordinates();
    }

    private void UpdateCoordinates()
    {
        // Connect right-center of source to left-center of target (200×100 node).
        X1 = _source.X + 200;
        Y1 = _source.Y + 50;
        X2 = _target.X;
        Y2 = _target.Y + 50;
    }
}
