using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace Arcus.EventGrid.Publishing
{
    public class EventGridPublisher
    {
        private string TopicEndpoint { get; set; }
        private string AuthorizationKey { get; set; }

        public static EventGridPublisher Create(string topicEndpoint, string authorizationKey)
        {
            return new EventGridPublisher
            {
                TopicEndpoint = topicEndpoint,
                AuthorizationKey = authorizationKey
            };
        }

        public async Task Publish<T>(string subject, string eventType, IEnumerable<T> data, string id = null) where T : class
        {
            var client = new HttpClient();
            client.DefaultRequestHeaders.Add("aeg-sas-key", AuthorizationKey);
            var eventList = data.Select(eventData => new Event<T>
            {
                Subject = subject,
                EventType = eventType,
                EventTime = DateTime.UtcNow,
                Id = id ?? Guid.NewGuid().ToString(),
                Data = eventData
            }).ToList();

            var json = JsonConvert.SerializeObject(eventList);
            var request = new HttpRequestMessage(HttpMethod.Post, TopicEndpoint)
            {
                Content = new StringContent(json, Encoding.UTF8, "application/json")
            };

            var response = await client.SendAsync(request);
            if (!response.IsSuccessStatusCode)
            {
                throw new ApplicationException($"Event grid publishing failed with status {response.StatusCode} and content {await response.Content.ReadAsStringAsync()}");
            }
        }
    }
}
