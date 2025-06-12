using System.Net;
using System.Text.Json;
using int_order_router.Models;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Logging;

namespace int_order_router.Functions;

public class CustomerFunction
{
    private readonly SqlConnection _connection;
    private readonly ILogger<CustomerFunction> _logger;

    public CustomerFunction(SqlConnection connection, ILogger<CustomerFunction> logger)
    {
        _connection = connection;
        _logger = logger;
    }

    [Function("CustomerFunction")]
    public async Task<HttpResponseData> Run(
        [HttpTrigger(AuthorizationLevel.Function, "get", "post", Route = "customer")] HttpRequestData req,
        FunctionContext context)
    {
        if (req.Method.Equals("GET", StringComparison.OrdinalIgnoreCase))
        {
            var customers = new List<Customer>();
            await _connection.OpenAsync();

            var cmd = new SqlCommand("SELECT id, name FROM customers", _connection);
            using var reader = await cmd.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                customers.Add(new Customer
                {
                    Name = reader.GetString(1)
                });
            }

            await _connection.CloseAsync();
            var response = req.CreateResponse(HttpStatusCode.OK);
            await response.WriteAsJsonAsync(customers);
            return response;
        }

        if (req.Method.Equals("POST", StringComparison.OrdinalIgnoreCase))
        {
            var customer = await JsonSerializer.DeserializeAsync<Customer>(req.Body, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
            if (customer == null || string.IsNullOrWhiteSpace(customer.Name))
            {
                var badResp = req.CreateResponse(HttpStatusCode.BadRequest);
                await badResp.WriteStringAsync("Customer name is required.");
                return badResp;
            }

            await _connection.OpenAsync();

            var insertCmd = new SqlCommand("INSERT INTO customers (name) VALUES (@name)", _connection);
            insertCmd.Parameters.AddWithValue("@name", customer.Name);
            await insertCmd.ExecuteNonQueryAsync();

            await _connection.CloseAsync();

            var okResp = req.CreateResponse(HttpStatusCode.OK);
            await okResp.WriteStringAsync($"Customer '{customer.Name}' added.");
            return okResp;
        }

        return req.CreateResponse(HttpStatusCode.MethodNotAllowed);
    }
}
