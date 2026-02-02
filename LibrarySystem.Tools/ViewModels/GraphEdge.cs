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
        {
            UpdateCoordinates();
        }
    }

    private void UpdateCoordinates()
    {
        X1 = _source.X + 100;
        Y1 = _source.Y + 50;

        X2 = _target.X + 100;
        Y2 = _target.Y + 50;
    }
}