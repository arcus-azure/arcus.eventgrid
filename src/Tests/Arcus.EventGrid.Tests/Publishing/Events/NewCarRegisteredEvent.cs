namespace Arcus.EventGrid.Tests.Publishing.Events
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