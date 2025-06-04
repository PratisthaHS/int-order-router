using int_order_router.Models;

namespace int_order_router.Helpers
{
    public  class Edi204Parser
    {
        public  List<Edi204Record> Parse(string ediContent)
        {
            var lines = ediContent.Split(new[] { "\r\n", "\n", "\r" }, StringSplitOptions.RemoveEmptyEntries);

            var records = new List<Edi204Record>();

            Edi204Record? currentRecord = null;

            foreach (var line in lines)
            {
                var fields = line.Split(',');
                if (fields.Length == 0) continue;

                if (fields[0] == "HDR")
                {
                    currentRecord = new Edi204Record
                    {
                        RecordType = fields.ElementAtOrDefault(0),
                        SiteCode = fields.ElementAtOrDefault(1),
                        JobCategory = fields.ElementAtOrDefault(2),
                        BookingNumber = fields.ElementAtOrDefault(3),
                        DocumentDate = fields.ElementAtOrDefault(4),
                        VesselName = fields.ElementAtOrDefault(5),
                        Voyage = fields.ElementAtOrDefault(6),
                        PickupLocationCode = fields.ElementAtOrDefault(7),
                        PickupName = fields.ElementAtOrDefault(8),
                        PickupAddress1 = fields.ElementAtOrDefault(9),
                        PickupAddress2 = fields.ElementAtOrDefault(10),
                        PickupCity = fields.ElementAtOrDefault(11),
                        PickupState = fields.ElementAtOrDefault(12),
                        PickupZip = fields.ElementAtOrDefault(13),
                        PickupRef = fields.ElementAtOrDefault(14),
                        DeliveryLocationCode = fields.ElementAtOrDefault(15),
                        DeliveryName = fields.ElementAtOrDefault(16),
                        DeliveryAddress1 = fields.ElementAtOrDefault(17),
                        DeliveryAddress2 = fields.ElementAtOrDefault(18),
                        DeliveryCity = fields.ElementAtOrDefault(19),
                        DeliveryState = fields.ElementAtOrDefault(20),
                        DeliveryZip = fields.ElementAtOrDefault(21),
                        DeliveryRef = fields.ElementAtOrDefault(22),
                        CustomerCode = fields.ElementAtOrDefault(23),
                        ShipmentId = fields.ElementAtOrDefault(24),
                        ContainerId = fields.ElementAtOrDefault(25),
                        ContainerSize = fields.ElementAtOrDefault(26),
                        ContainerType = fields.ElementAtOrDefault(27),
                        Contents = fields.ElementAtOrDefault(28),
                        Shipper = fields.ElementAtOrDefault(29),
                        Weight = fields.ElementAtOrDefault(30),
                        ReleaseNumber = fields.ElementAtOrDefault(31),
                        Overweight = fields.ElementAtOrDefault(32),
                        CarrierCode = fields.ElementAtOrDefault(33),
                        Hazmat = fields.ElementAtOrDefault(34),
                        SealNumber = fields.ElementAtOrDefault(35),
                        Notes = fields.ElementAtOrDefault(36),
                        ETA = fields.ElementAtOrDefault(37),
                        Pieces = fields.ElementAtOrDefault(38),
                        HireDehireCode = fields.ElementAtOrDefault(39),
                        DemurrageLFD = fields.ElementAtOrDefault(40),
                        Action = fields.ElementAtOrDefault(41),
                        Stop1Code = fields.ElementAtOrDefault(42),
                        Stop1Name = fields.ElementAtOrDefault(43),
                        Stop1Address1 = fields.ElementAtOrDefault(44),
                        Stop1Address2 = fields.ElementAtOrDefault(45),
                        Stop1City = fields.ElementAtOrDefault(46),
                        Stop1State = fields.ElementAtOrDefault(47),
                        Stop1Zip = fields.ElementAtOrDefault(48),
                        Stop2Code = fields.ElementAtOrDefault(49),
                        Stop2Name = fields.ElementAtOrDefault(50),
                        Stop2Address1 = fields.ElementAtOrDefault(51),
                        Stop2Address2 = fields.ElementAtOrDefault(52),
                        Stop2City = fields.ElementAtOrDefault(53),
                        Stop2State = fields.ElementAtOrDefault(54),
                        Stop2Zip = fields.ElementAtOrDefault(55),
                        LiveLoad = fields.ElementAtOrDefault(56),
                        ScheduledPickupDate = fields.ElementAtOrDefault(57),
                        ScheduledPickupTimeFrom = fields.ElementAtOrDefault(58),
                        ScheduledPickupTimeTo = fields.ElementAtOrDefault(59),
                        ScheduledDeliveryDate = fields.ElementAtOrDefault(60),
                        ScheduledDeliveryTimeFrom = fields.ElementAtOrDefault(61),
                        ScheduledDeliveryTimeTo = fields.ElementAtOrDefault(62),
                        ContainerOwner = fields.ElementAtOrDefault(63),
                        RefImNumber = fields.ElementAtOrDefault(64),
                        CesIeNumber = fields.ElementAtOrDefault(65),
                        ItNumber = fields.ElementAtOrDefault(66),
                        CesCbpNumber = fields.ElementAtOrDefault(67),
                        ShipperRef = fields.ElementAtOrDefault(68),
                        ItemRef = fields.ElementAtOrDefault(69),
                        ItemCustomerReleaseDate = fields.ElementAtOrDefault(70),
                        HouseBill = fields.ElementAtOrDefault(71),
                        OrderEntry = fields.ElementAtOrDefault(72),
                        IsGensetRequired = fields.ElementAtOrDefault(73),
                        ItemTemperature = fields.ElementAtOrDefault(74)
                    };

                    records.Add(currentRecord);
                }
                else if (fields[0] == "DET" && currentRecord != null)
                {
                    var detail = new Edi204DetailRecord
                    {
                        RowNumber = int.TryParse(fields.ElementAtOrDefault(1), out var rowNum) ? rowNum : 0,
                        Fields = fields.Skip(2).ToList()
                    };
                    currentRecord.DetailRecords.Add(detail);
                }
            }

            return records;
        }
    }
}
