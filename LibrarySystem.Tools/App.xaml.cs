using System.Windows;
using LibrarySystem.Contracts.Protos;
using LibrarySystem.Tools.ViewModels;
using LibrarySystem.Tools.Services;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Grpc.Net.Client;
using LibrarySystem.Tools.Services.Core;

namespace LibrarySystem.Tools;

/// <summary>
/// Interaction logic for App.xaml
/// </summary>
public partial class App : Application
{
    private readonly IHost _host;

    public App()
    {
        _host = Host.CreateDefaultBuilder()
            .ConfigureServices((context, services) =>
            {
                services.AddSingleton(services => 
                {
                    var channel = GrpcChannel.ForAddress("http://localhost:5001"); 
                    return new Library.LibraryClient(channel);
                });

                services.AddSingleton<ILogTailerService, FileLogTailerService>();
                services.AddSingleton<IGraphDataService, GrpcGraphDataService>();

                services.AddTransient<MainViewModel>();
                services.AddTransient<InspectorViewModel>();
                services.AddTransient<GraphViewModel>();

                services.AddSingleton<MainWindow>(s => new MainWindow()
                {
                    DataContext = s.GetRequiredService<MainViewModel>()
                });
            })
            .Build();
    }

    protected override async void OnStartup(StartupEventArgs e)
    {
        await _host.StartAsync();

        var mainWindow = _host.Services.GetRequiredService<MainWindow>();
        mainWindow.Show();

        base.OnStartup(e);
    }

    protected override async void OnExit(ExitEventArgs e)
    {
        await _host.StopAsync();
        base.OnExit(e);
    }
}

