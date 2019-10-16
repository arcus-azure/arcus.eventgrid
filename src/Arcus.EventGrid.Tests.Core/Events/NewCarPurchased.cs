using System;
using Arcus.EventGrid.Contracts;
using Arcus.EventGrid.Tests.Core.Events.Data;

namespace Arcus.EventGrid.Tests.Core.Events
{
    public class NewCarPurchased : CloudEvent<CarEventData>
    {
        private const string DefaultDataVersion = "1", 
                             DefaultEventType = "Arcus.Samples.Cars.NewCarPurchased";

        public NewCarPurchased(string id, string licensePlate) : this(id, "New purchased car", licensePlate)
        {
        }

        public NewCarPurchased(string id, string subject, string licensePlate) 
            : base(id, subject, new CarEventData(licensePlate), DefaultDataVersion, DefaultEventType, DefaultDataVersion, DateTime.UtcNow) 
        {
        }
    }
}
