$sampleCode = @"
namespace MyCompany.Domain.Orders;

using Microsoft.AspNetCore.Mvc;
using System;
using System.Threading.Tasks;

public class OrderService
{
    private string dbApiKey = "sk_live_998877665544332211";

    public async void ProcessPaymentAsync()
    {
        try
        {
            await Task.Delay(50);
        }
        catch (Exception)
        {
        }
    }
}
"@

$payloadObj = @{
    requestId = "req-live-100"
    filePath = "src/Domain/Orders/OrderService.cs"
    sourceCode = $sampleCode
    language = "CSHARP"
    enableAiAgents = $false
}

$jsonBody = $payloadObj | ConvertTo-Json
$utf8Bytes = [System.Text.Encoding]::UTF8.GetBytes($jsonBody)

Write-Host "=== 1. DETERMINISTIC AST TEST (NO AI) ===" -ForegroundColor Cyan
try {
    $resp1 = Invoke-RestMethod -Uri "http://localhost:5000/api/v1/analyze" -Method Post -Body $utf8Bytes -ContentType "application/json; charset=utf-8"
    $resp1 | ConvertTo-Json -Depth 5
} catch {
    Write-Host "Fallback to curl for endpoint 1..." -ForegroundColor DarkGray
    curl.exe -s -X POST http://localhost:5000/api/v1/analyze -H "Content-Type: application/json" -d @deploy/payload.json
}

Write-Host "`n=== 2. AUTONOMOUS MULTI-AGENT DEBATE & CONSENSUS (AUTOGPT MODE) ===" -ForegroundColor Yellow
try {
    $resp2 = Invoke-RestMethod -Uri "http://localhost:5000/api/v1/debate" -Method Post -Body $utf8Bytes -ContentType "application/json; charset=utf-8"
    $resp2 | ConvertTo-Json -Depth 5
} catch {
    Write-Host "Fallback to curl for endpoint 2..." -ForegroundColor DarkGray
    curl.exe -s -X POST http://localhost:5000/api/v1/debate -H "Content-Type: application/json" -d @deploy/payload.json
}
