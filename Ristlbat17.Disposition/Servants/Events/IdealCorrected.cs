namespace Ristlbat17.Disposition.Servants.Events
{
    public class IdealCorrected : ServantEvent {
        public IdealCorrected(Grade grade, string company, int ideal) : base(grade, company, "", ServantEventType.IdealSet)
        {
            Ideal = ideal;
        }
        public int Ideal { get; set; }
    }
}