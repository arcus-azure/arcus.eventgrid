using GuardNet;
using Newtonsoft.Json.Linq;

// ReSharper disable once CheckNamespace
namespace System
{
    /// <summary>
    /// Extensions on the <c>string</c> type related to event interaction.
    /// </summary>
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