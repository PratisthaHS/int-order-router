using Azure.Storage.Blobs;
using int_order_router.Helpers;
using int_order_router.Services;
using int_order_router.Services.Interfaces;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

var host = new HostBuilder()
    .ConfigureFunctionsWorkerDefaults()
    .ConfigureServices(services =>
    {
        // Register BlobServiceClient using connection string from app settings
        services.AddSingleton(serviceProvider =>
        {
            var configuration = serviceProvider.GetRequiredService<IConfiguration>();
            var connectionString = configuration.GetValue<string>("AzureWebJobsStorage");
            return new BlobServiceClient(connectionString);
        });

        services.AddSingleton<Text204Parser>();
        services.AddScoped<IRoutingService, RoutingService>();
    })
    .Build();

host.Run();
