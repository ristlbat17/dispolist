using System.Collections.Generic;
using System.Linq;
using MongoDB.Bson.Serialization.Attributes;

namespace Ristlbat17.Disposition.Servants
{
    [BsonIgnoreExtraElements]
    public class ServantInventoryItem
    {
        public string Company { get; set; }

        public Grade Grade { get; set; }

        public int Ideal { get; set; }

        [BsonIgnore]
        public int Stock => Distribution?.Sum(inStock => inStock.Stock) ?? 0;

        [BsonIgnore]
        public int Used => Distribution?.Sum(inUse => inUse.Used) ?? 0;

        [BsonIgnore]
        public int Detached => Distribution?.Sum(isDamaged => isDamaged.Detached) ?? 0;

        [BsonIgnore]
        public int Available => Distribution?.Sum(isAvailable => isAvailable.Available) ?? 0;

        public List<ServantAllocation> Distribution { get; set; }
    }
}