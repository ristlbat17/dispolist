using System.Collections.Generic;
using Ristlbat17.Disposition.Servants;

namespace Ristlbat17.Disposition.Reporting
{
    public class ServantReportItemBataillon
    {
        public Grade Grade { get; set; }
        public int Ideal { get; set; }
        public int Stock { get; set; }
        public int Used { get; set; }
        public int Detached { get; set; }
        public int Available => Stock - Used - Detached;

        public List<ServantReportItemCompany> PerCompany { get; set; }
    }
}