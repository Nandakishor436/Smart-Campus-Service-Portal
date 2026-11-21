$s = New-Object Microsoft.PowerShell.Commands.WebRequestSession
$lp = Invoke-WebRequest -Uri "http://localhost:62359/Account/Login" -WebSession $s -UseBasicParsing
$token = ""
if ($lp.Content -match 'name="__RequestVerificationToken" type="hidden" value="([^"]+)"') { $token = $matches[1] }

$loginRes = Invoke-WebRequest -Uri "http://localhost:62359/Account/Login" -Method Post -Body @{ Email="student.alex@campus.edu"; Password="Student@123"; RememberMe="false"; __RequestVerificationToken=$token } -WebSession $s -UseBasicParsing -MaximumRedirection 0 -ErrorAction SilentlyContinue

try {
    $res = Invoke-WebRequest -Uri "http://localhost:62359/Student/Voucher/REQ-2026-0001" -WebSession $s -UseBasicParsing
    Write-Host "Voucher status: $($res.StatusCode)"
} catch {
    if ($_.Exception.Response) {
        $reader = New-Object System.IO.StreamReader($_.Exception.Response.GetResponseStream())
        $body = $reader.ReadToEnd()
        [System.IO.File]::WriteAllText("voucher_error.html", $body)
        Write-Host "Saved error to voucher_error.html ($($body.Length) chars)"
    }
}
