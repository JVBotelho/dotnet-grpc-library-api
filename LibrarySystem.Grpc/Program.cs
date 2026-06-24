using LibrarySystem.Application;
using LibrarySystem.Grpc.Interceptors;
using LibrarySystem.Grpc.Services;
using LibrarySystem.Persistence;
using Microsoft.AspNetCore.Server.Kestrel.Core;

using Microsoft.AspNetCore.Authentication.Certificate;
using System.Security.Cryptography.X509Certificates;

var builder = WebApplication.CreateBuilder(args);

builder.WebHost.ConfigureKestrel(options =>
{
    options.ConfigureEndpointDefaults(lo => lo.Protocols = HttpProtocols.Http2);
    // Add HTTP/2 keep alive so kiosk channel connectivity watcher works effectively
    options.Limits.Http2.KeepAlivePingDelay = TimeSpan.FromSeconds(60);
    options.Limits.Http2.KeepAlivePingTimeout = TimeSpan.FromSeconds(30);

    // Require client certificates for mTLS
    options.ConfigureHttpsDefaults(httpsOptions =>
    {
        httpsOptions.ClientCertificateMode = Microsoft.AspNetCore.Server.Kestrel.Https.ClientCertificateMode.RequireCertificate;
    });
});

builder.Services.AddGrpcClient<LibrarySystem.Contracts.Protos.Compute.ComputeClient>(o =>
{
    var url = builder.Configuration["GrpcSettings:ComputeUrl"] ?? "http://localhost:50052";
    o.Address = new Uri(url);
});

builder.Services.AddAuthentication(CertificateAuthenticationDefaults.AuthenticationScheme)
    .AddCertificate(options =>
    {
        if (!builder.Environment.IsDevelopment())
        {
            options.RevocationMode = X509RevocationMode.Online;
        }
        else
        {
            options.RevocationMode = X509RevocationMode.NoCheck;
        }
        
        options.Events = new CertificateAuthenticationEvents
        {
            OnCertificateValidated = context =>
            {
                var cert = context.ClientCertificate;
                var cn = cert.GetNameInfo(X509NameType.SimpleName, false);
                
                if (cn.StartsWith("KIOSK-", StringComparison.OrdinalIgnoreCase) || cn.StartsWith("INSPECTOR-", StringComparison.OrdinalIgnoreCase))
                {
                    var claims = new[] { new System.Security.Claims.Claim(System.Security.Claims.ClaimTypes.Name, cn) };
                    context.Principal = new System.Security.Claims.ClaimsPrincipal(new System.Security.Claims.ClaimsIdentity(claims, context.Scheme.Name));
                    context.Success();
                }
                else
                {
                    context.Fail("Invalid certificate.");
                }
                return Task.CompletedTask;
            }
        };
    });

builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("InspectorOnly", policy =>
        policy.RequireClaim(System.Security.Claims.ClaimTypes.Name, "INSPECTOR-WPF"));
});

builder.Services.AddSingleton<ApiKeyInterceptor>();
builder.Services.AddSingleton<DeviceIdentityInterceptor>();
builder.Services.AddSingleton<LibrarySystem.Application.Abstractions.ITelemetryHub, TelemetryHub>();
builder.Services.AddGrpc(options =>
{
    options.EnableDetailedErrors = builder.Environment.IsDevelopment();
    options.MaxReceiveMessageSize = 1 * 1024 * 1024; // 1 MB per message
    options.Interceptors.Add<ApiKeyInterceptor>();
    options.Interceptors.Add<DeviceIdentityInterceptor>();
});

builder.Services.AddApplication();
builder.Services.AddPersistence(builder.Configuration);

builder.Services.AddTransient<LibrarySystem.Application.Abstractions.Services.IImageHashingService, GrpcComputeHashingService>();

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;
    try
    {
        await app.SeedDatabaseAsync();
    }
    catch (Exception ex)
    {
        var logger = services.GetRequiredService<ILogger<Program>>();
        logger.LogError(ex, "An error occurred while seeding the database.");
    }
}

if (app.Environment.IsDevelopment())
{
    app.Logger.LogWarning("Certificate revocation check is disabled (development mode). Do not use in production.");
}

app.UseAuthentication();
app.UseAuthorization();

app.MapGrpcService<LibraryService>();
app.MapGrpcService<WafLogService>();
app.MapGrpcService<KioskService>();
app.MapGrpcService<TelemetryGrpcService>();
app.MapGet("/", () => "Communication with gRPC endpoints must be made through a gRPC client.");

app.Run();