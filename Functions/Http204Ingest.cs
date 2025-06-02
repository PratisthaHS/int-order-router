using System.Text;
using Azure.Storage.Queues;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Logging;

namespace int_order_router.Functions
{
    public class Http204Ingest
    {
        private readonly ILogger<Http204Ingest> _logger;

        public Http204Ingest(ILoggerFactory loggerFactory)
        {
            _logger = loggerFactory.CreateLogger<Http204Ingest>();
        }

        [Function("Http204Ingest")]
        public async Task<HttpResponseData> Run(
            [HttpTrigger(AuthorizationLevel.Function, "post")] HttpRequestData req,
            FunctionContext executionContext)
        {
            _logger.LogInformation("Received POST request to /Http204Ingest.");

            string requestBody = await new StreamReader(req.Body).ReadToEndAsync();

            if (string.IsNullOrWhiteSpace(requestBody))
            {
                _logger.LogWarning("Empty request body received.");
                var badResponse = req.CreateResponse(System.Net.HttpStatusCode.BadRequest);
                await badResponse.WriteStringAsync("Payload is empty.");
                return badResponse;
            }

            try
            {
                var connectionString = Environment.GetEnvironmentVariable("AzureWebJobsStorage");
                var queueName = Environment.GetEnvironmentVariable("QueueName") ?? "edi-orders";

                var queueClient = new QueueClient(connectionString, queueName);
                await queueClient.CreateIfNotExistsAsync();

                var encodedPayload = Convert.ToBase64String(Encoding.UTF8.GetBytes(requestBody));
                await queueClient.SendMessageAsync(encodedPayload);

                _logger.LogInformation("Payload successfully enqueued.");

                var response = req.CreateResponse(System.Net.HttpStatusCode.OK);
                await response.WriteStringAsync("Order received and queued.");
                return response;
            }
            catch (Exception ex)
            {
                _logger.LogError($"Queueing failed: {ex.Message}");
                var errorResponse = req.CreateResponse(System.Net.HttpStatusCode.InternalServerError);
                await errorResponse.WriteStringAsync("Failed to queue order.");
                return errorResponse;
            }
        }
    }
}
