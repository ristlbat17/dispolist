namespace Ristlbat17.Disposition.Servants.Events
{
    public class StockCorrected : ServantEvent
    {
        public StockCorrected(Grade grade, string company, string location, int stock) : base(grade, company, location,
            ServantEventType.StockCorrected)
        {
            Stock = stock;
        }

        public int Stock { get; set; }
    }
}