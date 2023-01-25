using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Azure;
using Azure.Messaging;
using Azure.Messaging.EventGrid;

namespace Arcus.EventGrid.Tests.Unit.Publishing.Fixtures
{
    public class InMemoryEventGridPublisherClient : EventGridPublisherClient
    {
        private readonly ICollection<ReadOnlyMemory<byte>> _encodedCloudEvents = new Collection<ReadOnlyMemory<byte>>();
        private readonly ICollection<CloudEvent> _cloudEvents = new Collection<CloudEvent>();
        private readonly ICollection<BinaryData> _customEvents = new Collection<BinaryData>();
        private readonly ICollection<EventGridEvent> _eventGridEvents = new Collection<EventGridEvent>();

        public IEnumerable<ReadOnlyMemory<byte>> EncodedCloudEvents => _encodedCloudEvents.AsEnumerable();
        public IEnumerable<CloudEvent> CloudEvents => _cloudEvents.AsEnumerable();
        public IEnumerable<BinaryData> CustomEvents => _customEvents.AsEnumerable();
        public IEnumerable<EventGridEvent> EventGridEvents => _eventGridEvents.AsEnumerable();

        public override Response SendEncodedCloudEvents(ReadOnlyMemory<byte> cloudEvents,
                                                        CancellationToken cancellationToken = new CancellationToken())
        {
            _encodedCloudEvents.Add(cloudEvents);
            return null;
        }

        public override Task<Response> SendEncodedCloudEventsAsync(ReadOnlyMemory<byte> cloudEvents,
                                                                   CancellationToken cancellationToken = new CancellationToken())
        {
            _encodedCloudEvents.Add(cloudEvents);
            return Task.FromResult<Response>(null);
        }

        public override Response SendEvent(EventGridEvent eventGridEvent, CancellationToken cancellationToken = new CancellationToken())
        {
            _eventGridEvents.Add(eventGridEvent);
            return null;
        }

        public override Response SendEvent(CloudEvent cloudEvent, CancellationToken cancellationToken = new CancellationToken())
        {
            _cloudEvents.Add(cloudEvent);
            return null;
        }

        public override Response SendEvent(CloudEvent cloudEvent,
                                           string channelName,
                                           CancellationToken cancellationToken = new CancellationToken())
        {
            _cloudEvents.Add(cloudEvent);
            return null;
        }

        public override Response SendEvent(BinaryData customEvent, CancellationToken cancellationToken = new CancellationToken())
        {
            _customEvents.Add(customEvent);
            return null;
        }

        public override Task<Response> SendEventAsync(EventGridEvent eventGridEvent, CancellationToken cancellationToken = new CancellationToken())
        {
            _eventGridEvents.Add(eventGridEvent);
            return Task.FromResult<Response>(null);
        }

        public override Task<Response> SendEventAsync(CloudEvent cloudEvent, CancellationToken cancellationToken = new CancellationToken())
        {
            _cloudEvents.Add(cloudEvent);
            return Task.FromResult<Response>(null);
        }

        public override Task<Response> SendEventAsync(CloudEvent cloudEvent,
                                                      string channelName,
                                                      CancellationToken cancellationToken = new CancellationToken())
        {
            _cloudEvents.Add(cloudEvent);
            return Task.FromResult<Response>(null);
        }

        public override Task<Response> SendEventAsync(BinaryData customEvent, CancellationToken cancellationToken = new CancellationToken())
        {
            _customEvents.Add(customEvent);
            return Task.FromResult<Response>(null);
        }

        public override Response SendEvents(IEnumerable<EventGridEvent> eventGridEvents, CancellationToken cancellationToken = new CancellationToken())
        {
            foreach (EventGridEvent eventGridEvent in eventGridEvents)
            {
                _eventGridEvents.Add(eventGridEvent);
            }

            return null;
        }

        public override Response SendEvents(IEnumerable<CloudEvent> cloudEvents, CancellationToken cancellationToken = new CancellationToken())
        {
            foreach (CloudEvent cloudEvent in cloudEvents)
            {
                _cloudEvents.Add(cloudEvent);
            }

            return null;
        }

        public override Response SendEvents(IEnumerable<CloudEvent> cloudEvents,
                                            string channelName,
                                            CancellationToken cancellationToken = new CancellationToken())
        {
            foreach (CloudEvent cloudEvent in cloudEvents)
            {
                _cloudEvents.Add(cloudEvent);
            }

            return null;
        }

        public override Response SendEvents(IEnumerable<BinaryData> customEvents, CancellationToken cancellationToken = new CancellationToken())
        {
            foreach (BinaryData customEvent in customEvents)
            {
                _customEvents.Add(customEvent);
            }
            return null;
        }

        public override Task<Response> SendEventsAsync(IEnumerable<EventGridEvent> eventGridEvents, CancellationToken cancellationToken = new CancellationToken())
        {
            foreach (EventGridEvent eventGridEvent in eventGridEvents)
            {
                _eventGridEvents.Add(eventGridEvent);
            }

            return Task.FromResult<Response>(null);
        }

        public override Task<Response> SendEventsAsync(IEnumerable<CloudEvent> cloudEvents, CancellationToken cancellationToken = new CancellationToken())
        {
            foreach (CloudEvent cloudEvent in cloudEvents)
            {
                _cloudEvents.Add(cloudEvent);
            }

            return Task.FromResult<Response>(null);
        }

        public override Task<Response> SendEventsAsync(IEnumerable<CloudEvent> cloudEvents,
                                                       string channelName,
                                                       CancellationToken cancellationToken = new CancellationToken())
        {
            foreach (CloudEvent cloudEvent in cloudEvents)
            {
                _cloudEvents.Add(cloudEvent);
            }

            return Task.FromResult<Response>(null);
        }

        public override Task<Response> SendEventsAsync(IEnumerable<BinaryData> customEvents, CancellationToken cancellationToken = new CancellationToken())
        {
            foreach (BinaryData customEvent in customEvents)
            {
                _customEvents.Add(customEvent);
            }

            return Task.FromResult<Response>(null);
        }
    }
}