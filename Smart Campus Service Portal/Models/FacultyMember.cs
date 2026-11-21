using System;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace Smart_Campus_Service_Portal.Models
{
    public class FacultyMember
    {
        [BsonId]
        [BsonRepresentation(BsonType.ObjectId)]
        public string Id { get; set; }

        public string UserId { get; set; }

        public string FacultyId { get; set; }

        public string Department { get; set; }

        public string Designation { get; set; }

        public string Specialization { get; set; }

        public string OfficeRoom { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}
