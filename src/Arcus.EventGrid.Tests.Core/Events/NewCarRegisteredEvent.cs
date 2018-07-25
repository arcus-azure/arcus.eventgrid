namespace Arcus.EventGrid.Tests.Core.Events
{
    public class NewCarRegisteredEvent
    {
        public NewCarRegisteredEvent(string licensePlate)
        {
            LicensePlate = licensePlate;
        }

        public string LicensePlate { get; }
    }
}