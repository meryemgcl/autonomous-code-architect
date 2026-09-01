$sampleCode = Get-Content -Path "./deploy/samples/SampleViolationService.cs" -Raw

$body = @{
    requestId = "req-live-100"
    filePath = "src/Domain/Orders/OrderService.cs"
    sourceCode = $sampleCode
    language = "CSHARP"
    enableAiAgents = $false
} | ConvertTo-Json

Write-Host "=== 1. DETERMINISTIC AST TEST (NO AI) ===" -ForegroundColor Cyan
$resp1 = Invoke-RestMethod -Uri "http://localhost:5000/api/v1/analyze" -Method Post -Body $body -ContentType "application/json"
$resp1 | ConvertTo-Json -Depth 5

Write-Host "`n=== 2. AUTONOMOUS MULTI-AGENT DEBATE & CONSENSUS (AUTOGPT MODE) ===" -ForegroundColor Yellow
$resp2 = Invoke-RestMethod -Uri "http://localhost:5000/api/v1/debate" -Method Post -Body $body -ContentType "application/json"
$resp2 | ConvertTo-Json -Depth 5
