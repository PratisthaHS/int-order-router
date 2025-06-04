using int_order_router.Models;

namespace int_order_router.Services.Interfaces;

public interface IRoutingService
{
    Task<string> RouteAsync(Edi204Record record);
}
