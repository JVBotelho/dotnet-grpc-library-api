using CommunityToolkit.Mvvm.ComponentModel;
using LibrarySystem.Contracts.Protos;

namespace LibrarySystem.Tools.ViewModels;

public partial class GraphNode : ObservableObject
{
    [ObservableProperty] private double _x;
    [ObservableProperty] private double _y;
    [ObservableProperty] private string _title = string.Empty;
    [ObservableProperty] private string _subtitle = string.Empty;
    [ObservableProperty] private int _id;
    
    [ObservableProperty] private string _nodeType = "Book"; 
    
    [ObservableProperty] private bool _isSelected;
    
    public BookResponse? OriginalData { get; set; }
}