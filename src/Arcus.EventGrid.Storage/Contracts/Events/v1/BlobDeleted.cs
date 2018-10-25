using Arcus.EventGrid.Contracts;
using Arcus.EventGrid.Storage.Contracts.Events.v1.Data;

namespace Arcus.EventGrid.Storage.Contracts.Events.v1
{
    public class BlobDeleted : Event<BlobEventData>
    {
        public BlobDeleted()
        {
        }

        public BlobDeleted(string id) : base(id)
        {
        }

        public BlobDeleted(string id, string subject) : base(id, subject)
        {
        }

        public override string DataVersion { get;  } = "1";
        public override string EventType { get;  } = "Microsoft.Storage.BlobDeleted";
    }
}