using System.IO;
using System.Threading.Tasks;
using Azure.Storage.Blobs;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;

namespace int_order_router.Functions
{
    public class Blob204Router
    {
        private readonly ILogger _logger;
        private readonly BlobServiceClient _blobServiceClient;

        public Blob204Router(ILoggerFactory loggerFactory, BlobServiceClient blobServiceClient)
        {
            _logger = loggerFactory.CreateLogger<Blob204Router>();
            _blobServiceClient = blobServiceClient;
        }

        [Function("Blob204Router")]
        public async Task Run(
            [BlobTrigger("edi-intake/{name}", Connection = "AzureWebJobsStorage")] ReadOnlyMemory<byte> blobBytes,
            string name)
        {
            _logger.LogInformation($"Blob trigger fired for file: {name}");

            var targetContainer = _blobServiceClient.GetBlobContainerClient("router-staging");
            await targetContainer.CreateIfNotExistsAsync();

            var targetBlob = targetContainer.GetBlobClient(name);

            using var stream = new MemoryStream(blobBytes.ToArray());
            await targetBlob.UploadAsync(stream, overwrite: true);

            _logger.LogInformation($"Successfully copied blob '{name}' to 'router-staging'.");
        }
    }
}
