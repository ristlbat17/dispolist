using MongoDB.Bson;
using MongoDB.Driver;
using Ristlbat17.Disposition.Material;
using Ristlbat17.Disposition.Reporting;
using Ristlbat17.Disposition.Servants;

namespace Ristlbat17.Disposition
{
    public interface IDispositionContext
    {
        IMongoCollection<BsonDocument> EventJournal { get; }
        IMongoCollection<Company> Companies { get; }
        IMongoCollection<DispositionReport> DispositionReport { get; }
    }

    public interface IMaterialDispositionContext : IDispositionContext
    {
        IMongoCollection<MaterialInventoryItem> MaterialInventory { get; }
        IMongoCollection<Material.Material> Material { get; }
    }

    public interface IServantDispositionContext : IDispositionContext
    {
        IMongoCollection<ServantInventoryItem> ServantInventory { get; }
    }
}