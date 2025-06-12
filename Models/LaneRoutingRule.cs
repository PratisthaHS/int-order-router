namespace int_order_router.Models
{
    public class LaneRoutingRuleModel
    {
        public string CustomerName { get; set; } = default!;
        public string PickUpCity { get; set; } = default!;
        public string DropOffCity { get; set; } = default!;
        public string RouteTo { get; set; } = default!;
        public string? CreatedBy { get; set; }
    }
}
