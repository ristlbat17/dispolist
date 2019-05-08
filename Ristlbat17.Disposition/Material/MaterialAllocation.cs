namespace Ristlbat17.Disposition.Material
{
    public class MaterialAllocation
    {
        public string Location { get; set; }
        public int Stock { get; set; }
        public int Used { get; set; }
        public int Damaged { get; set; }

        public static MaterialAllocation For(string location, int stock, int used = 0, int damaged = 0) => new MaterialAllocation
        {
            Location = location,
            Damaged = damaged,
            Stock = stock,
            Used = used
        };

        public int Available => Stock - Used - Damaged;
    }
}