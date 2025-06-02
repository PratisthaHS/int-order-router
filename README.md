##  Local Development Setup

### Prerequisites

- [.NET 8 SDK](https://dotnet.microsoft.com/en-us/download/dotnet/8.0)
- [Azure Functions Core Tools](https://learn.microsoft.com/en-us/azure/azure-functions/functions-run-local)
- [Azurite](https://github.com/Azure/Azurite)

Install Azurite globally:

```bash
npm install -g azurite
```
### Environment Config

Edit local.settings.json

```json
{
  "IsEncrypted": false,
  "Values": {
    "AzureWebJobsStorage": "UseDevelopmentStorage=true",
    "QueueName": "edi-orders",
    "FUNCTIONS_WORKER_RUNTIME": "dotnet-isolated"
  }
}
```

### Start Azurite

```bash
azurite --silent --location ./azurite
```

### . Run the Function

```bash
func start
```

### Test with Postman
Method: POST
URL: http://localhost:7071/api/http204Ingest
Headers:
    Content-Type: text/plain
Body: raw → text → EDI,ORDER,123,XYZ
