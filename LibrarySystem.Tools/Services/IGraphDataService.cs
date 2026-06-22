using LibrarySystem.Contracts.Protos;

namespace LibrarySystem.Tools.Services;

public interface IGraphDataService
{
    Task<IReadOnlyList<BookResponse>> GetTopologyAsync(CancellationToken ct = default);
    Task<BookResponse> UpdateBookAsync(UpdateBookRequest request, CancellationToken ct = default);
}
