namespace Ristlbat17.Disposition
{

    public class PatchAmount
    {
        public int Amount { get; set; }

        public static PatchAmount With(int amount) => new PatchAmount {Amount = amount};
    }
}