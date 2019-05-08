namespace Ristlbat17.Disposition.Material.Events
{
    public class StockCorrected : MaterialEvent
    {
        public int Stock { get; set; }

        public StockCorrected(string sapNr, string company, string location,int stock) 
            : base(sapNr, company, location, MaterialEventType.StockCorrected)
        {
            Stock = stock;
        }
    }
}