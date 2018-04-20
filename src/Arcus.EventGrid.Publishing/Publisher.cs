using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Flurl.Http;

namespace Arcus.EventGrid.Publishing
{
    public class EventGridPublisher
    {
        private string TopicEndpoint { get; set; }
        private string AuthorizationKey { get; set; }

        public static EventGridPublisher Create(string topicEndpoint, string authorizationKey)
        {
            //TODO: should be replaced by Guard package call
            Guard.AgainstNullOrEmptyValue(topicEndpoint, nameof(topicEndpoint), "The topic endpoint must not be empty and is required");
            Guard.AgainstNullOrEmptyValue(authorizationKey, nameof(authorizationKey), "The authorization key must not be empty and is required");
            return new EventGridPublisher
            {
                TopicEndpoint = topicEndpoint,
                AuthorizationKey = authorizationKey
            };
        }

        public async Task Publish<T>(string subject, string eventType, IEnumerable<T> data, string id = null) where T : class
        {
            var eventList = data.Select(eventData => new Event<T>
            {
                Subject = subject,
                EventType = eventType,
                EventTime = DateTime.UtcNow,
                Id = id ?? Guid.NewGuid().ToString(),
                Data = eventData
            }).ToList();

            var response = await TopicEndpoint
                .WithHeader("aeg-sas-key", AuthorizationKey)
                .PostJsonAsync(eventList);

            if (!response.IsSuccessStatusCode)
            {
                throw new ApplicationException($"Event grid publishing failed with status {response.StatusCode} and content {await response.Content.ReadAsStringAsync()}");
            }
        }
    }
}
