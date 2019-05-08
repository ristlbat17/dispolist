using System;

namespace Ristlbat17.Disposition.Servants.Events
{
    public abstract class ServantEvent {
        public ServantEvent(Grade grade, string company, string location, ServantEventType eventType)
        {
            Company = company;
            EventType = eventType;
            Location = location;
            TimeStamp = DateTime.UtcNow;
        }

        public Grade Grade { get; set; }

        public string Company { get; set; }

        public DateTime TimeStamp { get; set; }

        public string Location { get; set; }

        public ServantEventType EventType { get; set; }
    }
}