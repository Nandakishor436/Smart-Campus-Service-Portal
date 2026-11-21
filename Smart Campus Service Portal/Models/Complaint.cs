using System;
using System.Collections.Generic;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace Smart_Campus_Service_Portal.Models
{
    public class Complaint
    {
        [BsonId]
        [BsonRepresentation(BsonType.ObjectId)]
        public string Id { get; set; }

        public string ComplaintNumber { get; set; }

        public string StudentId { get; set; }

        public string StudentName { get; set; }

        public string StudentRollNo { get; set; }

        public string Department { get; set; }

        public string Category { get; set; } // Hostel, Mess, Library, Academic, IT, Transport, Infrastructure, Other

        public string Subject { get; set; }

        public string Description { get; set; }

        public string Location { get; set; }

        public string Priority { get; set; } = "Medium"; // Low, Medium, High, Urgent

        /// <summary>
        /// Open, Under Review, In Progress, Resolved, Closed
        /// </summary>
        public string Status { get; set; } = "Open";

        public string AssignedToId { get; set; }

        public string AssignedToName { get; set; }

        public string OfficialResponse { get; set; }

        public DateTime? ResolutionDate { get; set; }

        public List<ComplaintResponseItem> Responses { get; set; } = new List<ComplaintResponseItem>();

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    }

    public class ComplaintResponseItem
    {
        public string ResponderId { get; set; }
        public string ResponderName { get; set; }
        public string Role { get; set; }
        public string Message { get; set; }
        public DateTime Timestamp { get; set; } = DateTime.UtcNow;
    }
}
