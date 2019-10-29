using Arcus.EventGrid.Contracts;
using Arcus.EventGrid.Security.Contracts.Events.v1.Data;
using Microsoft.Azure.EventGrid.Models;
using System;

namespace Arcus.EventGrid.Security.Contracts.Events.v1
{
    [Obsolete(
        "Azure Event Grid events are now being used in favor of specific Arcus event types, use " 
        + nameof(EventGridEvent<SubscriptionValidationEventData>) + "<" + nameof(SubscriptionValidationEventData) + "> instead")]
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