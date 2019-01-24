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
    }
}