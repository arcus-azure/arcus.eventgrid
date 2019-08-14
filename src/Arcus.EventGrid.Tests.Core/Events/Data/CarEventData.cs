using GuardNet;

namespace Arcus.EventGrid.Tests.Core.Events.Data
{
    public class CarEventData
    {
        public CarEventData(string licensePlate)
        {
            Guard.NotNullOrWhitespace(licensePlate, nameof(licensePlate));

            LicensePlate = licensePlate;
        }

        public string LicensePlate { get; set; }
    }
}