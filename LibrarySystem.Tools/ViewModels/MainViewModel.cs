using System.Collections.ObjectModel;
using System.Windows;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using LibrarySystem.Contracts.Protos;
using Google.Protobuf.WellKnownTypes;

namespace LibrarySystem.Tools.ViewModels;

public partial class MainViewModel : ObservableObject
{
    private readonly Library.LibraryClient _client;
    
    // Otimização: Rastrear o nó selecionado para evitar loops desnecessários
    private GraphNode? _currentlySelectedNode;

    [ObservableProperty]
    private ObservableCollection<GraphNode> _nodes = new();
    
    [ObservableProperty]
    private ObservableCollection<GraphEdge> _edges = new();
    
    // Injeção de Dependência do Inspector (Painel Lateral)
    [ObservableProperty]
    private InspectorViewModel _inspector;

    [ObservableProperty]
    private string _statusMessage = "Ready";

    public MainViewModel(Library.LibraryClient client, InspectorViewModel inspector)
    {
        _client = client;
        _inspector = inspector;
    }
    
    [RelayCommand]
    public void SelectNode(GraphNode node)
    {
        // 1. Otimização O(1): Desmarca apenas o anterior em vez de varrer a lista toda
        if (_currentlySelectedNode != null)
        {
            _currentlySelectedNode.IsSelected = false;
        }

        // 2. Marca o novo
        node.IsSelected = true;
        _currentlySelectedNode = node;

        // 3. Envia para o Inspector (Lado Direito da tela)
        Inspector.LoadNode(node);
    }

    [RelayCommand]
    public async Task LoadGraphData()
    {
        try
        {
            StatusMessage = "Fetching topology from gRPC Core...";
            
            // Limpa seleção anterior ao recarregar
            _currentlySelectedNode = null;
            // Limpa o Inspector visualmente
            Inspector.SelectedNode = null;

            var response = await _client.GetAllBooksAsync(new Empty());

            Nodes.Clear();
            Edges.Clear();
            
            var authorNodes = new Dictionary<string, GraphNode>();
            var random = new Random();

            foreach (var book in response.Books)
            {
                // --- Lógica do Autor (Nó Pai) ---
                if (!authorNodes.TryGetValue(book.Author, out var authorNode))
                {
                    authorNode = new GraphNode
                    {
                        Id = random.Next(10000, 99999), // ID fictício visual
                        Title = book.Author,
                        Subtitle = "Author Entity",
                        NodeType = "Author",
                        // Layout: Autores ficam na parte superior
                        X = random.Next(50, 1200),
                        Y = random.Next(50, 250),
                        OriginalData = null // Autores são agregados, não editáveis diretamente neste DTO
                    };
                    Nodes.Add(authorNode);
                    authorNodes[book.Author] = authorNode;
                }

                // --- Lógica do Livro (Nó Filho) ---
                var bookNode = new GraphNode
                {
                    Id = book.Id,
                    Title = book.Title,
                    Subtitle = $"Published: {book.PublicationYear}",
                    NodeType = "Book", // Define a cor no XAML (Azul)
                    
                    // Layout: Livros ficam espalhados na parte inferior
                    X = random.Next(50, 1200),
                    Y = random.Next(350, 600),
                    
                    // --- O PULO DO GATO ---
                    // Guardamos o DTO original. É isso que permite o "Live Edit".
                    OriginalData = book 
                };
                Nodes.Add(bookNode);

                // --- Conexão (Edge) ---
                // Cria a linha entre Autor e Livro
                Edges.Add(new GraphEdge(authorNode, bookNode));
            }

            StatusMessage = $"Topology Loaded: {Nodes.Count} Nodes, {Edges.Count} Connections.";
        }
        catch (Exception ex)
        {
            StatusMessage = $"Connection Error. Is Docker running? ({ex.Message})";
            MessageBox.Show($"gRPC Connection Failed.\n\nCheck if Docker container 'library-grpc' is up and port 5001 is mapped.\n\nError: {ex.Message}");
        }
    }
}