namespace Ristlbat17.Disposition.Material.Events
{
    public class MaterialDamaged : MaterialEvent
    {
        public MaterialDamaged(string sapNr, string company, string location, int amount) 
            : base(sapNr, company, location,MaterialEventType.Defected)
        {
            Amount = amount;
        }

        public int Amount { get; set; }
    }
}