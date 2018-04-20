using System;

namespace Arcus.EventGrid.Publishing
{
    internal class Guard
    {
        public static void AgainstNullOrEmptyValue(string argumentValue, string argumentName, string errorMessage = null)
        {
            if (string.IsNullOrWhiteSpace(argumentValue))
            {
                throw new ArgumentException(errorMessage ?? $"The argument {argumentName} was empty or null");
            }
        }
    }
}