namespace Ristlbat17.Disposition.Material.Events
{
    public class MaterialUsed : MaterialEvent
    {
        public int Amount { get; set; }


        public MaterialUsed(string sapNr, string company, int amount, string location) 
            : base(sapNr, company, location, MaterialEventType.Used)
        {
            Amount = amount;
        }
    }
}