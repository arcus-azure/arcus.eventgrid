using System;
using Arcus.EventGrid.Tests.Core.Events;
using Microsoft.Rest;
using Xunit;

namespace Arcus.EventGrid.Tests.Unit.Events
{
    public class EventCreationTests
    {
        [Fact]
        public void Event_CreateWithoutId_ShouldFailWithArgumentException()
        {
            // Arrange
            string eventId = null;
            const string licensePlate = "1-TOM-337";
            const string subject = licensePlate;

            // Act & Assert
            Assert.Throws<ValidationException>(() => new NewCarRegistered(eventId, subject, licensePlate));
        }

        [Fact]
        public void Event_CreateWithoutIdAndSubject_ShouldFailWithArgumentException()
        {
            // Arrange
            string eventId = null;
            const string licensePlate = "1-TOM-337";

            // Act & Assert
            Assert.Throws<ValidationException>(() => new NewCarRegistered(eventId, licensePlate));
        }

        [Fact]
        public void Event_CreateWithEmptyId_ShouldSucceed()
        {
            // Arrange
            string eventId = string.Empty;
            const string licensePlate = "1-TOM-337";
            const string subject = licensePlate;

            // Act & Assert
            new NewCarRegistered(eventId, subject, licensePlate);
        }

        [Fact]
        public void Event_CreateWithEmptySubject_ShouldSucceed()
        {
            // Arrange
            string eventId = Guid.NewGuid().ToString();
            string subject = string.Empty;
            const string licensePlate = "1-TOM-337";

            // Act & Assert
            new NewCarRegistered(eventId, subject, licensePlate);
        }

        [Fact]
        public void Event_CreateWithoutSubject_ShouldSucceed()
        {
            // Arrange
            string eventId = Guid.NewGuid().ToString();
            string subject = " ";
            const string licensePlate = "1-TOM-337";

            // Act & Assert
            new NewCarRegistered(eventId, subject, licensePlate);
        }

        [Fact]
        public void Event_CreateWithIdAndSubject_ShouldSucceed()
        {
            // Arrange
            string eventId = Guid.NewGuid().ToString();
            const string licensePlate = "1-TOM-337";
            string subject = licensePlate;

            // Act
            var createdEvent = new NewCarRegistered(eventId, subject, licensePlate);

            // Assert
            Assert.NotNull(createdEvent);
            Assert.Equal(eventId, createdEvent.Id);
            Assert.Equal(subject, createdEvent.Subject);
            Assert.NotNull(createdEvent.Data);
            Assert.Equal(licensePlate, createdEvent.GetPayload().LicensePlate);
            Assert.NotEqual(default(DateTimeOffset), createdEvent.EventTime);
        }

        [Fact]
        public void Event_CreateWithId_ShouldSucceed()
        {
            // Arrange
            string eventId = Guid.NewGuid().ToString();
            const string licensePlate = "1-TOM-337";

            // Act
            var createdEvent = new NewCarRegistered(eventId, licensePlate);

            // Assert
            Assert.NotNull(createdEvent);
            Assert.Equal(eventId, createdEvent.Id);
            Assert.NotNull(createdEvent.Subject);
            Assert.NotNull(createdEvent.Data);
            Assert.Equal(licensePlate, createdEvent.GetPayload().LicensePlate);
            Assert.NotEqual(default(DateTimeOffset), createdEvent.EventTime);
        }
    }
}