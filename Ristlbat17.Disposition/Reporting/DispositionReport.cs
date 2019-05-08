using System;
using System.Collections.Generic;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace Ristlbat17.Disposition.Reporting
{
    public class DispositionReport
    {
        [BsonId]
        [BsonRepresentation(BsonType.ObjectId)]
        public string Id { get; set; }

        public DateTime ReportDate { get; set; }

        public List<MaterialReportItemBataillon> MaterialReportItems { get; set; } = new List<MaterialReportItemBataillon>();
        
        public List<ServantReportItemBataillon> ServantReportItems { get; set; } = new List<ServantReportItemBataillon>();

        public ReportType Type { get; set; }
    }
}