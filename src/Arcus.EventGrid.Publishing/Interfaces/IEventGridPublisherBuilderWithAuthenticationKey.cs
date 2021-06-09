using System;

namespace Arcus.EventGrid.Publishing.Interfaces
{
    /// <summary>
    ///     Represents an intermediary builder contract after the <see cref="EventGridPublisherBuilder.ForTopic(string)"/>
    ///     or <see cref="EventGridPublisherBuilder.ForTopic(Uri)"/> is called.
    /// </summary>
    /// <remarks>
    ///     This interface is not explicitly necessary at the moment
    ///     but could be after there exists another correct way of creating <see cref="EventGridPublisherBuilderResult"/> instances.
    /// </remarks>
    internal interface IEventGridPublisherBuilderWithAuthenticationKey
    {
        /// <summary>
        /// Specifies the <paramref name="authenticationKey"/> for the custom Event Grid topic for Which a <see cref="IEventGridPublisher"/> will be created.
        /// </summary>
        /// <param name="authenticationKey">The authentication key for the custom Azure Event Grid topic.</param>
        /// <exception cref="ArgumentException">Thrown when the <paramref name="authenticationKey"/> is blank.</exception>
        IEventGridPublisherBuilderWithExponentialRetry UsingAuthenticationKey(string authenticationKey);
    }
}