using System;

namespace Arcus.EventGrid
{
    public class Guard
    {
        //TODO : either use a common 3rd party library, or move to central Arcus library
        /// <summary>
        /// Validates the argument to make sure the string is not null or empty
        /// </summary>
        /// <param name="argumentValue">String to validate</param>
        /// <param name="argumentName">Name of the argument</param>
        /// <param name="errorMessage">Optional custom error message</param>
        /// <exception cref="ArgumentException">Exception thrown, when <paramref name="argumentValue"/> is null or empty</exception>
        public static void AgainstNullOrEmptyValue(string argumentValue, string argumentName, string errorMessage = null)
        {
            if (string.IsNullOrWhiteSpace(argumentValue))
            {
                throw new ArgumentException(errorMessage ?? $"The argument {argumentName} was empty or null");
            }
        }

        /// <summary>
        /// Validates the specific condition function to return true
        /// </summary>
        /// <param name="condition">Delegate that should return true</param>
        /// <param name="errorMessage">Custom error message</param>
        /// <exception cref="ArgumentException">Exception thrown, when <paramref name="condition"/> evalutes to false</exception>
        public static void ForCondition(Func<bool> condition, string errorMessage)
        {
            if (!condition())
            {
                throw new ArgumentException(errorMessage);
            }
        }
    }
}