namespace Arcus.EventGrid.IoTHub.Contracts.Events.v1.Data
{
    public class TwinProperties
    {
        public PropertyConfiguration Desired { get; set; }
        public PropertyConfiguration Reported { get; set; }
    }
}