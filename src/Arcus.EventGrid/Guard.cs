using System;

namespace Arcus.EventGrid
{
    public class Guard
    {
        public static void AgainstNullOrEmptyValue(string argumentValue, string argumentName, string errorMessage = null)
        {
            if (string.IsNullOrWhiteSpace(argumentValue))
            {
                throw new ArgumentException(errorMessage ?? $"The argument {argumentName} was empty or null");
            }
        }

        public static void ForCondition(Func<bool> condition, string errorMessage)
        {
            if (!condition())
            {
                throw new ArgumentException(errorMessage);
            }
        }
    }
}