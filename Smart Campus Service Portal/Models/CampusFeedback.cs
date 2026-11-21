using System;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace Smart_Campus_Service_Portal.Models
{
    public class CampusFeedback
    {
        [BsonId]
        [BsonRepresentation(BsonType.ObjectId)]
        public string Id { get; set; }

        public string StudentId { get; set; }

        public string StudentName { get; set; }

        public string ServiceId { get; set; }

        public string ServiceName { get; set; }

        public string Category { get; set; }

        public int Rating { get; set; } = 5; // 1 to 5 stars

        public string Comments { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}
