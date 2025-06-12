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

### Local Database Setup

```bash
docker run -e "ACCEPT_EULA=Y" -e "SA_PASSWORD="yourPassword"" -p 1433:1433 --name sqlserver -d mcr.microsoft.com/mssql/server:2022-latest
```

### Dummy Data

``` SQL
-- Add a customer
INSERT INTO customers (name) VALUES ('WALMCM');

-- Add lane-based routing rules
INSERT INTO lane_routing_rules (customer_id, pick_up_city, drop_off_city, route_to, is_active, created_by)
VALUES 
(1, 'Los Angeles', 'Chicago', 'Hub', 1, 'admin'),
(1, 'Chino', 'Denver', 'Trinium', 1, 'admin');

-- Add a weekly quota rule (e.g., max 2 per week)
INSERT INTO weekly_quota_rules (customer_id, weekly_quota, start_of_week, is_active, created_by)
VALUES 
(1, 3, '2025-06-02', 1, 'admin');

-- Simulate two routes already taken this week for quota testing
INSERT INTO routing_history (customer_id, mbol, booking_number, container_id, pick_up_city, drop_off_city, routed_to)
VALUES (1, 'MBOL000', 'BOOK000', 'CONT000', 'Los Angeles', 'Chicago', 'Hub');

INSERT INTO routing_history (customer_id, mbol, booking_number, container_id, pick_up_city, drop_off_city, routed_to)
VALUES (1, 'MBOL001', 'BOOK001', 'CONT001', 'Los Angeles', 'Chicago', 'Hub');

```

### . Run the Function

```bash
func start
```


