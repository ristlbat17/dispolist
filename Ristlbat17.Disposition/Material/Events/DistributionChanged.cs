using System.Collections.Generic;

namespace Ristlbat17.Disposition.Material.Events
{
    public class DistributionChanged : MaterialEvent
    {
        public DistributionChanged(string sapNr, string company, string location, MaterialAllocation material) : base(sapNr,
            company,location, MaterialEventType.DistributionChanged)
        {
            Material = material;
        }

        public MaterialAllocation Material { get; set; }
    }
}