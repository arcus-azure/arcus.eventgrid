namespace Arcus.EventGrid.Storage.Contracts.Events.v1.Data
{
    /// <summary>
    ///     Event data contract for Azure Blob Storage events
    /// </summary>
    public class BlobEventData
    {
        public string Api { get; set; }
        public string BlobType { get; set; }
        public string ClientRequestId { get; set; }
        public int ContentLength { get; set; }
        public string ContentType { get; set; }
        public string ETag { get; set; }
        public string RequestId { get; set; }
        public string Sequencer { get; set; }
        public StorageDiagnostics StorageDiagnostics { get; set; }
        public string Url { get; set; }
    }
}