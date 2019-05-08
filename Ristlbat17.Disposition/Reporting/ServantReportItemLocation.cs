namespace Ristlbat17.Disposition.Reporting
{
    public class ServantReportItemLocation
    {
        public string Location { get; set; }
        public int Stock { get; set; }
        public int Used { get; set; }
        public int Detached { get; set; }
        public int Available => Stock - Used - Detached;
    }
}
