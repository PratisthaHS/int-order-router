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
            _logger.LogWarning($"Customer not found: {record.RefImNumber}, using fallback customer 'UNKNOWN'");

            // Attempt to get the ID of the fallback customer 'UNKNOWN'
            var fallbackCmd = new SqlCommand("SELECT id FROM customers WHERE name = @unknown", _connection, transaction);
            fallbackCmd.Parameters.AddWithValue("@unknown", "UNKNOWN");
            var fallbackIdObj = await fallbackCmd.ExecuteScalarAsync();

            if (fallbackIdObj == null)
            {
                _logger.LogError("Fallback customer 'UNKNOWN' not found in database. Aborting routing.");
                transaction.Commit();
                return "Trinium";
            }

            int unknownCustomerId = (int)fallbackIdObj;

            var historyCmd = new SqlCommand(@"
                INSERT INTO routing_history
                    (customer_id, mbol, booking_number, container_id, pick_up_city, drop_off_city, routed_to)
                VALUES
                    (@custId, @mbol, @bkg, @cont, @pickup, @drop, @route)", _connection, transaction);

            historyCmd.Parameters.AddWithValue("@custId", unknownCustomerId);
            historyCmd.Parameters.AddWithValue("@mbol", record.ShipmentId);
            historyCmd.Parameters.AddWithValue("@bkg", record.ShipmentId);
            historyCmd.Parameters.AddWithValue("@cont", record.ContainerId);
            historyCmd.Parameters.AddWithValue("@pickup", record.PickupCity);
            historyCmd.Parameters.AddWithValue("@drop", record.DeliveryCity);
            historyCmd.Parameters.AddWithValue("@route", "Trinium");

            await historyCmd.ExecuteNonQueryAsync();
            transaction.Commit();
            return "Trinium";
        }


        int customerId = (int)customerIdObj;
        
        // Check for duplicate order
        var duplicateCheckCmd = new SqlCommand(@"
            SELECT TOP 1 id, routed_to FROM routing_history
            WHERE customer_id = @custId
            AND booking_number = @bkg
            AND container_id = @cont
            AND mbol = @mbol
            AND pick_up_city = @pickup
            AND drop_off_city = @drop
            ORDER BY id DESC", _connection, transaction);

        duplicateCheckCmd.Parameters.AddWithValue("@custId", customerId);
        duplicateCheckCmd.Parameters.AddWithValue("@bkg", record.ShipmentId);
        duplicateCheckCmd.Parameters.AddWithValue("@mbol", record.ShipmentId);
        duplicateCheckCmd.Parameters.AddWithValue("@cont", record.ContainerId);
        duplicateCheckCmd.Parameters.AddWithValue("@pickup", record.PickupCity);
        duplicateCheckCmd.Parameters.AddWithValue("@drop", record.DeliveryCity);

        using var dupReader = await duplicateCheckCmd.ExecuteReaderAsync();
        if (await dupReader.ReadAsync())
        {
            int priorRouteId = dupReader.GetInt32(0);
            string priorRouteTo = dupReader.GetString(1);
            dupReader.Close();

            // Log reused routing
            _logger.LogInformation($"Duplicate detected — reusing previous route: {priorRouteTo} (route_id={priorRouteId})");

            // Insert new history for tracking this duplicate
            var historyReuseCmd = new SqlCommand(@"
                INSERT INTO routing_history
                    (customer_id, mbol, booking_number, container_id, pick_up_city, drop_off_city, routed_to)
                VALUES
                    (@custId, @mbol, @bkg, @cont, @pickup, @drop, @route)", _connection, transaction);

            historyReuseCmd.Parameters.AddWithValue("@custId", customerId);
            historyReuseCmd.Parameters.AddWithValue("@mbol", record.ShipmentId);
            historyReuseCmd.Parameters.AddWithValue("@bkg", record.ShipmentId);
            historyReuseCmd.Parameters.AddWithValue("@cont", record.ContainerId);
            historyReuseCmd.Parameters.AddWithValue("@pickup", record.PickupCity);
            historyReuseCmd.Parameters.AddWithValue("@drop", record.DeliveryCity);
            historyReuseCmd.Parameters.AddWithValue("@route", priorRouteTo);

            await historyReuseCmd.ExecuteNonQueryAsync();
            transaction.Commit();
            return priorRouteTo;
        }
        dupReader.Close();


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
        bool laneMatched = false;
        int laneRuleId = 0;
        string initialRouteTo = "Trinium";

        if (await laneReader.ReadAsync())
        {
            laneMatched = true;
            laneRuleId = laneReader.GetInt32(0);
            initialRouteTo = laneReader.GetString(1);
        }
        laneReader.Close();

        if (!laneMatched)
        {
            // No lane rule, route to Trinium and record history only
            _logger.LogInformation($"No lane rule matched for customer: {record.RefImNumber}, routing to Trinium");

            var historyCmd = new SqlCommand(@"
                INSERT INTO routing_history
                    (customer_id, mbol, booking_number, container_id, pick_up_city, drop_off_city, routed_to)
                VALUES
                    (@custId, @mbol, @bkg, @cont, @pickup, @drop, @route)", _connection, transaction);

            historyCmd.Parameters.AddWithValue("@custId", customerId);
            historyCmd.Parameters.AddWithValue("@mbol", record.ShipmentId);
            historyCmd.Parameters.AddWithValue("@bkg", record.ShipmentId);
            historyCmd.Parameters.AddWithValue("@cont", record.ContainerId);
            historyCmd.Parameters.AddWithValue("@pickup", record.PickupCity);
            historyCmd.Parameters.AddWithValue("@drop", record.DeliveryCity);
            historyCmd.Parameters.AddWithValue("@route", "Trinium");

            await historyCmd.ExecuteNonQueryAsync();
            transaction.Commit();
            return "Trinium";
        }

        // Lane rule matched — log history first
        var historyInsertCmd = new SqlCommand(@"
            INSERT INTO routing_history
                (customer_id, mbol, booking_number, container_id, pick_up_city, drop_off_city, routed_to)
            OUTPUT INSERTED.id
            VALUES
                (@custId, @mbol, @bkg, @cont, @pickup, @drop, @route)", _connection, transaction);

        historyInsertCmd.Parameters.AddWithValue("@custId", customerId);
        historyInsertCmd.Parameters.AddWithValue("@mbol", record.ShipmentId);
        historyInsertCmd.Parameters.AddWithValue("@bkg", record.ShipmentId);
        historyInsertCmd.Parameters.AddWithValue("@cont", record.ContainerId);
        historyInsertCmd.Parameters.AddWithValue("@pickup", record.PickupCity);
        historyInsertCmd.Parameters.AddWithValue("@drop", record.DeliveryCity);
        historyInsertCmd.Parameters.AddWithValue("@route", ""); // placeholder

        int historyId = 0;
        string finalRouteTo = "Hub"; // default unless quota is exhausted

        // Compute start of week in PST
        TimeZoneInfo pstZone = TimeZoneInfo.FindSystemTimeZoneById("Pacific Standard Time");
        DateTime nowPst = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, pstZone);
        int daysToSubtract = (int)nowPst.DayOfWeek - 1;
        DateTime weekStartPst = nowPst.Date.AddDays(-daysToSubtract);

        // Quota rule check
        var quotaCmd = new SqlCommand(@"
            SELECT TOP 1 id, weekly_quota, (
                SELECT COUNT(*) FROM routing_history
                WHERE customer_id = @custId
                  AND routed_to = @hub
                  AND routed_at >= @weekStart
            ) AS used
            FROM weekly_quota_rules
            WHERE customer_id = @custId
            ORDER BY id DESC", _connection, transaction);

        quotaCmd.Parameters.AddWithValue("@custId", customerId);
        quotaCmd.Parameters.AddWithValue("@hub", "Hub");
        quotaCmd.Parameters.AddWithValue("@weekStart", weekStartPst);

        using var quotaReader = await quotaCmd.ExecuteReaderAsync();
        int? quotaRuleId = null;
        int used = 0;
        int quota = 0;

        if (await quotaReader.ReadAsync())
        {
            quotaRuleId = quotaReader.GetInt32(0);
            quota = quotaReader.GetInt32(1);
            used = quotaReader.GetInt32(2);

            finalRouteTo = used < quota ? "Hub" : "Trinium";
            _logger.LogInformation($"Quota Rule Found: quota={quota}, used={used}, routedTo={finalRouteTo}");
        }
        else
        {
            finalRouteTo = "Hub"; // no quota rule found, but lane matched
            _logger.LogInformation("No active quota rule found, routing to Hub by default.");
        }
        quotaReader.Close();

        // Set final route in history
        historyInsertCmd.Parameters["@route"].Value = finalRouteTo;
        historyId = (int)await historyInsertCmd.ExecuteScalarAsync();

        // Lane audit
        var laneAuditCmd = new SqlCommand(@"
            INSERT INTO lane_rule_audit_log (route_id, rule_id, notes)
            VALUES (@routeId, @ruleId, @notes)", _connection, transaction);

        laneAuditCmd.Parameters.AddWithValue("@routeId", historyId);
        laneAuditCmd.Parameters.AddWithValue("@ruleId", laneRuleId);
        laneAuditCmd.Parameters.AddWithValue("@notes", $"Used lane rule for customer {record.RefImNumber}, pickup {record.PickupCity}, dropoff {record.DeliveryCity}");

        await laneAuditCmd.ExecuteNonQueryAsync();
        _logger.LogInformation($"Logged lane rule audit for route_id={historyId}");

        // Quota audit
        if (quotaRuleId.HasValue)
        {
            var quotaAuditCmd = new SqlCommand(@"
                INSERT INTO quota_rule_audit_log (route_id, rule_id, notes)
                VALUES (@routeId, @ruleId, @notes)", _connection, transaction);

            quotaAuditCmd.Parameters.AddWithValue("@routeId", historyId);
            quotaAuditCmd.Parameters.AddWithValue("@ruleId", quotaRuleId);
            quotaAuditCmd.Parameters.AddWithValue("@notes", $"Used quota rule for customer {record.RefImNumber}, quota={quota}, used={used}");

            await quotaAuditCmd.ExecuteNonQueryAsync();
            _logger.LogInformation($"Logged quota rule audit for route_id={historyId}");
        }

        transaction.Commit();
        return finalRouteTo;
    }
    catch (Exception ex)
    {
        _logger.LogError(ex, "Error during routing");
        transaction.Rollback();
        return "Trinium";
    }
    finally
    {
        await _connection.CloseAsync();
    }
}

}
