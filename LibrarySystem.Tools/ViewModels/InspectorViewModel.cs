using System.Windows;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using LibrarySystem.Contracts.Protos;

namespace LibrarySystem.Tools.ViewModels;

public partial class InspectorViewModel : ObservableObject
{
    private readonly Library.LibraryClient _client;
    
    // O Nó que estamos editando atualmente
    [ObservableProperty] private GraphNode? _selectedNode;

    // Propriedades editáveis (Binds do TextBox)
    [ObservableProperty] private string _editTitle = string.Empty;
    [ObservableProperty] private string _editAuthor = string.Empty;
    [ObservableProperty] private int _editYear;

    public InspectorViewModel(Library.LibraryClient client)
    {
        _client = client;
    }

    // Chamado pelo MainViewModel quando o usuário clica num nó
    public void LoadNode(GraphNode node)
    {
        SelectedNode = node;
        
        // Copia dados para edição (se for um Livro)
        if (node.OriginalData != null)
        {
            EditTitle = node.OriginalData.Title;
            EditAuthor = node.OriginalData.Author;
            EditYear = node.OriginalData.PublicationYear;
        }
        else
        {
            // Fallback se for nó de Autor ou outro
            EditTitle = node.Title;
            EditAuthor = "System Entity";
        }
    }

    [RelayCommand]
    public async Task SaveChanges()
    {
        if (SelectedNode?.OriginalData == null) return;

        try
        {
            // 1. Cria o Request gRPC
            var request = new UpdateBookRequest
            {
                Id = SelectedNode.Id,
                Title = EditTitle,
                Author = EditAuthor,
                PublicationYear = EditYear,
                // Mantemos os outros campos originais para não zerar
                Pages = SelectedNode.OriginalData.Pages,
                TotalCopies = SelectedNode.OriginalData.TotalCopies
            };

            // 2. Envia para o Backend (Docker)
            var response = await _client.UpdateBookAsync(request);

            // 3. Atualiza o Gráfico Visualmente (Feedback Instantâneo)
            SelectedNode.Title = response.Title;
            SelectedNode.Subtitle = response.Author; // Se mudou o autor
            
            // Atualiza o cache local
            SelectedNode.OriginalData = response;

            MessageBox.Show("Saved successfully via gRPC!", "Success");
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Update failed: {ex.Message}", "Error");
        }
    }
}