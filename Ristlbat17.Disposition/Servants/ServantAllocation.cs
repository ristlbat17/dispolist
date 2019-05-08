namespace Ristlbat17.Disposition.Servants
{
    public class ServantAllocation
    {
        public string Location { get; set; }
        public int Stock { get; set; }
        public int Used { get; set; }
        public int Detached { get; set; }

        public static ServantAllocation For(string company, int stock, int used = 0, int detached = 0) => new ServantAllocation
        {
            Location = company,
            Detached = detached,
            Stock = stock,
            Used = used
        };

        public int Available => Stock - Used - Detached;
    }
}