using System.Collections.Generic;

namespace Ristlbat17.Disposition.Reporting
{
    public class MaterialReportItemBataillon
    {
        public Material.Material Material { get; set; }
        public int Stock { get; set; }
        public int Used { get; set; }
        public int Damaged { get; set; }
        public int Available => Stock - Used - Damaged;

        public List<MaterialReportItemCompany> PerCompany { get; set; }
    }
}