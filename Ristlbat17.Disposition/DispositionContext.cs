using Microsoft.Extensions.Options;
using MongoDB.Bson;
using MongoDB.Driver;
using Ristlbat17.Disposition.Material;
using Ristlbat17.Disposition.Reporting;
using Ristlbat17.Disposition.Servants;

namespace Ristlbat17.Disposition
{
    public class DispositionContext : IMaterialDispositionContext, IServantDispositionContext
    {
        private readonly IMongoDatabase _db;

        public DispositionContext(IOptions<DbSettings> options)
        {
            var client = new MongoClient(options.Value.ConnectionString);
            _db = client.GetDatabase(options.Value.Database);
            CreateMaterialIndex();
        }

        public DispositionContext(IMongoDatabase db)
        {
            _db = db;
            CreateMaterialIndex();
        }

        public IMongoCollection<BsonDocument> EventJournal => _db.GetCollection<BsonDocument>("eventJournal");
        public IMongoCollection<Company> Companies => _db.GetCollection<Company>("companies");
        public IMongoCollection<DispositionReport> DispositionReport => _db.GetCollection<DispositionReport>("reports");
        public IMongoCollection<MaterialInventoryItem> MaterialInventory => _db.GetCollection<MaterialInventoryItem>("inventory");
        public IMongoCollection<Material.Material> Material => _db.GetCollection<Material.Material>("material");
        public IMongoCollection<ServantInventoryItem> ServantInventory => _db.GetCollection<ServantInventoryItem>("servantInventory");

        private void CreateMaterialIndex()
        {
            Material.Indexes.CreateOne(
                new CreateIndexModel<Material.Material>(
                    Builders<Material.Material>.IndexKeys.Ascending(i => i.SapNr),
                    new CreateIndexOptions<Material.Material>
                    {
                        Unique = true
                    }));
        }
    }
}