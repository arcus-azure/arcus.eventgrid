namespace Arcus.EventGrid.IoTHub.Contracts
{
    public class TwinProperties
    {
        public PropertyConfiguration Desired { get; set; }
        public PropertyConfiguration Reported { get; set; }
    }
}