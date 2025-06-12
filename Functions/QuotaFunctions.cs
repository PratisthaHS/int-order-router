using System.Net;
using System.Text.Json;
using int_order_router.Models;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Logging;

namespace int_order_router.Functions;

public class QuotaFunctions
{
    private readonly SqlConnection _connection;
    private readonly ILogger<QuotaFunctions> _logger;

    public QuotaFunctions(SqlConnection connection, ILogger<QuotaFunctions> logger)
    {
        _connection = connection;
        _logger = logger;
    }

    [Function("QuotaFunctions")]
    public async Task<HttpResponseData> Run(
        [HttpTrigger(AuthorizationLevel.Function, "get", "post", Route = "quota-rule")] HttpRequestData req,
        FunctionContext context)
    {
        if (req.Method.Equals("GET", StringComparison.OrdinalIgnoreCase))
        {
            var responseList = new List<QuotaRuleResponse>();
            await _connection.OpenAsync();

            var command = new SqlCommand(@"
                SELECT q.weekly_quota, q.updated_at, c.name
                FROM weekly_quota_rules q
                JOIN customers c ON q.customer_id = c.id", _connection);

            using var reader = await command.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                responseList.Add(new QuotaRuleResponse
                {
                    WeeklyQuota = reader.GetInt32(0),
                    UpdatedAt = reader.GetDateTime(1),
                    CustomerName = reader.GetString(2)
                });
            }

            await _connection.CloseAsync();

            var response = req.CreateResponse(HttpStatusCode.OK);
            await response.WriteAsJsonAsync(responseList);
            return response;
        }

        if (req.Method.Equals("POST", StringComparison.OrdinalIgnoreCase))
        {
            var quota = await JsonSerializer.DeserializeAsync<QuotaRule>(req.Body, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

            if (quota is null || string.IsNullOrWhiteSpace(quota.CustomerName) || quota.WeeklyQuota <= 0)
            {
                var badResp = req.CreateResponse(HttpStatusCode.BadRequest);
                await badResp.WriteStringAsync("Invalid payload.");
                return badResp;
            }

            await _connection.OpenAsync();

            // Find customer ID
            var customerCmd = new SqlCommand("SELECT id FROM customers WHERE name = @name", _connection);
            customerCmd.Parameters.AddWithValue("@name", quota.CustomerName);
            var customerIdObj = await customerCmd.ExecuteScalarAsync();

            if (customerIdObj == null)
            {
                await _connection.CloseAsync();
                var notFound = req.CreateResponse(HttpStatusCode.NotFound);
                await notFound.WriteStringAsync("Customer not found.");
                return notFound;
            }

            int customerId = (int)customerIdObj;

            // Check if rule exists
            var checkCmd = new SqlCommand("SELECT COUNT(*) FROM weekly_quota_rules WHERE customer_id = @cid", _connection);
            checkCmd.Parameters.AddWithValue("@cid", customerId);
            bool exists = (int)await checkCmd.ExecuteScalarAsync() > 0;

            SqlCommand cmd;
            if (exists)
            {
                cmd = new SqlCommand(@"
                    UPDATE weekly_quota_rules 
                    SET weekly_quota = @quota, updated_at = GETDATE() 
                    WHERE customer_id = @cid", _connection);
            }
            else
            {
                cmd = new SqlCommand(@"
                    INSERT INTO weekly_quota_rules (customer_id, weekly_quota, created_by) 
                    VALUES (@cid, @quota, @creator)", _connection);
                cmd.Parameters.AddWithValue("@creator", quota.CreatedBy ?? "system");
            }

            cmd.Parameters.AddWithValue("@cid", customerId);
            cmd.Parameters.AddWithValue("@quota", quota.WeeklyQuota);
            await cmd.ExecuteNonQueryAsync();

            // Insert audit log
            var auditCmd = new SqlCommand(@"
                INSERT INTO quota_rule_change_log (customer_id, weekly_quota, changed_by, comment) 
                VALUES (@cid, @quota, @by, @reason)", _connection);
            auditCmd.Parameters.AddWithValue("@cid", customerId);
            auditCmd.Parameters.AddWithValue("@quota", quota.WeeklyQuota);
            auditCmd.Parameters.AddWithValue("@by", quota.CreatedBy ?? "system");
            auditCmd.Parameters.AddWithValue("@reason", quota.Comment ?? "(no reason)");

            await auditCmd.ExecuteNonQueryAsync();
            await _connection.CloseAsync();

            var okResp = req.CreateResponse(HttpStatusCode.OK);
            await okResp.WriteStringAsync("Quota rule updated.");
            return okResp;
        }

        return req.CreateResponse(HttpStatusCode.MethodNotAllowed);
    }
}
