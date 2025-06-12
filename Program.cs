using Azure.Storage.Blobs;
using int_order_router.Helpers;
using int_order_router.Services;
using int_order_router.Services.Interfaces;
using Microsoft.Data.SqlClient;
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

        services.AddScoped(serviceProvider =>
        {
            var config = serviceProvider.GetRequiredService<IConfiguration>();
            var connectionString = config.GetConnectionString("SqlConnection");
            return new SqlConnection(connectionString);
        });

        services.AddSingleton<Edi204Parser>();
        services.AddScoped<IRoutingService, RoutingService>();
    })
    .Build();

host.Run();
