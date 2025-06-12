using System.Data;
using System.Text.Json;
using int_order_router.Models;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Logging;

namespace int_order_router.Functions;

public class LaneRoutingRuleFunction(SqlConnection connection, ILogger<LaneRoutingRuleFunction> logger)
{
    private readonly SqlConnection _connection = connection;
    private readonly ILogger _logger = logger;

    [Function("GetLaneRoutingRules")]
    public async Task<HttpResponseData> GetAsync([HttpTrigger(AuthorizationLevel.Function, "get", Route = "lane-routing-rule")] HttpRequestData req)
    {
        await _connection.OpenAsync();
        var cmd = new SqlCommand(@"
            SELECT r.id, c.name AS customer_name, r.pick_up_city, r.drop_off_city, r.route_to, r.is_active, r.created_by, r.created_at
            FROM lane_routing_rules r
            JOIN customers c ON r.customer_id = c.id
            WHERE r.is_active = 1
            ORDER BY r.id DESC", _connection);

        var rules = new List<object>();
        using var reader = await cmd.ExecuteReaderAsync();
        while (await reader.ReadAsync())
        {
            rules.Add(new
            {
                Id = reader.GetInt32(0),
                CustomerName = reader.GetString(1),
                PickUpCity = reader.GetString(2),
                DropOffCity = reader.GetString(3),
                RouteTo = reader.GetString(4),
                IsActive = reader.GetBoolean(5),
                CreatedBy = reader.IsDBNull(6) ? null : reader.GetString(6),
                CreatedAt = reader.GetDateTime(7)
            });
        }

        await _connection.CloseAsync();
        var response = req.CreateResponse(System.Net.HttpStatusCode.OK);
        await response.WriteAsJsonAsync(rules);
        return response;
    }

    [Function("PostLaneRoutingRule")]
    public async Task<HttpResponseData> PostAsync(
        [HttpTrigger(AuthorizationLevel.Function, "post", Route = "lane-routing-rule")] HttpRequestData req)
    {
        var payload = await JsonSerializer.DeserializeAsync<LaneRoutingRuleModel>(req.Body, new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        });

        if (payload == null)
        {
            var badResponse = req.CreateResponse(System.Net.HttpStatusCode.BadRequest);
            await badResponse.WriteStringAsync("Invalid request body.");
            return badResponse;
        }

        await _connection.OpenAsync();

        // Get customer ID
        var getCustomerCmd = new SqlCommand("SELECT id FROM customers WHERE name = @name", _connection);
        getCustomerCmd.Parameters.AddWithValue("@name", payload.CustomerName);
        var customerIdObj = await getCustomerCmd.ExecuteScalarAsync();

        if (customerIdObj == null)
        {
            await _connection.CloseAsync();
            var notFoundResponse = req.CreateResponse(System.Net.HttpStatusCode.NotFound);
            await notFoundResponse.WriteStringAsync($"Customer '{payload.CustomerName}' not found.");
            return notFoundResponse;
        }

        int customerId = (int)customerIdObj;

        // Insert rule
        var insertCmd = new SqlCommand(@"
            INSERT INTO lane_routing_rules (customer_id, pick_up_city, drop_off_city, route_to, is_active, created_by)
            VALUES (@custId, @pickup, @dropoff, @routeTo, 1, @createdBy)", _connection);

        insertCmd.Parameters.AddWithValue("@custId", customerId);
        insertCmd.Parameters.AddWithValue("@pickup", payload.PickUpCity);
        insertCmd.Parameters.AddWithValue("@dropoff", payload.DropOffCity);
        insertCmd.Parameters.AddWithValue("@routeTo", payload.RouteTo);
        insertCmd.Parameters.AddWithValue("@createdBy", (object?)payload.CreatedBy ?? DBNull.Value);

        await insertCmd.ExecuteNonQueryAsync();
        await _connection.CloseAsync();

        var okResponse = req.CreateResponse(System.Net.HttpStatusCode.Created);
        await okResponse.WriteStringAsync("Lane routing rule inserted successfully.");
        return okResponse;
    }
}
