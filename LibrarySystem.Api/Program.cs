using LibrarySystem.Api;
using LibrarySystem.Contracts.Protos;

var builder = WebApplication.CreateBuilder(args);


builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services.AddGrpcClient<Library.LibraryClient>(o =>
{
    var address = builder.Configuration["GrpcSettings:ServiceUrl"] ?? "https://localhost:7049";

    o.Address = new Uri(address);

    if (o.Address.Scheme == "http")
        AppContext.SetSwitch("System.Net.Http.SocketsHttpHandler.Http2UnencryptedSupport", true);
});

builder.Services.AddSingleton<IApiMarker, ApiMarker>();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

// app.UseHttpsRedirection();

app.MapControllers();

app.Run();

public partial class Program
{
}