using System;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace Smart_Campus_Service_Portal.Models
{
    public class Announcement
    {
        [BsonId]
        [BsonRepresentation(BsonType.ObjectId)]
        public string Id { get; set; }

        public string Title { get; set; }

        public string Content { get; set; }

        public string Category { get; set; } // Academic, Event, Administrative, Holiday, Examination, Urgent

        /// <summary>
        /// Target role: "All", "Student", "Faculty"
        /// </summary>
        public string TargetRole { get; set; } = "All";

        public string Priority { get; set; } = "Medium"; // Low, Medium, High, Urgent

        public string AuthorName { get; set; } = "Campus Administration";

        public bool IsActive { get; set; } = true;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public DateTime? ExpiryDate { get; set; }
    }
}
