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
            .ConfigureAppConfiguration(cfg =>
            {
                cfg.AddJsonFile("appsettings.json", optional: false);
                cfg.AddJsonFile("appsettings.Development.json", optional: true);
                cfg.AddEnvironmentVariables(); // GrpcApiKey env var overrides file config.
            })
            .ConfigureServices((ctx, services) =>
            {
                var endpoint = ctx.Configuration["GrpcEndpoint"]
                    ?? throw new InvalidOperationException("GrpcEndpoint is not configured.");
                var apiKey = ctx.Configuration["GrpcApiKey"];
                if (string.IsNullOrEmpty(apiKey))
                    throw new InvalidOperationException(
                        "GrpcApiKey is missing. Set it in appsettings.Development.json or via the GrpcApiKey environment variable.");

                // Required for cleartext HTTP/2 (h2c) on non-loopback; harmless for localhost.
                AppContext.SetSwitch("System.Net.Http.SocketsHttpHandler.Http2UnencryptedSupport", true);
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
                services.AddSingleton<WafLogViewModel>(); // owns the WAF stream — disposed with host
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
        try
        {
            await _host.StartAsync();
            _host.Services.GetRequiredService<MainWindow>().Show();
            base.OnStartup(e);
        }
        catch (Exception ex)
        {
            MessageBox.Show(
                $"Application failed to start:\n\n{ex.Message}",
                "LibrarySystem — Fatal Error",
                MessageBoxButton.OK, MessageBoxImage.Error);
            Shutdown(1);
        }
    }

    protected override async void OnExit(ExitEventArgs e)
    {
        await _host.StopAsync();
        _host.Dispose(); // disposes the DI container, which cancels WafLogViewModel stream
        base.OnExit(e);
    }
}
