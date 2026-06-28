using System.Threading;
using System.Threading.Tasks;

namespace LibrarySystem.Application.Abstractions.Services;

public interface IImageHashingService
{
    Task<string> ComputeHashAsync(byte[] imageData, CancellationToken cancellationToken = default);
}
