using System;

namespace Arcus.EventGrid.Publishing.Interfaces
{
    /// <summary>
    /// Intermediary builder contract after the <see cref="EventGridPublisherBuilder.ForTopic"/> is called.
    /// </summary>
    /// <remarks>
    /// This interface is not explicitly necessary at the moment but could be after there exists another correct way of creating 
    /// <see cref="EventGridPublisherBuilderResult"/> instances.
    /// </remarks>
    internal interface IEventGridPublisherBuilderWithAuthenticationKey
    {
        /// <summary>
        /// Specifies the <paramref name="authenticationKey"/> 
        /// for the custom Event Grid topic for whcih a <see cref="EventGridPublisher"/> will be created.
        /// </summary>
        /// <param name="authenticationKey">Authentication key for the custom Event Grid topic</param>
        /// <exception cref="ArgumentException">The authentication key must not be empty and is required</exception>
        /// <returns>
        /// Finilized builder result that can directly create <see cref="EventGridPublisher"/> instances 
        /// via the <see cref="IBuilder.Build()"/> method or extend the publisher even further.
        /// </returns>
        IBuilder UsingAuthenticationKey(string authenticationKey);
    }
}