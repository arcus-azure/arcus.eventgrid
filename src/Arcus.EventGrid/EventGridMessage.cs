using System.Collections.Generic;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Arcus.EventGrid
{
    public class EventGridMessage<T>
    {

        public static EventGridMessage<T> Parse(string jsonBody)
        {
            var array = JArray.Parse(jsonBody);
            var result = new EventGridMessage<T>();
            foreach (var eventObject in array.Children<JObject>())
            {
                var gridEvent = JsonConvert.DeserializeObject<Event<T>>(eventObject.ToString());
                result.Events.Add(gridEvent);
            }
            return result;
        }

        public string SessionId { get; set; }
        public List<Event<T>> Events { get; set; } = new List<Event<T>>();
    }
}