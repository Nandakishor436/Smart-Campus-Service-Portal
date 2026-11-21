using System;
using System.Collections.Generic;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace Smart_Campus_Service_Portal.Models
{
    public class CampusService
    {
        [BsonId]
        [BsonRepresentation(BsonType.ObjectId)]
        public string Id { get; set; }

        public string ServiceCode { get; set; }

        public string Name { get; set; }

        public string Category { get; set; }

        public string Description { get; set; }

        public string Icon { get; set; }

        public int EstimatedDays { get; set; } = 3;

        public string Department { get; set; }

        public bool IsActive { get; set; } = true;

        public List<ServiceFormField> FormFields { get; set; } = new List<ServiceFormField>();

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    }

    public class ServiceFormField
    {
        public string Name { get; set; }
        public string Label { get; set; }
        public string FieldType { get; set; } // text, textarea, select, date, number
        public bool IsRequired { get; set; } = true;
        public string Placeholder { get; set; }
        public List<string> Options { get; set; } = new List<string>();
    }
}
