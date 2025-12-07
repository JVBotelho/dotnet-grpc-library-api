using LibrarySystem.Api;
using LibrarySystem.Contracts.Protos;

var builder = WebApplication.CreateBuilder(args);


builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services.AddGrpcClient<Library.LibraryClient>(options =>
{
    options.Address = new Uri("https://localhost:7049"); 
});

builder.Services.AddSingleton<IApiMarker, ApiMarker>();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

app.MapControllers();

app.Run();

public partial class Program { }