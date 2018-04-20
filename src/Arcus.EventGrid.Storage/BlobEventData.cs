using Codit.EventGrid.Blobs;

namespace Arcus.EventGrid.Storage
{
    public class BlobEventData
    {
        public string Api { get; set; }
        public string ClientRequestId { get; set; }
        public string RequestId { get; set; }
        public string ETag { get; set; }
        public string ContentType { get; set; }
        public int ContentLength { get; set; }
        public string BlobType { get; set; }
        public string Url { get; set; }
        public string Sequencer { get; set; }
        public Storagediagnostics StorageDiagnostics { get; set; }

    }
}
