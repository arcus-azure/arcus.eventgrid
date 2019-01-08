using System;

namespace Arcus.EventGrid.Publishing.Interfaces
{
    /// <summary>
    /// Intermediary builder contract after the <see cref="EventGridPublisherBuilder.ForTopic(string)"/>
    /// or <see cref="EventGridPublisherBuilder.ForTopic(Uri)"/> is called.
    /// </summary>
    /// <remarks>
    /// This interface is not explicitly necessary at the moment but could be after there exists another correct way of creating 
    /// <see cref="EventGridPublisherBuilderResult"/> instances.
    /// </remarks>
    internal interface IEventGridPublisherBuilderWithAuthenticationKey
    {
        /// <summary>
        /// Specifies the <paramref name="authenticationKey"/> 
        /// for the custom Event Grid topic for Which a <see cref="EventGridPublisher"/> will be created.
        /// </summary>
        /// <param name="authenticationKey">Authentication key for the custom Event Grid topic</param>
        /// <exception cref="ArgumentException">The authentication key must not be empty and is required</exception>
        /// <returns>
        /// Finalized builder result that can directly create <see cref="EventGridPublisher"/> instances 
        /// via the <see cref="IBuilder.Build()"/> method or extend the publisher even further.
        /// </returns>
        /// <exception cref="ArgumentException">The authentication key must not be empty and is required.</exception>
        IEventGridPublisherBuilderWithExponentialRetry UsingAuthenticationKey(string authenticationKey);
    }
}