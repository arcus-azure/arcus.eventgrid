using Microsoft.Azure.EventGrid.Models;
using System;

namespace Arcus.EventGrid.Storage.Contracts.Events.v1.Data
{
     [Obsolete(
        "Azure Event Grid events are now being used in favor of specific Arcus event types, use " 
        + nameof(StorageBlobCreatedEventData.StorageDiagnostics) + " for example or any  other  'StorageBlob...' event data models to access the storage diagnostics" )]
    public class StorageDiagnostics
    {
        public string BatchId { get; set; }
    }
}