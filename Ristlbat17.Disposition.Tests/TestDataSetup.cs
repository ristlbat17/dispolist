using Ristlbat17.Disposition.Material;

namespace Ristlbat17.Disposition.Tests
{
    public static class TestDataSetup
    {
        public static void GivenMaterial(this IMaterialDispositionContext context, params Material.Material[] items)
        {
            context.Material.InsertMany(items);
        }
        public static void GivenInventoryItems(this IMaterialDispositionContext context,params MaterialInventoryItem[] items)
        {
            context.MaterialInventory.InsertMany(items);
        }
    }
}