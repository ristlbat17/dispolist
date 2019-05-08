namespace Ristlbat17.Disposition.Servants.Events
{
    public class ServantDetached : ServantEvent
    {
        public ServantDetached(Grade grade, string company, string location, int amount) : base(grade, company,
            location, ServantEventType.Detached)
        {
            Amount = amount;
        }


        public int Amount { get; set; }
    }
}