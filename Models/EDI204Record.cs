namespace int_order_router.Models;

public class Edi204Record
{
    // HDR fields
    public string? ShipmentId { get; set; }
    public string? EquipmentNumber { get; set; }
    public string? ShipmentDate { get; set; }
    public string? VesselName { get; set; }
    public string? BookingNumber { get; set; }
    public string? PickupLocationName { get; set; }
    public string? PickupAddress { get; set; }
    public string? PickupCity { get; set; }
    public string? PickupState { get; set; }
    public string? PickupZip { get; set; }
    public string? DeliveryLocationName { get; set; }
    public string? DeliveryAddress { get; set; }
    public string? DeliveryCity { get; set; }
    public string? DeliveryState { get; set; }
    public string? DeliveryZip { get; set; }
    public string? Scac { get; set; }
    public string? ContainerNumber { get; set; }
    public string? CustomerReferenceNumber { get; set; }

    // DET fields
    public string? StopSequence { get; set; }
    public string? LoadNumber { get; set; }
    public string? ShipNotLaterThan { get; set; }
    public string? DeliverNoLaterThan { get; set; }
    public string? InternalCustomerNumber { get; set; }
    public string? CustomerOrderNumber { get; set; }
}