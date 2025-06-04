using Azure.Storage.Blobs;
using int_order_router.Helpers;
using int_order_router.Models;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;

namespace int_order_router.Functions;

public class Blob204Router
{
    private readonly ILogger _logger;
    private readonly BlobServiceClient _blobServiceClient;
    private readonly Text204Parser _parser;

    public Blob204Router(ILoggerFactory loggerFactory, BlobServiceClient blobServiceClient, Text204Parser parser)
    {
        _logger = loggerFactory.CreateLogger<Blob204Router>();
        _blobServiceClient = blobServiceClient;
        _parser = parser;
    }

    [Function("Blob204Router")]
    public async Task Run(
        [BlobTrigger("edi-intake/{name}", Connection = "AzureWebJobsStorage")] ReadOnlyMemory<byte> blobBytes,
        string name)
    {
        _logger.LogInformation($"Blob trigger fired for file: {name}");

        // Parse file content
        using var stream = new MemoryStream(blobBytes.ToArray());
        using var reader = new StreamReader(stream);
        var content = await reader.ReadToEndAsync();
        var lines = content.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
        var parsedRecords = _parser.Parse(lines);

        foreach (var record in parsedRecords)
        {
            _logger.LogInformation($"Record - ShipmentID: {record.ShipmentId}, Bkg: {record.BookingNumber}, From: {record.PickupCity}, To: {record.DeliveryCity}, Order#: {record.CustomerOrderNumber}");
        }
        
        // Copy to router-staging container
        var targetContainer = _blobServiceClient.GetBlobContainerClient("router-staging");
        await targetContainer.CreateIfNotExistsAsync();
        var targetBlob = targetContainer.GetBlobClient(name);
        await targetBlob.UploadAsync(new BinaryData(blobBytes.ToArray()), overwrite: true);

        _logger.LogInformation($"Successfully copied blob '{name}' to 'router-staging' ");

    }
}
