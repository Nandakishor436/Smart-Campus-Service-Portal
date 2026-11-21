using System;
using System.Collections.Generic;
using MongoDB.Driver;
using Smart_Campus_Service_Portal.Helpers;

namespace Smart_Campus_Service_Portal.Models
{
    public static class DatabaseSeeder
    {
        public static void Seed()
        {
            var db = MongoDbContext.Instance;

            // 1. Seed Users & Profiles if empty
            if (db.Users.CountDocuments(FilterDefinition<User>.Empty) == 0)
            {
                // Admin User
                var admin = new User
                {
                    Username = "admin",
                    Email = "admin@campus.edu",
                    PasswordHash = PasswordHasher.HashPassword("Admin@123"),
                    FullName = "Dr. Robert Vance",
                    Role = "Admin",
                    Phone = "+1 555-019-2831",
                    IsActive = true,
                    CreatedAt = DateTime.UtcNow.AddMonths(-6),
                    UpdatedAt = DateTime.UtcNow
                };
                db.Users.InsertOne(admin);

                // Faculty User 1
                var faculty1 = new User
                {
                    Username = "faculty.sharma",
                    Email = "faculty.sharma@campus.edu",
                    PasswordHash = PasswordHasher.HashPassword("Faculty@123"),
                    FullName = "Prof. Rajesh Sharma",
                    Role = "Faculty",
                    Phone = "+1 555-014-9922",
                    IsActive = true,
                    CreatedAt = DateTime.UtcNow.AddMonths(-5),
                    UpdatedAt = DateTime.UtcNow
                };
                db.Users.InsertOne(faculty1);

                db.Faculty.InsertOne(new FacultyMember
                {
                    UserId = faculty1.Id,
                    FacultyId = "FAC-CS-101",
                    Department = "Computer Science",
                    Designation = "Professor & Department Chair",
                    Specialization = "Artificial Intelligence & Distributed Systems",
                    OfficeRoom = "CS Block - Room 304",
                    CreatedAt = DateTime.UtcNow.AddMonths(-5)
                });

                // Faculty User 2
                var faculty2 = new User
                {
                    Username = "faculty.patel",
                    Email = "faculty.patel@campus.edu",
                    PasswordHash = PasswordHasher.HashPassword("Faculty@123"),
                    FullName = "Dr. Anita Patel",
                    Role = "Faculty",
                    Phone = "+1 555-018-7711",
                    IsActive = true,
                    CreatedAt = DateTime.UtcNow.AddMonths(-4),
                    UpdatedAt = DateTime.UtcNow
                };
                db.Users.InsertOne(faculty2);

                db.Faculty.InsertOne(new FacultyMember
                {
                    UserId = faculty2.Id,
                    FacultyId = "FAC-LIB-205",
                    Department = "Library & Information Science",
                    Designation = "Chief Campus Librarian",
                    Specialization = "Digital Archives & Academic Research",
                    OfficeRoom = "Central Library - 2nd Floor",
                    CreatedAt = DateTime.UtcNow.AddMonths(-4)
                });

                // Student User 1
                var student1 = new User
                {
                    Username = "student.alex",
                    Email = "student.alex@campus.edu",
                    PasswordHash = PasswordHasher.HashPassword("Student@123"),
                    FullName = "Alex Johnson",
                    Role = "Student",
                    Phone = "+1 555-012-3456",
                    IsActive = true,
                    CreatedAt = DateTime.UtcNow.AddMonths(-3),
                    UpdatedAt = DateTime.UtcNow
                };
                db.Users.InsertOne(student1);

                db.Students.InsertOne(new Student
                {
                    UserId = student1.Id,
                    RollNumber = "CS-2024-042",
                    Department = "Computer Science",
                    Degree = "B.Tech Computer Science & Engineering",
                    Semester = 4,
                    AcademicYear = "2025-2026",
                    Address = "Room 214, Pine Valley Campus Hostel",
                    EmergencyContact = "+1 555-098-7654 (Parent)",
                    DateOfBirth = new DateTime(2004, 5, 14),
                    CreatedAt = DateTime.UtcNow.AddMonths(-3)
                });

                // Student User 2
                var student2 = new User
                {
                    Username = "student.priya",
                    Email = "student.priya@campus.edu",
                    PasswordHash = PasswordHasher.HashPassword("Student@123"),
                    FullName = "Priya Nair",
                    Role = "Student",
                    Phone = "+1 555-017-6543",
                    IsActive = true,
                    CreatedAt = DateTime.UtcNow.AddMonths(-2),
                    UpdatedAt = DateTime.UtcNow
                };
                db.Users.InsertOne(student2);

                db.Students.InsertOne(new Student
                {
                    UserId = student2.Id,
                    RollNumber = "EE-2023-018",
                    Department = "Electrical Engineering",
                    Degree = "B.Tech Electrical & Electronics",
                    Semester = 6,
                    AcademicYear = "2025-2026",
                    Address = "Room 108, Lakeview Girls Residence",
                    EmergencyContact = "+1 555-087-4321 (Guardian)",
                    DateOfBirth = new DateTime(2003, 9, 21),
                    CreatedAt = DateTime.UtcNow.AddMonths(-2)
                });
            }

            // 2. Seed Campus Services (8 core services)
            if (db.Services.CountDocuments(FilterDefinition<CampusService>.Empty) == 0)
            {
                var services = new List<CampusService>
                {
                    new CampusService
                    {
                        ServiceCode = "SRV-LIB",
                        Name = "Library Services",
                        Category = "Library",
                        Description = "Book reservation, digital archive access, journal requests, study carrel bookings, and library card renewals.",
                        Icon = "fa-book-open",
                        EstimatedDays = 2,
                        Department = "Library & Information Science",
                        IsActive = true,
                        FormFields = new List<ServiceFormField>
                        {
                            new ServiceFormField { Name = "ServiceType", Label = "Service Type", FieldType = "select", Options = new List<string> { "Book Reservation", "Digital Access Pass", "Study Carrel Booking", "Library Card Renewal", "Inter-library Loan" } },
                            new ServiceFormField { Name = "BookTitle", Label = "Book / Resource Title & Author", FieldType = "text", Placeholder = "e.g. Introduction to Algorithms (CLRS)" },
                            new ServiceFormField { Name = "ISBNNumber", Label = "ISBN / Accession Number (if known)", FieldType = "text", IsRequired = false, Placeholder = "Optional" },
                            new ServiceFormField { Name = "Purpose", Label = "Research / Study Purpose", FieldType = "textarea", Placeholder = "Brief details about research or course project" }
                        }
                    },
                    new CampusService
                    {
                        ServiceCode = "SRV-HST",
                        Name = "Hostel Services",
                        Category = "Hostel",
                        Description = "Room allocation, hostel maintenance, mess change requests, night outpass approval, and furniture replacement.",
                        Icon = "fa-hotel",
                        EstimatedDays = 3,
                        Department = "Student Housing & Residential Life",
                        IsActive = true,
                        FormFields = new List<ServiceFormField>
                        {
                            new ServiceFormField { Name = "RequestType", Label = "Request Type", FieldType = "select", Options = new List<string> { "Room Allocation / Shift", "Outpass / Leave Permission", "Mess Category Change", "Hostel Room Maintenance", "Locker Key Reissue" } },
                            new ServiceFormField { Name = "HostelBlock", Label = "Hostel Block & Room Number", FieldType = "text", Placeholder = "e.g. Block B, Room 305" },
                            new ServiceFormField { Name = "Dates", Label = "Relevant Dates / Duration", FieldType = "text", Placeholder = "e.g. 25-Nov-2026 to 28-Nov-2026" },
                            new ServiceFormField { Name = "Details", Label = "Reason / Specific Details", FieldType = "textarea", Placeholder = "State details or justification" }
                        }
                    },
                    new CampusService
                    {
                        ServiceCode = "SRV-TRN",
                        Name = "Transport Services",
                        Category = "Transport",
                        Description = "Campus bus pass application, route modification, shuttle schedule inquiries, and parking permits.",
                        Icon = "fa-bus",
                        EstimatedDays = 2,
                        Department = "Campus Operations & Transport",
                        IsActive = true,
                        FormFields = new List<ServiceFormField>
                        {
                            new ServiceFormField { Name = "ServiceType", Label = "Service Type", FieldType = "select", Options = new List<string> { "Semester Bus Pass", "Route Change Request", "Campus Parking Permit (2-Wheeler)", "Campus Parking Permit (4-Wheeler)", "Special Shuttle Request" } },
                            new ServiceFormField { Name = "RouteNumber", Label = "Bus Route / Pickup Stop", FieldType = "text", Placeholder = "e.g. Route 7 - Downtown Metro to Campus" },
                            new ServiceFormField { Name = "VehicleNumber", Label = "Vehicle Registration No (if parking permit)", FieldType = "text", IsRequired = false, Placeholder = "e.g. KA-05-MN-1234" },
                            new ServiceFormField { Name = "AdditionalNotes", Label = "Additional Notes", FieldType = "textarea", IsRequired = false }
                        }
                    },
                    new CampusService
                    {
                        ServiceCode = "SRV-ITS",
                        Name = "IT Support",
                        Category = "IT Support",
                        Description = "Campus high-speed WiFi registration, student portal / email reset, licensed lab software access, and hardware troubleshooting.",
                        Icon = "fa-laptop-code",
                        EstimatedDays = 1,
                        Department = "Information Technology Services",
                        IsActive = true,
                        FormFields = new List<ServiceFormField>
                        {
                            new ServiceFormField { Name = "IssueCategory", Label = "Issue Category", FieldType = "select", Options = new List<string> { "Campus WiFi Access / MAC Registration", "Student Portal & Email Password Reset", "Licensed Software (MATLAB/AutoCAD)", "Hardware Repair / Diagnostic", "Network Port Activation" } },
                            new ServiceFormField { Name = "DeviceType", Label = "Device Type & OS", FieldType = "text", Placeholder = "e.g. Dell XPS Laptop (Windows 11) / MacBook (macOS)" },
                            new ServiceFormField { Name = "MacAddress", Label = "Device MAC Address", FieldType = "text", IsRequired = false, Placeholder = "e.g. 00:1A:2B:3C:4D:5E" },
                            new ServiceFormField { Name = "IssueDescription", Label = "Detailed Description of Issue", FieldType = "textarea", Placeholder = "Explain the problem or software required" }
                        }
                    },
                    new CampusService
                    {
                        ServiceCode = "SRV-MNT",
                        Name = "Maintenance Requests",
                        Category = "Maintenance",
                        Description = "Civil, electrical, plumbing, HVAC/air conditioning, and carpentry maintenance across campus buildings.",
                        Icon = "fa-tools",
                        EstimatedDays = 2,
                        Department = "Estate Management & Maintenance",
                        IsActive = true,
                        FormFields = new List<ServiceFormField>
                        {
                            new ServiceFormField { Name = "MaintenanceType", Label = "Maintenance Type", FieldType = "select", Options = new List<string> { "Electrical / Lighting", "Air Conditioning / Ventilation", "Plumbing & Restroom", "Carpentry & Furniture", "Civil / Painting / Glass" } },
                            new ServiceFormField { Name = "Location", Label = "Exact Campus Location", FieldType = "text", Placeholder = "e.g. Science Block 2, Lab 102 / Library 1st Floor" },
                            new ServiceFormField { Name = "Urgency", Label = "Urgency Level", FieldType = "select", Options = new List<string> { "Routine (within 3 days)", "Priority (within 24 hours)", "Emergency (Hazardous/Flooding)" } },
                            new ServiceFormField { Name = "ProblemDetails", Label = "Problem Description", FieldType = "textarea", Placeholder = "Describe the defect or repair needed" }
                        }
                    },
                    new CampusService
                    {
                        ServiceCode = "SRV-CRT",
                        Name = "Student Certificates",
                        Category = "Certificates",
                        Description = "Issue of Bonafide Certificates, Fee Structure Receipts, Course Completion Certificates, No Objection Certificates (NOC), and Transcripts.",
                        Icon = "fa-file-signature",
                        EstimatedDays = 3,
                        Department = "Academic Office & Registrar",
                        IsActive = true,
                        FormFields = new List<ServiceFormField>
                        {
                            new ServiceFormField { Name = "CertificateType", Label = "Certificate Required", FieldType = "select", Options = new List<string> { "Bonafide Student Certificate", "Official Academic Transcript", "Course Completion Certificate", "No Objection Certificate (NOC for Internship)", "Fee Structure & Payment Receipt", "Character / Conduct Certificate" } },
                            new ServiceFormField { Name = "Purpose", Label = "Purpose of Certificate", FieldType = "text", Placeholder = "e.g. Passport application / Internship at Tech Corp / Bank Education Loan" },
                            new ServiceFormField { Name = "DeliveryFormat", Label = "Format Preference", FieldType = "select", Options = new List<string> { "Digital Signed PDF", "Physical Printed Copy with Seal", "Both Digital & Physical" } },
                            new ServiceFormField { Name = "Remarks", Label = "Special Instructions", FieldType = "textarea", IsRequired = false }
                        }
                    },
                    new CampusService
                    {
                        ServiceCode = "SRV-EVT",
                        Name = "Event Registration",
                        Category = "Events",
                        Description = "Booking campus auditoriums, seminar halls, open-air theatre, sports grounds, and official student club event permits.",
                        Icon = "fa-calendar-star",
                        EstimatedDays = 4,
                        Department = "Dean of Student Affairs",
                        IsActive = true,
                        FormFields = new List<ServiceFormField>
                        {
                            new ServiceFormField { Name = "EventName", Label = "Event / Activity Name", FieldType = "text", Placeholder = "e.g. Annual Robotics Hackathon 2026" },
                            new ServiceFormField { Name = "VenueRequired", Label = "Venue Required", FieldType = "select", Options = new List<string> { "Main Campus Auditorium (800 seats)", "Mini Seminar Hall A (150 seats)", "Open Air Amphitheatre", "Indoor Sports Complex", "University Cricket / Football Ground" } },
                            new ServiceFormField { Name = "ExpectedAttendees", Label = "Expected Attendees", FieldType = "number", Placeholder = "e.g. 200" },
                            new ServiceFormField { Name = "EventDate", Label = "Proposed Event Date & Timings", FieldType = "text", Placeholder = "e.g. 15-Dec-2026, 9:00 AM - 5:00 PM" },
                            new ServiceFormField { Name = "OrganizingClub", Label = "Student Club / Organizing Body", FieldType = "text", Placeholder = "e.g. IEEE Student Branch / Robotics Club" }
                        }
                    },
                    new CampusService
                    {
                        ServiceCode = "SRV-LAB",
                        Name = "Classroom and Laboratory Support",
                        Category = "Academics",
                        Description = "AV projector support, advanced computing cluster access, specialized electronics equipment reservations, and smart classroom setup.",
                        Icon = "fa-chalkboard-teacher",
                        EstimatedDays = 1,
                        Department = "Academic Laboratories & Technical Services",
                        IsActive = true,
                        FormFields = new List<ServiceFormField>
                        {
                            new ServiceFormField { Name = "SupportType", Label = "Support Type", FieldType = "select", Options = new List<string> { "Projector / Audio-Visual Support", "High-Performance GPU Cluster Slot", "Specialized Hardware Equipment Access", "Weekend Lab Access Permission", "Smart Classroom Setup" } },
                            new ServiceFormField { Name = "LabOrRoom", Label = "Lab Name or Classroom Number", FieldType = "text", Placeholder = "e.g. Advanced Computing Lab 4, Tech Block" },
                            new ServiceFormField { Name = "RequiredDateSlot", Label = "Date and Time Slot", FieldType = "text", Placeholder = "e.g. 05-Dec-2026, 2:00 PM to 6:00 PM" },
                            new ServiceFormField { Name = "FacultyGuide", Label = "Faculty In-Charge / Guide Name", FieldType = "text", Placeholder = "e.g. Prof. Rajesh Sharma" }
                        }
                    }
                };

                db.Services.InsertMany(services);
            }

            // 3. Seed Announcements
            if (db.Announcements.CountDocuments(FilterDefinition<Announcement>.Empty) == 0)
            {
                var announcements = new List<Announcement>
                {
                    new Announcement
                    {
                        Title = "Fall Semester 2026 Final Examination Timetable Released",
                        Content = "The University Examination Board has finalized and published the end-semester examination timetable. All students are advised to download the schedule and verify room assignments on the student portal.",
                        Category = "Examination",
                        TargetRole = "All",
                        Priority = "High",
                        AuthorName = "Office of the Controller of Examinations",
                        IsActive = true,
                        CreatedAt = DateTime.UtcNow.AddDays(-2),
                        ExpiryDate = DateTime.UtcNow.AddDays(30)
                    },
                    new Announcement
                    {
                        Title = "Campus Tech Innovation Expo & Hackathon 2026",
                        Content = "Registrations are officially open for the Annual 48-Hour Smart Campus Hackathon. Top 3 teams win cash prizes, incubation grants, and direct industry mentorship. Submit your team proposals via the Event Registration service.",
                        Category = "Event",
                        TargetRole = "Student",
                        Priority = "Medium",
                        AuthorName = "Innovation & Entrepreneurship Cell",
                        IsActive = true,
                        CreatedAt = DateTime.UtcNow.AddDays(-4),
                        ExpiryDate = DateTime.UtcNow.AddDays(20)
                    },
                    new Announcement
                    {
                        Title = "Scheduled IT Core Network & Server Maintenance",
                        Content = "Please be advised that core network switches and database servers will undergo scheduled firmware upgrades this Sunday between 01:00 AM and 05:00 AM. Internet and portal access may experience brief intermittent pauses.",
                        Category = "Urgent",
                        TargetRole = "All",
                        Priority = "Urgent",
                        AuthorName = "IT Services Directorate",
                        IsActive = true,
                        CreatedAt = DateTime.UtcNow.AddDays(-1),
                        ExpiryDate = DateTime.UtcNow.AddDays(5)
                    },
                    new Announcement
                    {
                        Title = "Faculty Academic Research Grant Submissions 2026-27",
                        Content = "Proposals for internal seed research grants for the upcoming academic year are now invited. All full-time faculty members may submit proposals to the Dean of Research before the 15th of next month.",
                        Category = "Academic",
                        TargetRole = "Faculty",
                        Priority = "Medium",
                        AuthorName = "Dean (Academic Research)",
                        IsActive = true,
                        CreatedAt = DateTime.UtcNow.AddDays(-6),
                        ExpiryDate = DateTime.UtcNow.AddDays(40)
                    }
                };

                db.Announcements.InsertMany(announcements);
            }

            // 4. Seed Sample Service Requests
            if (db.ServiceRequests.CountDocuments(FilterDefinition<ServiceRequest>.Empty) == 0)
            {
                var studentUser = db.Users.Find(u => u.Email == "student.alex@campus.edu").FirstOrDefault();
                var facultyUser = db.Users.Find(u => u.Email == "faculty.sharma@campus.edu").FirstOrDefault();

                var requests = new List<ServiceRequest>
                {
                    new ServiceRequest
                    {
                        RequestNumber = "REQ-2026-0001",
                        ServiceId = "SRV-CRT",
                        ServiceName = "Student Certificates",
                        ServiceCategory = "Certificates",
                        StudentId = studentUser?.Id,
                        StudentName = studentUser?.FullName ?? "Alex Johnson",
                        StudentEmail = studentUser?.Email ?? "student.alex@campus.edu",
                        StudentRollNo = "CS-2024-042",
                        Department = "Computer Science",
                        Priority = "High",
                        Status = "Completed",
                        AssignedToId = facultyUser?.Id,
                        AssignedToName = facultyUser?.FullName ?? "Prof. Rajesh Sharma",
                        AssignedRole = "Faculty",
                        SubmissionDate = DateTime.UtcNow.AddDays(-7),
                        UpdatedAt = DateTime.UtcNow.AddDays(-5),
                        FormDetails = new Dictionary<string, string>
                        {
                            { "CertificateType", "Bonafide Student Certificate" },
                            { "Purpose", "Applying for Global Student Developer Grant" },
                            { "DeliveryFormat", "Digital Signed PDF" }
                        },
                        Remarks = "Certificate signed digitally and generated successfully.",
                        StatusHistory = new List<StatusHistoryItem>
                        {
                            new StatusHistoryItem { Status = "Pending", ChangedBy = "Alex Johnson", Role = "Student", Timestamp = DateTime.UtcNow.AddDays(-7), Comment = "Service request submitted." },
                            new StatusHistoryItem { Status = "In Progress", ChangedBy = "Prof. Rajesh Sharma", Role = "Faculty", Timestamp = DateTime.UtcNow.AddDays(-6), Comment = "Academic record and semester enrollment verified." },
                            new StatusHistoryItem { Status = "Approved", ChangedBy = "Prof. Rajesh Sharma", Role = "Faculty", Timestamp = DateTime.UtcNow.AddDays(-5), Comment = "Application approved by HOD." },
                            new StatusHistoryItem { Status = "Completed", ChangedBy = "Dr. Robert Vance", Role = "Admin", Timestamp = DateTime.UtcNow.AddDays(-5), Comment = "Official digital seal attached. Request fulfilled." }
                        }
                    },
                    new ServiceRequest
                    {
                        RequestNumber = "REQ-2026-0002",
                        ServiceId = "SRV-ITS",
                        ServiceName = "IT Support",
                        ServiceCategory = "IT Support",
                        StudentId = studentUser?.Id,
                        StudentName = studentUser?.FullName ?? "Alex Johnson",
                        StudentEmail = studentUser?.Email ?? "student.alex@campus.edu",
                        StudentRollNo = "CS-2024-042",
                        Department = "Computer Science",
                        Priority = "Medium",
                        Status = "In Progress",
                        AssignedToId = facultyUser?.Id,
                        AssignedToName = facultyUser?.FullName ?? "Prof. Rajesh Sharma",
                        AssignedRole = "Faculty",
                        SubmissionDate = DateTime.UtcNow.AddDays(-3),
                        UpdatedAt = DateTime.UtcNow.AddDays(-1),
                        FormDetails = new Dictionary<string, string>
                        {
                            { "IssueCategory", "Campus WiFi Access / MAC Registration" },
                            { "DeviceType", "Dell XPS 15 (Windows 11 Pro)" },
                            { "MacAddress", "F4-34-F0-8A-BC-12" },
                            { "IssueDescription", "New laptop requires campus RADIUS certificate and high-speed hostel WiFi whitelist." }
                        },
                        Remarks = "MAC submitted to Network Operations Center (NOC) for authorization.",
                        StatusHistory = new List<StatusHistoryItem>
                        {
                            new StatusHistoryItem { Status = "Pending", ChangedBy = "Alex Johnson", Role = "Student", Timestamp = DateTime.UtcNow.AddDays(-3), Comment = "WiFi registration request initiated." },
                            new StatusHistoryItem { Status = "In Progress", ChangedBy = "Prof. Rajesh Sharma", Role = "Faculty", Timestamp = DateTime.UtcNow.AddDays(-1), Comment = "Assigned to Network Operations. Provisioning credentials." }
                        }
                    },
                    new ServiceRequest
                    {
                        RequestNumber = "REQ-2026-0003",
                        ServiceId = "SRV-LIB",
                        ServiceName = "Library Services",
                        ServiceCategory = "Library",
                        StudentId = studentUser?.Id,
                        StudentName = studentUser?.FullName ?? "Alex Johnson",
                        StudentEmail = studentUser?.Email ?? "student.alex@campus.edu",
                        StudentRollNo = "CS-2024-042",
                        Department = "Computer Science",
                        Priority = "Low",
                        Status = "Pending",
                        SubmissionDate = DateTime.UtcNow.AddHours(-18),
                        UpdatedAt = DateTime.UtcNow.AddHours(-18),
                        FormDetails = new Dictionary<string, string>
                        {
                            { "ServiceType", "Study Carrel Booking" },
                            { "BookTitle", "Distributed Systems Research Group Work" },
                            { "Purpose", "Preparation for National Coding Hackathon" }
                        },
                        Remarks = "Awaiting review by library supervisor.",
                        StatusHistory = new List<StatusHistoryItem>
                        {
                            new StatusHistoryItem { Status = "Pending", ChangedBy = "Alex Johnson", Role = "Student", Timestamp = DateTime.UtcNow.AddHours(-18), Comment = "Carrel reservation submitted." }
                        }
                    }
                };

                db.ServiceRequests.InsertMany(requests);
            }

            // 5. Seed Sample Complaints
            if (db.Complaints.CountDocuments(FilterDefinition<Complaint>.Empty) == 0)
            {
                var studentUser = db.Users.Find(u => u.Email == "student.alex@campus.edu").FirstOrDefault();
                var facultyUser = db.Users.Find(u => u.Email == "faculty.sharma@campus.edu").FirstOrDefault();

                var complaints = new List<Complaint>
                {
                    new Complaint
                    {
                        ComplaintNumber = "CMP-2026-0001",
                        StudentId = studentUser?.Id,
                        StudentName = studentUser?.FullName ?? "Alex Johnson",
                        StudentRollNo = "CS-2024-042",
                        Department = "Computer Science",
                        Category = "IT",
                        Subject = "Slow WiFi connectivity on 3rd Floor of Central Library",
                        Description = "The wireless access point in the study hall on the 3rd floor keeps dropping connection during evening peak hours (6 PM - 9 PM). Speeds drop below 500 Kbps making online research and documentation inaccessible.",
                        Location = "Central Library - 3rd Floor Quiet Study Hall",
                        Priority = "High",
                        Status = "Resolved",
                        AssignedToId = facultyUser?.Id,
                        AssignedToName = facultyUser?.FullName ?? "Prof. Rajesh Sharma",
                        OfficialResponse = "IT Infrastructure inspected the access point and replaced the faulty PoE injector switch. A second dual-band AP (Wi-Fi 6) was added to load-balance traffic.",
                        ResolutionDate = DateTime.UtcNow.AddDays(-2),
                        CreatedAt = DateTime.UtcNow.AddDays(-6),
                        UpdatedAt = DateTime.UtcNow.AddDays(-2),
                        Responses = new List<ComplaintResponseItem>
                        {
                            new ComplaintResponseItem { ResponderId = facultyUser?.Id, ResponderName = "Prof. Rajesh Sharma", Role = "Faculty", Message = "Ticket forwarded to Network Operations. Engineers are checking the switch port.", Timestamp = DateTime.UtcNow.AddDays(-4) },
                            new ComplaintResponseItem { ResponderId = "admin", ResponderName = "IT Administration", Role = "Admin", Message = "Issue resolved. Wi-Fi 6 AP installed and tested. Signal strength is now 98% across the hall.", Timestamp = DateTime.UtcNow.AddDays(-2) }
                        }
                    },
                    new Complaint
                    {
                        ComplaintNumber = "CMP-2026-0002",
                        StudentId = studentUser?.Id,
                        StudentName = studentUser?.FullName ?? "Alex Johnson",
                        StudentRollNo = "CS-2024-042",
                        Department = "Computer Science",
                        Category = "Hostel",
                        Subject = "Hot water supply intermittent in Pine Valley Hostel Block A",
                        Description = "The solar water heating system in Block A, 2nd floor restrooms does not maintain temperature in the early mornings before 8 AM.",
                        Location = "Pine Valley Campus Hostel - Block A, 2nd Floor",
                        Priority = "Medium",
                        Status = "In Progress",
                        AssignedToId = facultyUser?.Id,
                        AssignedToName = facultyUser?.FullName ?? "Prof. Rajesh Sharma",
                        CreatedAt = DateTime.UtcNow.AddDays(-2),
                        UpdatedAt = DateTime.UtcNow.AddHours(-12),
                        Responses = new List<ComplaintResponseItem>
                        {
                            new ComplaintResponseItem { ResponderId = facultyUser?.Id, ResponderName = "Prof. Rajesh Sharma", Role = "Faculty", Message = "Estate maintenance plumber has been notified and scheduled for heating coil replacement today.", Timestamp = DateTime.UtcNow.AddHours(-12) }
                        }
                    }
                };

                db.Complaints.InsertMany(complaints);
            }

            // 6. Seed Sample Feedback
            if (db.Feedback.CountDocuments(FilterDefinition<CampusFeedback>.Empty) == 0)
            {
                var studentUser = db.Users.Find(u => u.Email == "student.alex@campus.edu").FirstOrDefault();

                var feedbackList = new List<CampusFeedback>
                {
                    new CampusFeedback
                    {
                        StudentId = studentUser?.Id,
                        StudentName = studentUser?.FullName ?? "Alex Johnson",
                        ServiceId = "SRV-CRT",
                        ServiceName = "Student Certificates",
                        Category = "Certificates",
                        Rating = 5,
                        Comments = "Super fast turnaround! Received my digitally signed bonafide certificate within 24 hours of applying.",
                        CreatedAt = DateTime.UtcNow.AddDays(-4)
                    },
                    new CampusFeedback
                    {
                        StudentId = studentUser?.Id,
                        StudentName = studentUser?.FullName ?? "Alex Johnson",
                        ServiceId = "SRV-LIB",
                        ServiceName = "Library Services",
                        Category = "Library",
                        Rating = 4,
                        Comments = "The online catalog search and reservation system is very convenient. Would love to have weekend carrel bookings extended.",
                        CreatedAt = DateTime.UtcNow.AddDays(-8)
                    }
                };

                db.Feedback.InsertMany(feedbackList);
            }

            // 7. Seed Notifications
            if (db.Notifications.CountDocuments(FilterDefinition<Notification>.Empty) == 0)
            {
                var studentUser = db.Users.Find(u => u.Email == "student.alex@campus.edu").FirstOrDefault();
                if (studentUser != null)
                {
                    var notifications = new List<Notification>
                    {
                        new Notification
                        {
                            UserId = studentUser.Id,
                            Title = "Service Request REQ-2026-0001 Completed",
                            Message = "Your Bonafide Certificate request has been completed and signed. You can now download or print your voucher.",
                            Type = "success",
                            Link = "/Student/RequestDetails/REQ-2026-0001",
                            IsRead = false,
                            CreatedAt = DateTime.UtcNow.AddDays(-1)
                        },
                        new Notification
                        {
                            UserId = studentUser.Id,
                            Title = "Complaint CMP-2026-0001 Resolved",
                            Message = "IT Services resolved your Wi-Fi issue on Library 3rd Floor.",
                            Type = "info",
                            Link = "/Student/ComplaintDetails/CMP-2026-0001",
                            IsRead = false,
                            CreatedAt = DateTime.UtcNow.AddDays(-2)
                        }
                    };
                    db.Notifications.InsertMany(notifications);
                }
            }
        }
    }
}
