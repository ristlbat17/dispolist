using System;

namespace Ristlbat17.Disposition.Material.Events
{
    public abstract class MaterialEvent {
        public MaterialEvent(string sapNr, string company, string location, MaterialEventType eventType)
        {
            SapNr = sapNr;
            Company = company;
            EventType = eventType;
            Location = location;
            TimeStamp = DateTime.UtcNow;
        }

        public string SapNr { get; set; }

        public string Company { get; set; }

        public DateTime TimeStamp { get; set; }

        public string Location { get; set; }

        public MaterialEventType EventType { get; set; }
    }
}