using System;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace Smart_Campus_Service_Portal.Models
{
    public class Student
    {
        [BsonId]
        [BsonRepresentation(BsonType.ObjectId)]
        public string Id { get; set; }

        public string UserId { get; set; }

        public string RollNumber { get; set; }

        public string Department { get; set; }

        public string Degree { get; set; }

        public int Semester { get; set; }

        public string AcademicYear { get; set; }

        public string Address { get; set; }

        public string EmergencyContact { get; set; }

        public DateTime DateOfBirth { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}
