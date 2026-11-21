$baseUrl = "http://localhost:62359"
$allPassed = $true

function Report-Result($testName, $passed, $details) {
    if ($passed) {
        Write-Host " [PASS] $testName : $details" -ForegroundColor Green
    } else {
        Write-Host " [FAIL] $testName : $details" -ForegroundColor Red
        $global:allPassed = $false
    }
}

Write-Host "==========================================================" -ForegroundColor Cyan
Write-Host " SMART CAMPUS SERVICE PORTAL - COMPREHENSIVE TEST SUITE" -ForegroundColor Cyan
Write-Host "==========================================================" -ForegroundColor Cyan

# -----------------------------------------------------------------------------
# 1. TEST PUBLIC WEB PAGES
# -----------------------------------------------------------------------------
Write-Host "`n--- 1. Testing Public Web Pages ---" -ForegroundColor Yellow

$publicPages = @(
    @{ Url = "$baseUrl/"; Expected = 200; Name = "Home Landing Page" },
    @{ Url = "$baseUrl/Home/About"; Expected = 200; Name = "About Page" },
    @{ Url = "$baseUrl/Home/Contact"; Expected = 200; Name = "Contact & Directory Page" },
    @{ Url = "$baseUrl/Account/Login"; Expected = 200; Name = "Sign In Page" },
    @{ Url = "$baseUrl/Account/Register"; Expected = 200; Name = "Student Registration Page" },
    @{ Url = "$baseUrl/Account/FacultyRegister"; Expected = 200; Name = "Faculty Registration Page" },
    @{ Url = "$baseUrl/Account/ForgotPassword"; Expected = 200; Name = "Forgot Password Page" }
)

foreach ($page in $publicPages) {
    try {
        $res = Invoke-WebRequest -Uri $page.Url -UseBasicParsing -TimeoutSec 10
        $passed = ($res.StatusCode -eq $page.Expected)
        Report-Result $page.Name $passed "Status: $($res.StatusCode)"
    } catch {
        Report-Result $page.Name $false $_.Exception.Message
    }
}

# -----------------------------------------------------------------------------
# 2. TEST REST APIs
# -----------------------------------------------------------------------------
Write-Host "`n--- 2. Testing REST API Endpoints ---" -ForegroundColor Yellow

# A. Student Login via API
$studentToken = ""
try {
    $loginBody = @{ Email = "student.alex@campus.edu"; Password = "Student@123" } | ConvertTo-Json
    $res = Invoke-RestMethod -Uri "$baseUrl/api/auth/login" -Method Post -Body $loginBody -ContentType "application/json"
    $passed = ($res.success -eq $true -and $res.data.token -ne $null)
    $studentToken = $res.data.token
    Report-Result "POST /api/auth/login (Student)" $passed "User: $($res.data.user.fullName), Token Generated"
} catch {
    Report-Result "POST /api/auth/login (Student)" $false $_.Exception.Message
}

# B. Admin Login via API
$adminToken = ""
try {
    $loginBody = @{ Email = "admin@campus.edu"; Password = "Admin@123" } | ConvertTo-Json
    $res = Invoke-RestMethod -Uri "$baseUrl/api/auth/login" -Method Post -Body $loginBody -ContentType "application/json"
    $passed = ($res.success -eq $true -and $res.data.token -ne $null)
    $adminToken = $res.data.token
    Report-Result "POST /api/auth/login (Admin)" $passed "User: $($res.data.user.fullName), Role: $($res.data.user.role)"
} catch {
    Report-Result "POST /api/auth/login (Admin)" $false $_.Exception.Message
}

# C. GET /api/auth/me (with Bearer token)
try {
    $headers = @{ Authorization = "Bearer $studentToken" }
    $res = Invoke-RestMethod -Uri "$baseUrl/api/auth/me" -Method Get -Headers $headers
    $passed = ($res.success -eq $true -and $res.data.user.email -eq "student.alex@campus.edu")
    Report-Result "GET /api/auth/me (Bearer Auth)" $passed "Authenticated as: $($res.data.user.fullName)"
} catch {
    Report-Result "GET /api/auth/me (Bearer Auth)" $false $_.Exception.Message
}

# D. GET /api/services
$testServiceId = ""
try {
    $res = Invoke-RestMethod -Uri "$baseUrl/api/services" -Method Get
    $count = $res.data.Count
    $passed = ($res.success -eq $true -and $count -ge 8)
    if ($count -gt 0) { $testServiceId = $res.data[0].id }
    Report-Result "GET /api/services" $passed "Retrieved $count services (All 8 mandatory services present)"
} catch {
    Report-Result "GET /api/services" $false $_.Exception.Message
}

# E. GET /api/students
try {
    $res = Invoke-RestMethod -Uri "$baseUrl/api/students" -Method Get
    $count = $res.data.Count
    $passed = ($res.success -eq $true -and $count -ge 1)
    Report-Result "GET /api/students" $passed "Retrieved $count student records"
} catch {
    Report-Result "GET /api/students" $false $_.Exception.Message
}

# F. GET /api/faculty
try {
    $res = Invoke-RestMethod -Uri "$baseUrl/api/faculty" -Method Get
    $count = $res.data.Count
    $passed = ($res.success -eq $true -and $count -ge 1)
    Report-Result "GET /api/faculty" $passed "Retrieved $count faculty records"
} catch {
    Report-Result "GET /api/faculty" $false $_.Exception.Message
}

# G. POST /api/requests (Submit new service request via API)
$createdReqId = ""
try {
    $headers = @{ Authorization = "Bearer $studentToken" }
    $reqBody = @{
        ServiceId = $testServiceId
        Priority = "High"
        FormDetails = @{
            "Requirement" = "Automated API Test Verification Request"
            "Purpose" = "Full-stack integration verification"
        }
    } | ConvertTo-Json
    $res = Invoke-RestMethod -Uri "$baseUrl/api/requests" -Method Post -Headers $headers -Body $reqBody -ContentType "application/json"
    $passed = ($res.success -eq $true -and $res.data.requestNumber -ne $null)
    $createdReqId = $res.data.id
    Report-Result "POST /api/requests" $passed "Created Request Number: $($res.data.requestNumber)"
} catch {
    Report-Result "POST /api/requests" $false $_.Exception.Message
}

# H. GET /api/requests & GET /api/requests/:id
try {
    $res = Invoke-RestMethod -Uri "$baseUrl/api/requests/$createdReqId" -Method Get
    $passed = ($res.success -eq $true -and $res.data.id -eq $createdReqId)
    Report-Result "GET /api/requests/:id" $passed "Found Request: $($res.data.requestNumber) Status: $($res.data.status)"
} catch {
    Report-Result "GET /api/requests/:id" $false $_.Exception.Message
}

# I. PUT /api/requests/:id (Update request status via Admin/Faculty API)
try {
    $headers = @{ Authorization = "Bearer $adminToken" }
    $updateBody = @{
        Status = "Approved"
        Remarks = "Approved via automated API test verification."
    } | ConvertTo-Json
    $res = Invoke-RestMethod -Uri "$baseUrl/api/requests/$createdReqId" -Method Put -Headers $headers -Body $updateBody -ContentType "application/json"
    $passed = ($res.success -eq $true -and $res.data.status -eq "Approved")
    Report-Result "PUT /api/requests/:id (Admin Update)" $passed "Status updated to: $($res.data.status)"
} catch {
    Report-Result "PUT /api/requests/:id (Admin Update)" $false $_.Exception.Message
}

# J. POST /api/complaints & GET /api/complaints
try {
    $headers = @{ Authorization = "Bearer $studentToken" }
    $cmpBody = @{
        Category = "IT, Network & WiFi"
        Subject = "API Test Complaint - Switch Port Check"
        Description = "Testing complaint registration via automated suite."
        Location = "CS Lab 3"
        Priority = "Medium"
    } | ConvertTo-Json
    $res = Invoke-RestMethod -Uri "$baseUrl/api/complaints" -Method Post -Headers $headers -Body $cmpBody -ContentType "application/json"
    $passed = ($res.success -eq $true -and $res.data.complaintNumber -ne $null)
    Report-Result "POST /api/complaints" $passed "Created Complaint Ticket: $($res.data.complaintNumber)"
} catch {
    Report-Result "POST /api/complaints" $false $_.Exception.Message
}

# K. GET /api/announcements
try {
    $res = Invoke-RestMethod -Uri "$baseUrl/api/announcements" -Method Get
    $count = $res.data.Count
    $passed = ($res.success -eq $true -and $count -ge 1)
    Report-Result "GET /api/announcements" $passed "Retrieved $count announcements"
} catch {
    Report-Result "GET /api/announcements" $false $_.Exception.Message
}

# L. POST /api/feedback & GET /api/feedback
try {
    $headers = @{ Authorization = "Bearer $studentToken" }
    $fbBody = @{
        ServiceId = $testServiceId
        Rating = 5
        Comments = "Seamless experience verified through REST API test suite."
    } | ConvertTo-Json
    $res = Invoke-RestMethod -Uri "$baseUrl/api/feedback" -Method Post -Headers $headers -Body $fbBody -ContentType "application/json"
    $passed = ($res.success -eq $true)
    Report-Result "POST /api/feedback" $passed "Feedback rating 5 stars recorded"
} catch {
    Report-Result "POST /api/feedback" $false $_.Exception.Message
}

# M. GET /api/analytics/dashboard
try {
    $res = Invoke-RestMethod -Uri "$baseUrl/api/analytics/dashboard" -Method Get
    $passed = ($res.success -eq $true -and $res.data.totalRequests -ge 1 -and $res.data.requestsByStatus -ne $null)
    Report-Result "GET /api/analytics/dashboard" $passed "Total Requests: $($res.data.totalRequests), Avg Rating: $($res.data.averageRating)"
} catch {
    Report-Result "GET /api/analytics/dashboard" $false $_.Exception.Message
}

# -----------------------------------------------------------------------------
# 3. TEST ROLE AUTHORIZATION & PERMISSION BOUNDARIES
# -----------------------------------------------------------------------------
Write-Host "`n--- 3. Testing Role Authorization & Route Protection ---" -ForegroundColor Yellow

# Unauthenticated access to /Admin should redirect to /Account/Login
try {
    $res = Invoke-WebRequest -Uri "$baseUrl/Admin" -UseBasicParsing -MaximumRedirection 0 -ErrorAction SilentlyContinue
    # Should either be 302 redirect or handled
    $passed = ($res.StatusCode -eq 302 -and $res.Headers.Location -like "*Login*")
    Report-Result "Unauthorized /Admin Access" $passed "Redirected to: $($res.Headers.Location)"
} catch {
    if ($_.Exception.Response.StatusCode.value__ -eq 302) {
        $loc = $_.Exception.Response.Headers["Location"]
        $passed = ($loc -like "*Login*")
        Report-Result "Unauthorized /Admin Access" $passed "Redirected to: $loc"
    } else {
        Report-Result "Unauthorized /Admin Access" $false $_.Exception.Message
    }
}

# Unauthenticated access to /Student should redirect to /Account/Login
try {
    $res = Invoke-WebRequest -Uri "$baseUrl/Student" -UseBasicParsing -MaximumRedirection 0 -ErrorAction SilentlyContinue
    $passed = ($res.StatusCode -eq 302 -and $res.Headers.Location -like "*Login*")
    Report-Result "Unauthorized /Student Access" $passed "Redirected to: $($res.Headers.Location)"
} catch {
    if ($_.Exception.Response.StatusCode.value__ -eq 302) {
        $loc = $_.Exception.Response.Headers["Location"]
        $passed = ($loc -like "*Login*")
        Report-Result "Unauthorized /Student Access" $passed "Redirected to: $loc"
    } else {
        Report-Result "Unauthorized /Student Access" $false $_.Exception.Message
    }
}

# -----------------------------------------------------------------------------
# 4. TEST AUTHENTICATED SESSION DASHBOARDS
# -----------------------------------------------------------------------------
Write-Host "`n--- 4. Testing Authenticated Web Dashboards (Cookie Sessions) ---" -ForegroundColor Yellow

function Test-WebSession($email, $password, $dashboardUrl, $roleName) {
    $session = New-Object Microsoft.PowerShell.Commands.WebRequestSession
    # 1. Fetch Login page to get AntiForgeryToken
    $loginPage = Invoke-WebRequest -Uri "$baseUrl/Account/Login" -WebSession $session -UseBasicParsing
    
    # 2. Submit login form
    $formData = @{
        Email = $email
        Password = $password
        RememberMe = "false"
    }
    
    # Extract token if present
    if ($loginPage.Content -match 'name="__RequestVerificationToken" type="hidden" value="([^"]+)"') {
        $formData["__RequestVerificationToken"] = $matches[1]
    }
    
    $loginRes = Invoke-WebRequest -Uri "$baseUrl/Account/Login" -Method Post -Body $formData -WebSession $session -UseBasicParsing -MaximumRedirection 0 -ErrorAction SilentlyContinue
    
    # 3. Access target dashboard
    $dashRes = Invoke-WebRequest -Uri $dashboardUrl -WebSession $session -UseBasicParsing
    $passed = ($dashRes.StatusCode -eq 200 -and $dashRes.Content -like "*SmartCampus*")
    Report-Result "$roleName Dashboard ($dashboardUrl)" $passed "Status: $($dashRes.StatusCode), HTML Size: $($dashRes.Content.Length) bytes"
    
    return $session
}

$studentSession = Test-WebSession "student.alex@campus.edu" "Student@123" "$baseUrl/Student" "Student"
$facultySession = Test-WebSession "faculty.sharma@campus.edu" "Faculty@123" "$baseUrl/Faculty" "Faculty"
$adminSession   = Test-WebSession "admin@campus.edu" "Admin@123" "$baseUrl/Admin" "Administrator"

# Additional Student Pages with active session
$studentSubPages = @(
    @{ Url = "$baseUrl/Student/Services"; Name = "Student Services Catalog" },
    @{ Url = "$baseUrl/Student/MyRequests"; Name = "Student My Requests" },
    @{ Url = "$baseUrl/Student/Complaints"; Name = "Student Complaints Desk" },
    @{ Url = "$baseUrl/Student/Feedback"; Name = "Student Feedback Page" },
    @{ Url = "$baseUrl/Student/Announcements"; Name = "Student Announcements" },
    @{ Url = "$baseUrl/Student/Notifications"; Name = "Student Notifications" },
    @{ Url = "$baseUrl/Student/Voucher/REQ-2026-0001"; Name = "Student Printable Voucher Slip" }
)
foreach ($sp in $studentSubPages) {
    try {
        $res = Invoke-WebRequest -Uri $sp.Url -WebSession $studentSession -UseBasicParsing
        $passed = ($res.StatusCode -eq 200)
        Report-Result $sp.Name $passed "Status: $($res.StatusCode)"
    } catch {
        Report-Result $sp.Name $false $_.Exception.Message
    }
}

# Additional Faculty Pages with active session
$facultySubPages = @(
    @{ Url = "$baseUrl/Faculty/AssignedRequests"; Name = "Faculty Assigned Queue" },
    @{ Url = "$baseUrl/Faculty/Complaints"; Name = "Faculty Complaints Desk" },
    @{ Url = "$baseUrl/Faculty/Services"; Name = "Faculty Department Services" },
    @{ Url = "$baseUrl/Faculty/Feedback"; Name = "Faculty Feedback View" }
)
foreach ($fp in $facultySubPages) {
    try {
        $res = Invoke-WebRequest -Uri $fp.Url -WebSession $facultySession -UseBasicParsing
        $passed = ($res.StatusCode -eq 200)
        Report-Result $fp.Name $passed "Status: $($res.StatusCode)"
    } catch {
        Report-Result $fp.Name $false $_.Exception.Message
    }
}

# Additional Admin Pages with active session
$adminSubPages = @(
    @{ Url = "$baseUrl/Admin/Users"; Name = "Admin User Accounts" },
    @{ Url = "$baseUrl/Admin/Students"; Name = "Admin Student Directory" },
    @{ Url = "$baseUrl/Admin/Faculty"; Name = "Admin Faculty Directory" },
    @{ Url = "$baseUrl/Admin/Services"; Name = "Admin Services CRUD" },
    @{ Url = "$baseUrl/Admin/Requests"; Name = "Admin Requests Oversight" },
    @{ Url = "$baseUrl/Admin/Complaints"; Name = "Admin Complaints Center" },
    @{ Url = "$baseUrl/Admin/Announcements"; Name = "Admin Announcements Manager" },
    @{ Url = "$baseUrl/Admin/Feedback"; Name = "Admin Feedback Analytics" },
    @{ Url = "$baseUrl/Admin/Reports"; Name = "Admin Reports & Export" },
    @{ Url = "$baseUrl/Admin/ExportRequestsCsv"; Name = "Admin Export Requests CSV" }
)
foreach ($ap in $adminSubPages) {
    try {
        $res = Invoke-WebRequest -Uri $ap.Url -WebSession $adminSession -UseBasicParsing
        $passed = ($res.StatusCode -eq 200)
        Report-Result $ap.Name $passed "Status: $($res.StatusCode)"
    } catch {
        Report-Result $ap.Name $false $_.Exception.Message
    }
}

Write-Host "`n==========================================================" -ForegroundColor Cyan
if ($allPassed) {
    Write-Host " ALL TEST SUITES PASSED PERFECTLY (100% SUCCESS RATE)!" -ForegroundColor Green
} else {
    Write-Host " SOME TESTS FAILED. PLEASE REVIEW LOGS." -ForegroundColor Red
}
Write-Host "==========================================================" -ForegroundColor Cyan
