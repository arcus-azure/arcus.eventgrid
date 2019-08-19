using GuardNet;
using Newtonsoft.Json.Linq;

namespace System
{
    public static class StringExtensions
    {
        /// <summary>
        ///     Determines if the string is a valid JSON payload
        /// </summary>
        /// <param name="jsonPayload">Payload to determine if it's a valid JSON</param>
        public static bool IsValidJson(this string jsonPayload)
        {
            Guard.NotNullOrEmpty(jsonPayload, nameof(jsonPayload));

            try
            {
                JToken.Parse(jsonPayload);
                return true;
            }
            catch (Exception)
            {
                return false;
            }
        }

        /// <summary>
        ///     Tries to parse the given string to a valid JSON payload.
        /// </summary>
        /// <param name="jsonPayload">Payload to determine if it's a valid JSON</param>
        /// <param name="payload">The deserialized JSON payload.</param>
        public static bool TryParseJson(this string jsonPayload, out JToken payload)
        {
            Guard.NotNullOrEmpty(jsonPayload, nameof(jsonPayload));

            try
            {
                payload = JToken.Parse(jsonPayload);
                return true;
            }
            catch (Exception)
            {
                payload = null;
                return false;
            }
        }
    }
}