using int_order_router.Models;
using int_order_router.Services.Interfaces;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Logging;

namespace int_order_router.Services;

public class RoutingService : IRoutingService
{
    private readonly SqlConnection _connection;
    private readonly ILogger<RoutingService> _logger;

    public RoutingService(SqlConnection connection, ILogger<RoutingService> logger)
    {
        _connection = connection;
        _logger = logger;
    }

    public async Task<string?> RouteAsync(Edi204Record record)
    {

        await _connection.OpenAsync();
        SqlTransaction transaction = _connection.BeginTransaction();

        try
        {
            // Get customer ID from record
            var customerCmd = new SqlCommand("SELECT id FROM customers WHERE name = @customerName", _connection, transaction);
            customerCmd.Parameters.AddWithValue("@customerName", record.RefImNumber);
            var customerIdObj = await customerCmd.ExecuteScalarAsync();
            if (customerIdObj == null)
            {
                _logger.LogWarning($"Customer not found: {record.RefImNumber}");
                transaction.Commit();
                return null;
            }
            int customerId = (int)customerIdObj;

            // Try lane-based routing rule first
            var laneCmd = new SqlCommand(@"
                SELECT TOP 1 id, route_to FROM lane_routing_rules
                WHERE customer_id = @customerId
                  AND pick_up_city = @pickup
                  AND drop_off_city = @dropoff
                  AND is_active = 1
                ORDER BY id DESC", _connection, transaction);

            laneCmd.Parameters.AddWithValue("@customerId", customerId);
            laneCmd.Parameters.AddWithValue("@pickup", record.PickupCity);
            laneCmd.Parameters.AddWithValue("@dropoff", record.DeliveryCity);

            using var laneReader = await laneCmd.ExecuteReaderAsync();
            if (await laneReader.ReadAsync())
            {
                int laneRuleId = laneReader.GetInt32(0);
                string laneRouteTo = laneReader.GetString(1);
                laneReader.Close();

                // Insert into routing_history
                var laneHistoryCmd = new SqlCommand(@"
                    INSERT INTO routing_history
                        (customer_id, mbol, booking_number, container_id, pick_up_city, drop_off_city, routed_to)
                    OUTPUT INSERTED.id
                    VALUES
                        (@custId, @mbol, @bkg, @cont, @pickup, @drop, @route)", _connection, transaction);

                laneHistoryCmd.Parameters.AddWithValue("@custId", customerId);
                laneHistoryCmd.Parameters.AddWithValue("@mbol", record.ShipmentId);
                laneHistoryCmd.Parameters.AddWithValue("@bkg", record.ShipmentId);
                laneHistoryCmd.Parameters.AddWithValue("@cont", record.RefImNumber);
                laneHistoryCmd.Parameters.AddWithValue("@pickup", record.PickupCity);
                laneHistoryCmd.Parameters.AddWithValue("@drop", record.DeliveryCity);
                laneHistoryCmd.Parameters.AddWithValue("@route", laneRouteTo);

                int laneRouteId = (int)await laneHistoryCmd.ExecuteScalarAsync();

                // Audit log for lane rule
                var auditCmd = new SqlCommand(@"
                    INSERT INTO lane_rule_audit_log (route_id, rule_id, notes)
                    VALUES (@routeId, @ruleId, @notes)", _connection, transaction);

                auditCmd.Parameters.AddWithValue("@routeId", laneRouteId);
                auditCmd.Parameters.AddWithValue("@ruleId", laneRuleId);
                auditCmd.Parameters.AddWithValue("@notes", $"Used lane routing rule for customer {record.RefImNumber} from {record.PickupCity} to {record.DeliveryCity}");

                await auditCmd.ExecuteNonQueryAsync();
                transaction.Commit();
                return laneRouteTo;
            }
            laneReader.Close();

            // Try weekly quota rule if no lane rule matched
            var quotaCmd = new SqlCommand(@"
                SELECT TOP 1 id, weekly_quota, (
                    SELECT COUNT(*) FROM routing_history
                    WHERE customer_id = @custId
                    AND routed_to = @routedToHub
                    AND routed_at >= DATEADD(day, 1 - DATEPART(weekday, GETDATE()), CAST(GETDATE() AS date))
                ) AS used
                FROM weekly_quota_rules
                WHERE customer_id = @custId AND is_active = 1
                ORDER BY id DESC", _connection, transaction);

            quotaCmd.Parameters.AddWithValue("@custId", customerId);
            quotaCmd.Parameters.AddWithValue("@routedToHub", "Hub");

            using var quotaReader = await quotaCmd.ExecuteReaderAsync();
            int? quotaRuleId = null;
            int quota = 0;
            int used = 0;
            string routeTo = "Trinium";

            if (await quotaReader.ReadAsync())
            {
                quotaRuleId = quotaReader.GetInt32(0);
                quota = quotaReader.GetInt32(1);
                used = quotaReader.GetInt32(2);
                routeTo = used < quota ? "Hub" : "Trinium";
                return routeTo;
            }
            quotaReader.Close();

            var historyCmd = new SqlCommand(@"
                INSERT INTO routing_history
                    (customer_id, mbol, booking_number, container_id, pick_up_city, drop_off_city, routed_to)
                OUTPUT INSERTED.id
                VALUES
                    (@custId, @mbol, @bkg, @cont, @pickup, @drop, @route)", _connection, transaction);

            historyCmd.Parameters.AddWithValue("@custId", customerId);
            historyCmd.Parameters.AddWithValue("@mbol", record.ShipmentId);
            historyCmd.Parameters.AddWithValue("@bkg", record.ShipmentId);
            historyCmd.Parameters.AddWithValue("@cont", record.ContainerId);
            historyCmd.Parameters.AddWithValue("@pickup", record.PickupCity);
            historyCmd.Parameters.AddWithValue("@drop", record.DeliveryCity);
            historyCmd.Parameters.AddWithValue("@route", routeTo);

            int routeId = (int)await historyCmd.ExecuteScalarAsync();

            if (quotaRuleId.HasValue)
            {
                var auditCmd = new SqlCommand(@"
                INSERT INTO quota_rule_audit_log (route_id, rule_id, notes)
                VALUES (@routeId, @ruleId, @notes)", _connection, transaction);

                auditCmd.Parameters.AddWithValue("@routeId", routeId);
                auditCmd.Parameters.AddWithValue("@ruleId", quotaRuleId);
                auditCmd.Parameters.AddWithValue("@notes", $"Used quota rule for customer {record.RefImNumber} with weekly quota {quota}, used {used}");

                await auditCmd.ExecuteNonQueryAsync();
                transaction.Commit();
            }                        

            transaction.Commit();
            return null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during routing");
            transaction.Rollback();
            return null;
        }
        finally
        {
            await _connection.CloseAsync();
        }
    }
}
