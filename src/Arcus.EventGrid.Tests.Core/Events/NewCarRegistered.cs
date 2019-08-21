using Arcus.EventGrid.Contracts;
using Arcus.EventGrid.Tests.Core.Events.Data;
using GuardNet;
using Newtonsoft.Json;

namespace Arcus.EventGrid.Tests.Core.Events
{
    public class NewCarRegistered : EventGridEvent<CarEventData>
    {
        private const string DefaultDataVersion = "1", 
                             DefaultEventType = "Arcus.Samples.Cars.NewCarRegistered";


        [JsonConstructor]
        public NewCarRegistered(string id, string subject, CarEventData data)
            : base(id, subject, data, DefaultDataVersion, DefaultEventType)
        {
        }

        public NewCarRegistered(string id, string subject, string licensePlate) 
            : this(id, subject, new CarEventData(licensePlate)) 
        {
        }

        public NewCarRegistered(string id, string licensePlate) : this(id, "New registered car", licensePlate)
        {
        }      
    }
}