using int_order_router.Models;

namespace int_order_router.Helpers;

public class Text204Parser
{
    public List<Edi204Record> Parse(string[] lines)
    {
        var records = new List<Edi204Record>();

        for (int i = 0; i < lines.Length; i++)
        {
            var line = lines[i];
            if (!line.StartsWith("HDR")) continue;

            var hdr = line.Split(',');

            string[]? det = null;
            if (i + 1 < lines.Length && lines[i + 1].StartsWith("DET"))
            {
                det = lines[i + 1].Split(',');
            }

            var record = new Edi204Record
            {
                // HDR Fields
                ShipmentId = hdr.ElementAtOrDefault(3),
                EquipmentNumber = hdr.ElementAtOrDefault(21),
                ShipmentDate = hdr.ElementAtOrDefault(4),
                VesselName = hdr.ElementAtOrDefault(5),
                BookingNumber = hdr.ElementAtOrDefault(23),
                PickupLocationName = hdr.ElementAtOrDefault(9),
                PickupAddress = hdr.ElementAtOrDefault(10),
                PickupCity = hdr.ElementAtOrDefault(11),
                PickupState = hdr.ElementAtOrDefault(12),
                PickupZip = hdr.ElementAtOrDefault(13),
                DeliveryLocationName = hdr.ElementAtOrDefault(14),
                DeliveryAddress = hdr.ElementAtOrDefault(15),
                DeliveryCity = hdr.ElementAtOrDefault(16),
                DeliveryState = hdr.ElementAtOrDefault(17),
                DeliveryZip = hdr.ElementAtOrDefault(18),
                Scac = hdr.ElementAtOrDefault(25),
                ContainerNumber = hdr.ElementAtOrDefault(26),
                CustomerReferenceNumber = hdr.ElementAtOrDefault(27),

                // DET Fields
                StopSequence = det?.ElementAtOrDefault(1),
                LoadNumber = det?.ElementAtOrDefault(2),
                ShipNotLaterThan = det?.ElementAtOrDefault(3)?.Replace("Ship Not Later Than Date:", ""),
                DeliverNoLaterThan = det?.ElementAtOrDefault(4)?.Replace("Deliver No Later than Date:", ""),
                InternalCustomerNumber = det?.ElementAtOrDefault(5)?.Replace("Internal Customer Number:", ""),
                CustomerOrderNumber = det?.ElementAtOrDefault(6)?.Replace("Customer Order Number:", "")
            };

            records.Add(record);
        }

        return records;
    }
}
