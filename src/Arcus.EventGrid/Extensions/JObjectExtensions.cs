using System;
using System.Collections.Generic;
using System.Text;
using CloudNative.CloudEvents;
using GuardNet;

// ReSharper disable once CheckNamespace
namespace Newtonsoft.Json.Linq
{
    /// <summary>
    /// Extensions on the <see cref="JObject"/> object.
    /// </summary>
    public static class JObjectExtensions
    {
        /// <summary>
        /// Determines if a given JSON <paramref name="jObject"/> is considered a <see cref="CloudEvent"/>.
        /// </summary>
        /// <param name="jObject">The raw JSON object.</param>
        /// <returns>
        ///     [true] if the specified <paramref name="jObject"/> is considered a valid <see cref="CloudEvent"/>; [false] otherwise.
        /// </returns>
        public static bool IsCloudEvent(this JObject jObject)
        {
            Guard.NotNull(jObject, nameof(jObject));

            return jObject.ContainsKey(CloudEventAttributes.SpecVersionAttributeName())
                   || jObject.ContainsKey(CloudEventAttributes.SpecVersionAttributeName(CloudEventsSpecVersion.V0_1));
        }
    }
}
