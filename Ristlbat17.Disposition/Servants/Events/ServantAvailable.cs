namespace Ristlbat17.Disposition.Servants.Events
{
    public class ServantAvailable : ServantEvent
    {
        public ServantAvailable(Grade grade, string company, string location, int amount) : base(grade, company,
            location, ServantEventType.Available)
        {
            Amount = amount;
        }

        public int Amount { get; set; }
    }
}