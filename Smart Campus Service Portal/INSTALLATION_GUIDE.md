# 🛠️ Smart Campus Service Portal – Installation & Setup Guide
### Complete Step-by-Step Deployment and Configuration Manual

---

## 1. System Requirements

Before setting up the Smart Campus Service Portal, ensure the host machine meets the following prerequisites:

| Component | Minimum Requirement | Recommended |
| :--- | :--- | :--- |
| **Operating System** | Windows 10 (64-bit) / Server 2016+ | Windows 11 / Windows Server 2022 |
| **Framework Runtime** | .NET Framework 4.7.2 | .NET Framework 4.8+ |
| **Database Engine** | MongoDB Community Server 6.0+ | MongoDB Server 8.2 (x64) |
| **Build Tools** | MSBuild 15.0+ | Visual Studio 2022 Community or MSBuild 17+ |
| **Web Server** | IIS Express 10.0 | IIS Express 10.0 or Full IIS 10.0 |
| **Command Shell** | Windows PowerShell 5.1 | PowerShell 7+ |
| **Browser** | Google Chrome, Edge, Firefox | Latest Modern Chromium Browser |

---

## 2. MongoDB Database Configuration

The application requires an active MongoDB instance on `localhost:27017`.

### 2.1 Verifying MongoDB Service Status
Open an elevated PowerShell prompt:
```powershell
# Check MongoDB Windows Service status
Get-Service -Name MongoDB
```
If the service is not running, start it:
```powershell
Start-Service -Name MongoDB
```

### 2.2 Manual MongoDB Server Execution (If not running as a Windows Service)
If MongoDB is installed standalone:
```powershell
& "C:\Program Files\MongoDB\Server\8.2\bin\mongod.exe" --dbpath="C:\data\db" --port 27017
```

### 2.3 Connection String Verification (`Web.config`)
Verify that the `connectionStrings` section in `Web.config` points to your MongoDB instance:
```xml
<connectionStrings>
  <add name="MongoDbConnection" connectionString="mongodb://localhost:27017" />
  <add name="MongoDbDatabase" connectionString="SmartCampusDb" />
</connectionStrings>
```

> **Note:** The application manages database creation, collections, and index generation automatically on initial startup. You do **not** need to manually import any initial JSON or BSON scripts!

---

## 3. Restoring NuGet Package Dependencies

All external assemblies are referenced from the parent `packages` directory (`..\packages`).

### Required Packages:
* `MongoDB.Driver` & `MongoDB.Bson` (v2.25.0)
* `MongoDB.Libmongocrypt` (v1.8.2)
* `BCrypt.Net-Next` (v4.0.3)
* `Snappier` (v1.0.0)
* `ZstdSharp.Port` (v0.7.3)
* `SharpCompress` (v0.30.1)
* `DnsClient` (v1.6.1)
* `Microsoft.AspNet.Mvc` (v5.2.9)
* `Newtonsoft.Json` (v13.0.3)

### Restoring via NuGet CLI (if packages are missing):
```powershell
cd "C:\Users\nandu\source\repos\Smart Campus Service Portal"
nuget restore "Smart Campus Service Portal.sln"
```

---

## 4. Compiling the Project

You can compile the project using Visual Studio 2022 or directly from the terminal via MSBuild.

### Option A: Using MSBuild from Command Line (Fastest)
```powershell
cd "C:\Users\nandu\source\repos\Smart Campus Service Portal\Smart Campus Service Portal"

& "C:\Program Files\Microsoft Visual Studio\18\Community\MSBuild\Current\Bin\MSBuild.exe" "Smart Campus Service Portal.csproj" /p:Configuration=Debug /t:Build
```
**Expected Output:**
```
Build succeeded.
    0 Warning(s)
    0 Error(s)
```

### Option B: Using Visual Studio 2022 IDE
1. Open `Smart Campus Service Portal.sln` in Visual Studio 2022.
2. Ensure build configuration is set to **Debug** or **Release** and platform is **Any CPU**.
3. Right-click the project in Solution Explorer &rarr; Click **Rebuild Solution**.

---

## 5. Running the Application

### Method 1: Using IIS Express (Development Server)
Launch IIS Express pointing to the application root directory:
```powershell
& "C:\Program Files\IIS Express\iisexpress.exe" /path:"C:\Users\nandu\source\repos\Smart Campus Service Portal\Smart Campus Service Portal" /port:62359
```

Once running, navigate to:
```
http://localhost:62359
```

### Method 2: Launching from Visual Studio
Press **`F5`** (with debugging) or **`Ctrl + F5`** (without debugging) inside Visual Studio.

---

## 6. Default Demo Credentials & Seed Data

On the first application run, `Global.asax.cs` triggers `DatabaseSeeder.SeedInitialData()`. This populates the MongoDB `SmartCampusDb` with all 8 campus services, demonstration announcements, and pre-configured accounts:

| User Role | Email | Password | Full Name | Associated Permissions |
| :--- | :--- | :--- | :--- | :--- |
| **Administrator** | `admin@campus.edu` | `Admin@123` | Dr. Robert Vance | Full portal management, user control, service CRUD, reports |
| **Faculty Member** | `faculty.sharma@campus.edu` | `Faculty@123` | Prof. Rajesh Sharma | Request verification, complaint investigation, department services |
| **Faculty Member** | `faculty.chen@campus.edu` | `Faculty@123` | Dr. Emily Chen | Academic request approval, department feedback review |
| **Student** | `student.alex@campus.edu` | `Student@123` | Alex Johnson | Request submission, status tracking, printable voucher, complaints |
| **Student** | `student.priya@campus.edu` | `Student@123` | Priya Patel | Computer science student account with sample submitted requests |

> **Quick Login Tip:** On the Sign-In page (`/Account/Login`), click any of the **Quick Demo Login** badges (`Student Demo`, `Faculty Demo`, or `Admin Demo`) to auto-populate credentials and log in instantly.

---

## 7. Running the Automated Test Suite

A comprehensive test suite script is provided to verify end-to-end functionality across the web and REST API layers.

With the server running on `http://localhost:62359`, execute:
```powershell
cd "C:\Users\nandu\source\repos\Smart Campus Service Portal\Smart Campus Service Portal"
powershell -ExecutionPolicy Bypass -File test_suite.ps1
```

The script will automatically perform and report:
* 7 Public route accessibility tests
* 11 REST API transactional tests (Auth token generation, Service retrieval, Request submission, Status updates, Complaints, Feedback)
* 2 Unauthorized access redirect checks
* 12 Authenticated session web page tests across Student, Faculty, and Admin portals

**Target Verification:** `ALL TEST SUITES PASSED PERFECTLY (100% SUCCESS RATE)!`

---

## 8. Troubleshooting Guide

### Issue 1: "Unable to connect to the remote server" or MongoDB timeout
* **Cause:** The local MongoDB service is stopped.
* **Resolution:** Run `Start-Service -Name MongoDB` in an Administrator PowerShell window. Check that port 27017 is open using `Test-NetConnection -ComputerName localhost -Port 27017`.

### Issue 2: "Could not load file or assembly 'MongoDB.Libmongocrypt' or one of its dependencies"
* **Cause:** Native decompression and crypto assemblies were not copied to the `bin\` output directory.
* **Resolution:** Ensure the `<Reference Include="MongoDB.Libmongocrypt">`, `Snappier`, `ZstdSharp`, and `SharpCompress` items exist in `Smart Campus Service Portal.csproj`. Re-run the MSBuild build command.

### Issue 3: IIS Express port conflict
* **Cause:** Port `62359` is already bound to another process.
* **Resolution:** Terminate conflicting IIS Express instances:
  ```powershell
  Stop-Process -Name iisexpress -Force
  ```
  Or change the port in the launch command (e.g., `/port:62360`).

---
*Smart Campus Service Portal &bull; Installation and Setup Guide &bull; 2026*
