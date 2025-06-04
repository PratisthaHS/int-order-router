using Azure.Storage.Blobs;
using int_order_router.Helpers;
using int_order_router.Services.Interfaces;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;
using System.Text.Json;

namespace int_order_router.Functions;

public class Blob204Router
{
    private readonly ILogger _logger;
    private readonly BlobServiceClient _blobServiceClient;
    private readonly Edi204Parser _parser;
    private readonly IRoutingService _routingService;

    public Blob204Router(ILoggerFactory loggerFactory, BlobServiceClient blobServiceClient, Edi204Parser parser, IRoutingService routingService)
    {
        _logger = loggerFactory.CreateLogger<Blob204Router>();
        _blobServiceClient = blobServiceClient;
        _parser = parser;
        _routingService = routingService;
    }

    [Function("Blob204Router")]
    public async Task Run(
        [BlobTrigger("edi-intake/{name}", Connection = "AzureWebJobsStorage")] ReadOnlyMemory<byte> blobBytes,
        string name)
    {
        _logger.LogInformation($"Blob trigger fired for file: {name}");

        var sourceContainer = _blobServiceClient.GetBlobContainerClient("edi-intake");
        var sourceBlob = sourceContainer.GetBlobClient(name);

        var routerContainer = _blobServiceClient.GetBlobContainerClient("router-staging");
        await routerContainer.CreateIfNotExistsAsync();

        // Step 1: Copy to router-staging/originals/
        var originalBlob = routerContainer.GetBlobClient($"originals/{name}");
        await originalBlob.StartCopyFromUriAsync(sourceBlob.Uri);
        _logger.LogInformation($"Copied to 'router-staging/originals/{name}'");

        // Step 2: Delete from edi-intake (complete move)
        await sourceBlob.DeleteIfExistsAsync();
        _logger.LogInformation($"Deleted original blob from 'edi-intake/{name}'");

        // Step 3: Parse for routing
        using var stream = new MemoryStream(blobBytes.ToArray());
        using var reader = new StreamReader(stream);
        var content = await reader.ReadToEndAsync();
        
        var parsedRecords = _parser.Parse(content);

        string? routeTo = null;
        foreach (var record in parsedRecords)
        {
            routeTo = await _routingService.RouteAsync(record);
            // _logger.LogInformation($"Routed to: {routeTo} - ShipmentID: {record.ShipmentId}, Pickup: {record.PickupCity}, Delivery: {record.DeliveryCity}, Order#: {record.ContainerId}, ContainerOwner: {record.ContainerOwner}, CustomerName: {record.CustomerName}");
            string json = JsonSerializer.Serialize(record, new JsonSerializerOptions
            {
            WriteIndented = false
            });
            _logger.LogInformation($"Routed to: {routeTo} - Record: {json}");
        }

        // Step 4: Move to respective TMS folder
        if (!string.IsNullOrEmpty(routeTo))
        {
            var tmsBlob = routerContainer.GetBlobClient($"{routeTo}/{name}");
            await tmsBlob.StartCopyFromUriAsync(originalBlob.Uri);
            await originalBlob.DeleteIfExistsAsync();

            _logger.LogInformation($"Moved blob from 'originals/{name}' to '{routeTo}/{name}'");
        }
    }
}
