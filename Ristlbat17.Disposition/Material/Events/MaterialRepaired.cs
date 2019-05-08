namespace Ristlbat17.Disposition.Material.Events
{
    public class MaterialRepaired : MaterialEvent
    {
        public MaterialRepaired(string sapNr, string location, string company, int amount):
            base(sapNr,company, location, MaterialEventType.Repaired)
        {
            Amount = amount;
        }

        public int Amount { get; set; }
    }
}