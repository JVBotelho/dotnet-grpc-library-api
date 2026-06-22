using System.Windows;
using Grpc.Core.Interceptors;
using Grpc.Net.Client;
using LibrarySystem.Contracts.Protos;
using LibrarySystem.Tools.Services;
using LibrarySystem.Tools.Services.Core;
using LibrarySystem.Tools.ViewModels;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace LibrarySystem.Tools;

public partial class App : Application
{
    private readonly IHost _host;

    public App()
    {
        _host = Host.CreateDefaultBuilder()
            .ConfigureAppConfiguration(cfg => cfg.AddJsonFile("appsettings.json", optional: false))
            .ConfigureServices((ctx, services) =>
            {
                var endpoint = ctx.Configuration["GrpcEndpoint"]
                    ?? throw new InvalidOperationException("GrpcEndpoint is not configured.");
                var apiKey = ctx.Configuration["GrpcApiKey"]
                    ?? throw new InvalidOperationException("GrpcApiKey is not configured.");

                // h2c (plaintext) is acceptable for loopback localhost traffic.
                var channel = GrpcChannel.ForAddress(endpoint);
                var callInvoker = channel.Intercept(metadata =>
                {
                    metadata.Add("x-api-key", apiKey);
                    return metadata;
                });

                services.AddSingleton(channel);
                services.AddSingleton(_ => new Library.LibraryClient(callInvoker));
                services.AddSingleton(_ => new Security.SecurityClient(callInvoker));

                // Service abstractions — ViewModels depend only on these interfaces.
                services.AddSingleton<IGraphDataService, GrpcGraphDataService>();
                services.AddSingleton<ILogTailerService, GrpcLogTailerService>();

                // Notification service: singleton registered as both concrete
                // type (for XAML binding) and interface (for ViewModel injection).
                services.AddSingleton<NotificationService>();
                services.AddSingleton<INotificationService>(
                    s => s.GetRequiredService<NotificationService>());

                services.AddTransient<InspectorViewModel>();
                services.AddTransient<WafLogViewModel>();
                services.AddTransient<MainViewModel>();

                services.AddSingleton<MainWindow>(s => new MainWindow
                {
                    DataContext = s.GetRequiredService<MainViewModel>()
                });
            })
            .Build();
    }

    protected override async void OnStartup(StartupEventArgs e)
    {
        await _host.StartAsync();
        _host.Services.GetRequiredService<MainWindow>().Show();
        base.OnStartup(e);
    }

    protected override async void OnExit(ExitEventArgs e)
    {
        await _host.StopAsync();
        base.OnExit(e);
    }
}
