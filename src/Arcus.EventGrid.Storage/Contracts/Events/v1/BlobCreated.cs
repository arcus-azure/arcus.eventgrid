using Arcus.EventGrid.Contracts;
using Arcus.EventGrid.Storage.Contracts.Events.v1.Data;
using Microsoft.Azure.EventGrid.Models;
using System;

namespace Arcus.EventGrid.Storage.Contracts.Events.v1
{
    [Obsolete(
        "Azure Event Grid events are now being used in favor of specific Arcus event types, use " 
        + nameof(EventGridEvent<StorageBlobCreatedEventData>) + "<" + nameof(StorageBlobCreatedEventData) + "> instead")]
    public class BlobCreated : Event<BlobEventData>
    {
        public BlobCreated()
        {
        }

        public BlobCreated(string id) : base(id)
        {
        }

        public BlobCreated(string id, string subject) : base(id, subject)
        {
        }

        public override string DataVersion { get; } = "1";
        public override string EventType { get; } = "Microsoft.Storage.BlobCreated";
    }
}