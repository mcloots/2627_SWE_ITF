using ITFPulse.Domain.Common;
using System;
using System.Collections.Generic;
using System.Text;

namespace ITFPulse.Domain.Notifications
{
    internal class Notification
    {
        public Guid Id { get; private set; }
        public NotificationMessage NotificationMessage { get; private set; }

        public CreatedAt CreatedAt { get; private set; }

        public Notification(
            string message,
            DateTimeOffset createdAt,
            DateTimeOffset currentTime)
        {
            Id = Guid.NewGuid();
            NotificationMessage = new NotificationMessage(message);
            CreatedAt = new CreatedAt(createdAt, currentTime);
        }
    }
}