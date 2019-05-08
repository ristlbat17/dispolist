using System.Collections.Generic;
using System.Linq;
using MongoDB.Bson.Serialization.Attributes;

namespace Ristlbat17.Disposition.Material
{
    [BsonIgnoreExtraElements]
    public class MaterialInventoryItem
    {
        public string SapNr { get; set; }

        [BsonIgnore]
        public int Stock => Distribution?.Sum(inStock => inStock.Stock) ?? 0;
         
        [BsonIgnore]
        public int Used => Distribution?.Sum(inUse => inUse.Used) ?? 0;

        [BsonIgnore]
        public int Damaged => Distribution?.Sum(isDamaged => isDamaged.Damaged) ?? 0;

        [BsonIgnore]
        public int Available => Distribution?.Sum(isAvailable => isAvailable.Available) ?? 0;

        public string Company { get; set; }

        public List<MaterialAllocation> Distribution { get; set; }
    }
}