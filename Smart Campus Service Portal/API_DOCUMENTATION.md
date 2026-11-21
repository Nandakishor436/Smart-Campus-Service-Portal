# 🌐 Smart Campus Service Portal – REST API Specification

This document provides complete technical specifications for the Smart Campus Service Portal REST API. All endpoints produce and consume standard JSON payloads (`Content-Type: application/json`).

---

## 🔑 Authentication & Authorization

The API supports two authentication modes:
1. **Bearer Token Authentication (Recommended for API clients / Mobile apps):** Include the token received from `POST /api/auth/login` in the `Authorization` header:
   ```http
   Authorization: Bearer <your-jwt-hmac-token>
   ```
2. **Cookie Session Authentication:** If accessing the API from a browser with an active session, the ASP.NET authentication cookie is automatically evaluated.

### Response Envelope Format
All API responses adhere to a consistent JSON structure:
```json
{
  "success": true,
  "message": "Operation completed successfully.",
  "data": { ... },
  "timestamp": "2026-11-21T07:15:00.000Z"
}
```

In the event of an error:
```json
{
  "success": false,
  "message": "Detailed error explanation",
  "data": null,
  "timestamp": "2026-11-21T07:15:00.000Z"
}
```

---

## 1. Authentication APIs

### 1.1 Register Student Account
* **Endpoint:** `POST /api/auth/register`
* **Access:** Public
* **Description:** Creates a new student user account and associated student academic record.

#### Request Headers
```http
Content-Type: application/json
```

#### Request Body
```json
{
  "email": "new.student@campus.edu",
  "password": "Password@123",
  "fullName": "Samantha Miller",
  "role": "Student",
  "department": "Information Science",
  "rollNumber": "IS-2024-089",
  "semester": 4,
  "enrollmentYear": 2024,
  "phoneNumber": "+1-555-019-2834"
}
```

#### Response (`201 Created`)
```json
{
  "success": true,
  "message": "Registration successful. You may now log in.",
  "data": {
    "userId": "673ec9a4891b2c4e610d33a1",
    "email": "new.student@campus.edu",
    "fullName": "Samantha Miller",
    "role": "Student"
  },
  "timestamp": "2026-11-21T07:15:00Z"
}
```

---

### 1.2 User Login
* **Endpoint:** `POST /api/auth/login`
* **Access:** Public
* **Description:** Verifies credentials and generates a stateless HMAC-SHA256 Bearer token.

#### Request Body
```json
{
  "email": "student.alex@campus.edu",
  "password": "Student@123"
}
```

#### Response (`200 OK`)
```json
{
  "success": true,
  "message": "Authentication successful.",
  "data": {
    "token": "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9...",
    "user": {
      "id": "673ebf129a0c4d2e8b1a2001",
      "email": "student.alex@campus.edu",
      "fullName": "Alex Johnson",
      "role": "Student",
      "department": "Computer Science"
    }
  },
  "timestamp": "2026-11-21T07:15:00Z"
}
```

---

### 1.3 User Logout
* **Endpoint:** `POST /api/auth/logout`
* **Access:** Authenticated
* **Description:** Invalidates the current session and instructs client to purge tokens.

#### Response (`200 OK`)
```json
{
  "success": true,
  "message": "Logged out successfully.",
  "data": null,
  "timestamp": "2026-11-21T07:15:00Z"
}
```

---

### 1.4 Get Current User Profile
* **Endpoint:** `GET /api/auth/me`
* **Access:** Authenticated (Bearer Token or Cookie)
* **Description:** Returns profile details and role metadata of the authenticated user.

#### Response (`200 OK`)
```json
{
  "success": true,
  "message": "Profile retrieved.",
  "data": {
    "id": "673ebf129a0c4d2e8b1a2001",
    "email": "student.alex@campus.edu",
    "fullName": "Alex Johnson",
    "role": "Student",
    "department": "Computer Science",
    "studentProfile": {
      "rollNumber": "CS-2024-042",
      "semester": 4,
      "enrollmentYear": 2024
    }
  },
  "timestamp": "2026-11-21T07:15:00Z"
}
```

---

## 2. Student APIs

### 2.1 Get All Students
* **Endpoint:** `GET /api/students`
* **Access:** Admin, Faculty
* **Description:** Retrieves a directory list of registered students.

#### Response (`200 OK`)
```json
{
  "success": true,
  "message": "Students retrieved.",
  "data": [
    {
      "id": "673ebf129a0c4d2e8b1a2010",
      "userId": "673ebf129a0c4d2e8b1a2001",
      "rollNumber": "CS-2024-042",
      "fullName": "Alex Johnson",
      "department": "Computer Science",
      "semester": 4,
      "enrollmentYear": 2024
    }
  ],
  "timestamp": "2026-11-21T07:15:00Z"
}
```

---

### 2.2 Get Student by ID
* **Endpoint:** `GET /api/students/{id}`
* **Access:** Admin, Faculty, Student (Own Profile)
* **Description:** Retrieves specific student record by student document ID or User ID.

#### Response (`200 OK`)
```json
{
  "success": true,
  "message": "Student found.",
  "data": {
    "id": "673ebf129a0c4d2e8b1a2010",
    "userId": "673ebf129a0c4d2e8b1a2001",
    "rollNumber": "CS-2024-042",
    "fullName": "Alex Johnson",
    "department": "Computer Science",
    "semester": 4,
    "enrollmentYear": 2024
  },
  "timestamp": "2026-11-21T07:15:00Z"
}
```

---

### 2.3 Update Student Profile
* **Endpoint:** `PUT /api/students/{id}`
* **Access:** Admin, Student (Own Profile)
* **Description:** Updates academic or personal information.

#### Request Body
```json
{
  "semester": 5,
  "department": "Computer Science & Engineering"
}
```

#### Response (`200 OK`)
```json
{
  "success": true,
  "message": "Student updated successfully.",
  "data": { ... },
  "timestamp": "2026-11-21T07:15:00Z"
}
```

---

## 3. Faculty APIs

### 3.1 Get All Faculty Members
* **Endpoint:** `GET /api/faculty`
* **Access:** Public or Authenticated
* **Description:** Retrieves directory of campus faculty members and departmental assignments.

#### Response (`200 OK`)
```json
{
  "success": true,
  "message": "Faculty list retrieved.",
  "data": [
    {
      "id": "673ebf129a0c4d2e8b1a2020",
      "userId": "673ebf129a0c4d2e8b1a2002",
      "employeeId": "FAC-CS-001",
      "fullName": "Prof. Rajesh Sharma",
      "department": "Computer Science",
      "designation": "Associate Professor & HOD"
    }
  ],
  "timestamp": "2026-11-21T07:15:00Z"
}
```

---

### 3.2 Get Faculty by ID
* **Endpoint:** `GET /api/faculty/{id}`
* **Access:** Public or Authenticated
* **Description:** Retrieves specific faculty member details.

---

## 4. Campus Service APIs

### 4.1 Get All Campus Services
* **Endpoint:** `GET /api/services`
* **Access:** Public or Authenticated
* **Description:** Retrieves all active campus services, including descriptions, turn-around times, and form field requirements.

#### Response (`200 OK`)
```json
{
  "success": true,
  "message": "Services retrieved.",
  "data": [
    {
      "id": "673ebf129a0c4d2e8b1a2030",
      "serviceCode": "SRV-LIB",
      "name": "Library Services",
      "category": "Library",
      "description": "Book reservations, digital library access, journal archives.",
      "icon": "fa-book-open-reader",
      "estimatedDays": 1,
      "department": "University Central Library",
      "isActive": true,
      "formFields": [
        {
          "name": "RequestType",
          "label": "Service Required",
          "fieldType": "select",
          "isRequired": true,
          "options": ["Book Borrowing", "Digital Library Access Pass", "Journal Archive Access"]
        }
      ]
    }
  ],
  "timestamp": "2026-11-21T07:15:00Z"
}
```

---

### 4.2 Create New Service
* **Endpoint:** `POST /api/services`
* **Access:** Admin only
* **Description:** Creates a new campus service definition with custom dynamic form fields.

#### Request Body
```json
{
  "serviceCode": "SRV-INNOV",
  "name": "Maker Space & 3D Printing",
  "category": "Laboratory",
  "description": "Access to high-precision 3D printers and laser cutters.",
  "icon": "fa-cube",
  "estimatedDays": 2,
  "department": "Engineering Prototyping Center",
  "formFields": [
    {
      "name": "StlFileUrl",
      "label": "Design Link / Notes",
      "fieldType": "text",
      "isRequired": true
    }
  ]
}
```

#### Response (`201 Created`)
```json
{
  "success": true,
  "message": "Campus service created successfully.",
  "data": { ... },
  "timestamp": "2026-11-21T07:15:00Z"
}
```

---

### 4.3 Update Service
* **Endpoint:** `PUT /api/services/{id}`
* **Access:** Admin only
* **Description:** Updates service name, description, estimated days, or form fields.

---

### 4.4 Delete Service
* **Endpoint:** `DELETE /api/services/{id}`
* **Access:** Admin only
* **Description:** Soft-disables or permanently removes a campus service definition.

---

## 5. Service Request APIs

### 5.1 Submit Service Request
* **Endpoint:** `POST /api/requests`
* **Access:** Student only
* **Description:** Submits a service request with custom form parameters.

#### Request Body
```json
{
  "serviceId": "673ebf129a0c4d2e8b1a2030",
  "priority": "High",
  "formDetails": {
    "CertificateType": "Bonafide Student Certificate",
    "Purpose": "Bank Education Loan Processing",
    "DeliveryFormat": "Digital Signed PDF"
  }
}
```

#### Response (`201 Created`)
```json
{
  "success": true,
  "message": "Service request submitted successfully.",
  "data": {
    "id": "673ec56a9a0c4d2e8b1a2055",
    "requestNumber": "REQ-2026-0005",
    "serviceName": "Student Certificates",
    "status": "Pending",
    "priority": "High",
    "submissionDate": "2026-11-21T07:20:00Z"
  },
  "timestamp": "2026-11-21T07:20:00Z"
}
```

---

### 5.2 Get Service Requests
* **Endpoint:** `GET /api/requests`
* **Access:** Authenticated
  * **Students:** Returns only their own submitted requests.
  * **Faculty:** Returns requests assigned to their department/queue.
  * **Admin:** Returns all requests across the campus.
* **Query Parameters:**
  * `status` (Optional): Filter by `Pending`, `In Progress`, `Approved`, `Completed`, `Rejected`.
  * `category` (Optional): Filter by service category.

---

### 5.3 Get Service Request by ID
* **Endpoint:** `GET /api/requests/{id}`
* **Access:** Authenticated (Student Owner, Assigned Faculty, or Admin)
* **Description:** Retrieves complete request details including dynamic form entries, tracking history, and reviewer remarks. Can be queried by MongoDB ObjectId or human-readable Request Number (`REQ-2026-0001`).

---

### 5.4 Update Service Request Status / Assignment
* **Endpoint:** `PUT /api/requests/{id}`
* **Access:** Admin or Faculty
* **Description:** Updates request progression status (`In Progress`, `Approved`, `Rejected`, `Completed`), reassigns faculty, or adds official remarks.

#### Request Body
```json
{
  "status": "Approved",
  "remarks": "Academic transcripts verified and approved by Department Head.",
  "assignedToId": "673ebf129a0c4d2e8b1a2002"
}
```

#### Response (`200 OK`)
```json
{
  "success": true,
  "message": "Service request updated successfully.",
  "data": {
    "id": "673ec56a9a0c4d2e8b1a2055",
    "requestNumber": "REQ-2026-0005",
    "status": "Approved",
    "remarks": "Academic transcripts verified and approved by Department Head.",
    "statusHistory": [
      {
        "status": "Pending",
        "changedBy": "Alex Johnson",
        "role": "Student",
        "timestamp": "2026-11-21T07:20:00Z"
      },
      {
        "status": "Approved",
        "changedBy": "Prof. Rajesh Sharma",
        "role": "Faculty",
        "timestamp": "2026-11-21T07:25:00Z",
        "comment": "Academic transcripts verified and approved by Department Head."
      }
    ]
  },
  "timestamp": "2026-11-21T07:25:00Z"
}
```

---

### 5.5 Delete Service Request
* **Endpoint:** `DELETE /api/requests/{id}`
* **Access:** Admin only
* **Description:** Deletes a service request from the database.

---

## 6. Complaint Management APIs

### 6.1 Submit Complaint
* **Endpoint:** `POST /api/complaints`
* **Access:** Student only
* **Description:** Registers a new campus grievance ticket.

#### Request Body
```json
{
  "category": "IT, Network & WiFi",
  "subject": "Intermittent WiFi Outage in Science Block 2",
  "description": "The wireless access point in Room 204 drops connection every 10 minutes during morning lab sessions.",
  "location": "Science Block 2, Lab 204",
  "priority": "High"
}
```

#### Response (`201 Created`)
```json
{
  "success": true,
  "message": "Complaint registered successfully.",
  "data": {
    "id": "673ec7129a0c4d2e8b1a2060",
    "complaintNumber": "CMP-2026-0004",
    "subject": "Intermittent WiFi Outage in Science Block 2",
    "status": "Open",
    "createdAt": "2026-11-21T07:30:00Z"
  },
  "timestamp": "2026-11-21T07:30:00Z"
}
```

---

### 6.2 Get Complaints
* **Endpoint:** `GET /api/complaints`
* **Access:** Authenticated
* **Description:** Retrieves complaints list. Students receive their own tickets; Faculty and Admins receive campus-wide complaints.

---

### 6.3 Update / Resolve Complaint
* **Endpoint:** `PUT /api/complaints/{id}`
* **Access:** Admin or Faculty
* **Description:** Updates complaint status (`Open`, `Under Investigation`, `Resolved`, `Closed`), assigns an officer, or attaches official response comments.

#### Request Body
```json
{
  "status": "Resolved",
  "officialResponse": "Technician replaced the faulty Cisco PoE switch port. Network throughput restored to 300 Mbps."
}
```

---

## 7. Announcement APIs

### 7.1 Get Announcements
* **Endpoint:** `GET /api/announcements`
* **Access:** Public or Authenticated
* **Description:** Retrieves active campus announcements filtered by audience role.

---

### 7.2 Create Announcement
* **Endpoint:** `POST /api/announcements`
* **Access:** Admin only
* **Description:** Publishes an announcement.

#### Request Body
```json
{
  "title": "Semester Final Examination Timetable Published",
  "content": "The official timetable for upcoming semester examinations is now accessible under academic downloads.",
  "category": "Academic",
  "targetRole": "All",
  "priority": "High"
}
```

---

## 8. Feedback & Analytics APIs

### 8.1 Submit Feedback
* **Endpoint:** `POST /api/feedback`
* **Access:** Student only
* **Description:** Submits a quantitative rating (1 to 5) and qualitative comments on campus services.

#### Request Body
```json
{
  "serviceId": "SRV-ITS",
  "rating": 5,
  "comments": "The IT support team resolved the MAC address registration in less than 2 hours. Exceptional turnaround!"
}
```

---

### 8.2 Get Feedback Feed
* **Endpoint:** `GET /api/feedback`
* **Access:** Admin, Faculty
* **Description:** Retrieves submitted feedback and satisfaction ratings.

---

### 8.3 Get Executive Analytics Dashboard
* **Endpoint:** `GET /api/analytics/dashboard`
* **Access:** Admin or Faculty
* **Description:** Computes aggregated KPI metrics and chart datasets for real-time dashboards.

#### Response (`200 OK`)
```json
{
  "success": true,
  "message": "Analytics metrics computed.",
  "data": {
    "totalStudents": 42,
    "totalFaculty": 12,
    "totalServices": 8,
    "totalRequests": 18,
    "pendingRequests": 4,
    "completedRequests": 12,
    "totalComplaints": 6,
    "openComplaints": 2,
    "averageRating": 4.8,
    "requestsByStatus": {
      "Pending": 4,
      "InProgress": 2,
      "Approved": 0,
      "Completed": 12,
      "Rejected": 0
    },
    "requestsByCategory": {
      "Certificates": 6,
      "IT Support": 5,
      "Hostel": 3,
      "Transport": 2,
      "Library": 2
    }
  },
  "timestamp": "2026-11-21T07:35:00Z"
}
```

---

## 9. Error Codes Summary

| HTTP Status | Error Type | Cause |
| :---: | :--- | :--- |
| **400** | Bad Request | Missing required parameters or invalid JSON structure. |
| **401** | Unauthorized | Missing or expired Bearer token or invalid login credentials. |
| **403** | Forbidden | Authenticated user lacks permission for the requested resource (e.g. Student calling Admin endpoint). |
| **404** | Not Found | Target resource ID does not exist in MongoDB. |
| **405** | Method Not Allowed | Attempted HTTP verb is not supported on this endpoint. |
| **500** | Internal Server Error | Unhandled server exception (logged with stack trace). |

---
*Smart Campus Service Portal &bull; REST API Documentation &bull; 2026*
