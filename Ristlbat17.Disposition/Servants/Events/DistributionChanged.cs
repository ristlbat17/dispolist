namespace Ristlbat17.Disposition.Servants.Events
{
    public class DistributionChanged : ServantEvent
    {
        public DistributionChanged(Grade grade, string company, string location, ServantAllocation allocation) : base(
            grade, company, location, ServantEventType.DistributionChanged)
        {
            Allocation = allocation;
        }

        public ServantAllocation Allocation { get; set; }
    }
}