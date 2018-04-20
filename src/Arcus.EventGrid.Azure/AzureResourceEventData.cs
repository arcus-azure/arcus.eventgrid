namespace Arcus.EventGrid.Azure
{

    public class AzureResourceEventData
    {
        public string Authorization { get; set; }
        public string Claims { get; set; }
        public string CorrelationId { get; set; }
        public string HttpRequest { get; set; }
        public string ResourceProvider { get; set; }
        public string ResourceUri { get; set; }
        public string OperationName { get; set; }
        public string Status { get; set; }
        public string SubscriptionId { get; set; }
        public string TenantId { get; set; }
    }


}
