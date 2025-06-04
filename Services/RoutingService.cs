using int_order_router.Models;
using int_order_router.Services.Interfaces;

namespace int_order_router.Services;

public class RoutingService : IRoutingService
{
   public Task<string> RouteAsync(Edi204Record record)
    {
        // In future: use DB lookup
        if (record.PickupCity?.ToLower() == "chino")
            return Task.FromResult("Trinium");

        return Task.FromResult("Hub");
    }
}
