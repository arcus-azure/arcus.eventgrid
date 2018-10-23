﻿using Arcus.EventGrid.Contracts;
using Arcus.EventGrid.Storage.Contracts.Events.Data;

namespace Arcus.EventGrid.Storage.Contracts.Events
{
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

        public override string DataVersion { get; set; } = "1";
        public override string EventType { get; set; } = "Microsoft.Storage.BlobCreated";
    }
}