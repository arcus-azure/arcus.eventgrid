using Arcus.EventGrid.Contracts;
using Microsoft.Azure.EventGrid.Models;
using System;

namespace Arcus.EventGrid.Azure.Contracts.Events.Data
{
    /// <summary>
    ///     Event data contract for Azure resource events
    /// </summary>
    [Obsolete(
        "Azure Event Grid events are now being used in favor of specific Arcus event types, use " 
        + nameof(EventGridEvent<ResourceActionSuccessData>) + "<" + nameof(ResourceActionSuccessData) + "> for example or any other 'Resource...' event data models" )]
    public class AzureResourceEventData
    {
        public string Authorization { get; set; }
        public string Claims { get; set; }
        public string CorrelationId { get; set; }
        public string HttpRequest { get; set; }
        public string OperationName { get; set; }
        public string ResourceProvider { get; set; }
        public string ResourceUri { get; set; }
        public string Status { get; set; }
        public string SubscriptionId { get; set; }
        public string TenantId { get; set; }
    }
}