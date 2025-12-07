using LibrarySystem.Application;
using LibrarySystem.Grpc.Services;
using LibrarySystem.Persistence;

var builder = WebApplication.CreateBuilder(args);

builder.WebHost.ConfigureKestrel(options =>
{
    // options.ListenLocalhost(7049, o => o.Protocols = Microsoft.AspNetCore.Server.Kestrel.Core.HttpProtocols.Http2);
});

// Adicionar serviços
builder.Services.AddGrpc();
builder.Services.AddApplication();
builder.Services.AddPersistence(builder.Configuration);

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;
    try
    {
        // Executa o SeedData que criamos anteriormente dentro da Persistence
        // Nota: O método SeedDatabaseAsync era uma extensão de IHost, talvez precise ajustar a chamada:
        await SeedData.SeedDatabaseAsync(app); 
    }
    catch (Exception ex)
    {
        var logger = services.GetRequiredService<ILogger<Program>>();
        logger.LogError(ex, "An error occurred while seeding the database.");
    }
}

app.MapGrpcService<LibraryService>();
app.MapGet("/", () => "Communication with gRPC endpoints must be made through a gRPC client.");

app.Run();