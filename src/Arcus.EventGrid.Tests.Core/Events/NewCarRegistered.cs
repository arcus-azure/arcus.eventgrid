using Arcus.EventGrid.Contracts;
using Arcus.EventGrid.Tests.Core.Events.Data;
using GuardNet;

namespace Arcus.EventGrid.Tests.Core.Events
{
    public class NewCarRegistered : EventGridEvent<CarEventData>
    {
        private const string DefaultDataVersion = "1", 
                             DefaultEventType = "Arcus.Samples.Cars.NewCarRegistered";


        private NewCarRegistered()
        {
        }

        public NewCarRegistered(string id, string licensePlate) : this(id, "New registered car", licensePlate)
        {
        }

        public NewCarRegistered(string id, string subject, string licensePlate) 
            : base(id, subject, new CarEventData(licensePlate), DefaultDataVersion, DefaultEventType) 
        {
        }
    }
}