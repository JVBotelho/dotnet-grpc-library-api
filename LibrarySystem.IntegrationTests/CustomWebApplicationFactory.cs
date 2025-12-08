using LibrarySystem.Contracts.Protos;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Moq;

namespace LibrarySystem.IntegrationTests;

public class CustomWebApplicationFactory : WebApplicationFactory<Program>
{
    // A CORREÇÃO ESTÁ AQUI: { CallBase = true }
    // Isso permite que o Mock redirecione as chamadas das sobrecargas simplificadas 
    // para a sobrecarga principal que aceita 'CallOptions', onde fizemos o Setup.
    public Mock<Library.LibraryClient> LibraryClientMock { get; } = new(MockBehavior.Loose) { CallBase = true };

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.ConfigureTestServices(services =>
        {
            services.RemoveAll(typeof(Library.LibraryClient));
            services.RemoveAll(typeof(Grpc.Net.ClientFactory.GrpcClientFactory));
            
            services.AddSingleton(LibraryClientMock.Object);
        });
    }
}