namespace int_order_router.Models
{
    public class Edi204Record
    {
        public string? RecordType { get; set; } // HDR
        public string? SiteCode { get; set; }
        public string? JobCategory { get; set; }
        public string? BookingNumber { get; set; }
        public string? DocumentDate { get; set; }
        public string? VesselName { get; set; }
        public string? Voyage { get; set; }

        public string? PickupLocationCode { get; set; }
        public string? PickupName { get; set; }
        public string? PickupAddress1 { get; set; }
        public string? PickupAddress2 { get; set; }
        public string? PickupCity { get; set; }
        public string? PickupState { get; set; }
        public string? PickupZip { get; set; }
        public string? PickupRef { get; set; }

        public string? DeliveryLocationCode { get; set; }
        public string? DeliveryName { get; set; }
        public string? DeliveryAddress1 { get; set; }
        public string? DeliveryAddress2 { get; set; }
        public string? DeliveryCity { get; set; }
        public string? DeliveryState { get; set; }
        public string? DeliveryZip { get; set; }
        public string? DeliveryRef { get; set; }

        public string? CustomerCode { get; set; }
        public string? CustomerName { get; set; }
        public string? ShipmentId { get; set; } // Customer Ref

        public string? ContainerId { get; set; }
        public string? ContainerSize { get; set; }
        public string? ContainerType { get; set; }
        public string? Contents { get; set; }
        public string? Shipper { get; set; }
        public string? Weight { get; set; }
        public string? ReleaseNumber { get; set; }
        public string? Overweight { get; set; }
        public string? CarrierCode { get; set; }
        public string? Hazmat { get; set; }
        public string? SealNumber { get; set; }
        public string? Notes { get; set; }

        public string? ETA { get; set; }
        public string? Pieces { get; set; }
        public string? HireDehireCode { get; set; }
        public string? DemurrageLFD { get; set; }
        public string? Action { get; set; }

        public string? Stop1Code { get; set; }
        public string? Stop1Name { get; set; }
        public string? Stop1Address1 { get; set; }
        public string? Stop1Address2 { get; set; }
        public string? Stop1City { get; set; }
        public string? Stop1State { get; set; }
        public string? Stop1Zip { get; set; }

        public string? Stop2Code { get; set; }
        public string? Stop2Name { get; set; }
        public string? Stop2Address1 { get; set; }
        public string? Stop2Address2 { get; set; }
        public string? Stop2City { get; set; }
        public string? Stop2State { get; set; }
        public string? Stop2Zip { get; set; }

        public string? LiveLoad { get; set; }
        public string? ScheduledPickupDate { get; set; }
        public string? ScheduledPickupTimeFrom { get; set; }
        public string? ScheduledPickupTimeTo { get; set; }
        public string? ScheduledDeliveryDate { get; set; }
        public string? ScheduledDeliveryTimeFrom { get; set; }
        public string? ScheduledDeliveryTimeTo { get; set; }

        public string? ContainerOwner { get; set; }
        public string? RefImNumber { get; set; }
        public string? CesIeNumber { get; set; }
        public string? ItNumber { get; set; }
        public string? CesCbpNumber { get; set; }

        public string? ShipperRef { get; set; }
        public string? ItemRef { get; set; }
        public string? ItemCustomerReleaseDate { get; set; }
        public string? HouseBill { get; set; }
        public string? OrderEntry { get; set; }
        public string? IsGensetRequired { get; set; }
        public string? ItemTemperature { get; set; }

        public List<Edi204DetailRecord> DetailRecords { get; set; } = new();
    }

    public class Edi204DetailRecord
    {
        public int RowNumber { get; set; }
        public List<string?> Fields { get; set; } = new();
    }
}
