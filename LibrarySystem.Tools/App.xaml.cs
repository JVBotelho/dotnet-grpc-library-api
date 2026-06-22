using System.Windows;
using LibrarySystem.Contracts.Protos;
using LibrarySystem.Tools.Services;
using LibrarySystem.Tools.Services.Core;
using LibrarySystem.Tools.ViewModels;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Grpc.Net.Client;

namespace LibrarySystem.Tools;

public partial class App : Application
{
    private readonly IHost _host;

    public App()
    {
        _host = Host.CreateDefaultBuilder()
            .ConfigureServices((_, services) =>
            {
                services.AddSingleton(_ =>
                    GrpcChannel.ForAddress("http://localhost:5001"));

                services.AddSingleton(s =>
                    new Library.LibraryClient(s.GetRequiredService<GrpcChannel>()));
                services.AddSingleton(s =>
                    new Security.SecurityClient(s.GetRequiredService<GrpcChannel>()));

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
