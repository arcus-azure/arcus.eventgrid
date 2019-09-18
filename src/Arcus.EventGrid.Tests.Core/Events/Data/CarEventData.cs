using GuardNet;
using System;

namespace Arcus.EventGrid.Tests.Core.Events.Data
{
    /// <summary>
    /// Event data representing a Collection of information about a car.
    /// </summary>
    public class CarEventData : IEquatable<CarEventData>
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="CarEventData"/> class.
        /// </summary>
        /// <param name="licensePlate">The license plate of the car.</param>
        public CarEventData(string licensePlate)
        {
            Guard.NotNullOrWhitespace(licensePlate, nameof(licensePlate));

            LicensePlate = licensePlate;
        }

        /// <summary>
        /// Gets the license plate of the car.
        /// </summary>
        public string LicensePlate { get; }

        /// <summary>
        /// Indicates whether the current object is equal to another object of the same type.
        /// </summary>
        /// <param name="other">An object to compare with this object.</param>
        /// <returns><c>true</c> if the current object is equal to the other parameter; otherwise, <c>false</c>.</returns>
        public bool Equals(CarEventData other)
        {
            if (other is null)
            {
                return false;
            }

            if (ReferenceEquals(this, other))
            {
                return true;
            }

            return LicensePlate.Equals(other.LicensePlate);
        }

        /// <summary>
        /// Indicates whether the current object is equal to another object of the same type.
        /// </summary>
        /// <param name="other">An object to compare with this object.</param>
        /// <returns><c>true</c> if the current object is equal to the other parameter; otherwise, <c>false</c>.</returns>
        public override bool Equals(object obj)
        {
            if (obj is null)
            {
                return false;
            }

            if (ReferenceEquals(this, obj))
            {
                return true;
            }

            return obj is CarEventData other && Equals(other);
        }

        /// <summary>
        /// Serves as the default hash function.
        /// </summary>
        public override int GetHashCode()
        {
            return LicensePlate.GetHashCode();
        }
    }
}