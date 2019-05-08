using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace Ristlbat17.Disposition.Material
{
    
    public class Company
    {
        [BsonId]
        [BsonRepresentation(BsonType.ObjectId)]
        public string Id { get; set; }

        [Required]
        public string Name { get; set; }

        [Required]
        public List<Location> Locations { get; set; }

        [Required]
        public Location DefaultLocation { get; set; }

        public static Company With(string name, List<Location> locations) => new Company {Locations = locations, Name = name, DefaultLocation = locations.First()};
    }

    public class Location
    {
        public string Name { get; set; }

        public static Location At(string name) => new Location {Name = name};
    }
}