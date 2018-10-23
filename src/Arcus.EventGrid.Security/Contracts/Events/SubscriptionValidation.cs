using Arcus.EventGrid.Contracts;

namespace Arcus.EventGrid.Security.Contracts.Events
{
    public class SubscriptionValidation : Event<SubscriptionEventData>
    {
        public SubscriptionValidation()
        {
        }

        public SubscriptionValidation(string id) : base(id)
        {
        }

        public SubscriptionValidation(string id, string subject) : base(id, subject)
        {
        }

        public override string DataVersion { get;  } = "1";
        public override string EventType { get;  } = "Microsoft.EventGrid.SubscriptionValidationEvent";
    }
}