namespace Ristlbat17.Disposition.Servants.Events
{
    public class ServantUsed : ServantEvent
    {
        public ServantUsed(Grade grade, string company, string location, int amount) : base(grade, company, location,
            ServantEventType.Used)
        {
            Amount = amount;
        }

        public int Amount { get; set; }
    }
}