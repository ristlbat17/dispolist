namespace Ristlbat17.Disposition.Reporting
{
    public class MaterialReportItemLocation
    {
        public string Location { get; set; }
        public int Stock { get; set; }
        public int Used { get; set; }
        public int Damaged { get; set; }
        public int Available => Stock - Used - Damaged;
    }
}
