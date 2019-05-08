using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace Ristlbat17.Disposition.Material
{
    public class Material
    {
        [BsonId]
        [BsonRepresentation(BsonType.ObjectId)]
        public string Id { get; set; }

        public string SapNr { get; set; }
        public string Category { get; set; }
        public string ShortDescription { get; set; }
        public string Description { get; set; }

        public static Material With(string sapNr, string description) => new Material {Description = description, SapNr = sapNr};
    }
}