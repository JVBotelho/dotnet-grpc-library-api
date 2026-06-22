using Google.Protobuf.WellKnownTypes;
using Grpc.Core;
using LibrarySystem.Contracts.Protos;

namespace LibrarySystem.Tools.Services.Core;

public class GrpcGraphDataService : IGraphDataService
{
    private readonly Library.LibraryClient _client;

    public GrpcGraphDataService(Library.LibraryClient client) => _client = client;

    public async Task<IReadOnlyList<BookResponse>> GetTopologyAsync(CancellationToken ct = default)
    {
        var response = await _client.GetAllBooksAsync(new Empty(), cancellationToken: ct);
        return response.Books;
    }

    public async Task<BookResponse> UpdateBookAsync(UpdateBookRequest request, CancellationToken ct = default) =>
        await _client.UpdateBookAsync(request, cancellationToken: ct);
}
