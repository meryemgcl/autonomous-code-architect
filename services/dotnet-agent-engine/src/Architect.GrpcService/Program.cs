using Architect.Agents.Abstractions;
using Architect.Agents.Models;
using Architect.Agents.Orchestrator;
using Architect.Agents.Specialists;
using Architect.Core.Models;
using Architect.GrpcService.Services;
using Architect.Infrastructure.History;
using Architect.Infrastructure.Memory;
using Architect.RoslynParser.Remediation;
using Architect.RoslynParser.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Server.Kestrel.Core;

var builder = WebApplication.CreateBuilder(args);

// Kestrel Yapılandırması
builder.WebHost.ConfigureKestrel(options =>
{
    options.ListenAnyIP(5000, o => o.Protocols = HttpProtocols.Http1AndHttp2);
    options.ListenAnyIP(5001, o => o.Protocols = HttpProtocols.Http2);
});

// Dependency Injection
builder.Services.AddGrpc();
builder.Services.AddOpenApi();
builder.Services.AddSingleton<IVectorMemoryService, VectorMemoryService>();
builder.Services.AddSingleton<ICSharpAstAnalyzer, CSharpAstAnalyzer>();
builder.Services.AddSingleton<ISelfHealingRemediationEngine, SelfHealingRemediationEngine>();
builder.Services.AddSingleton<IAnalysisHistoryService, AnalysisHistoryService>();  // FAZ C

builder.Services.AddSingleton<IAgent, ReviewerAgent>();
builder.Services.AddSingleton<IAgent, SecurityAgent>();
builder.Services.AddSingleton<IAgent, QaTestWriterAgent>();
builder.Services.AddSingleton<IArbiterAgent, ArbiterAgent>();
builder.Services.AddSingleton<IStreamingArbiterAgent, StreamingArbiterAgent>();

var app = builder.Build();

app.UseDefaultFiles();
app.UseStaticFiles();

if (app.Environment.IsDevelopment()) app.MapOpenApi();

app.MapGrpcService<CodeAnalysisGrpcServiceImpl>();

// ── GET /api/v1/health ──────────────────────────────────────────────────────
app.MapGet("/api/v1/health", () => Results.Ok(new
{
    Service = "dotnet-agent-engine",
    Status = "Healthy",
    Version = "1.0.0-net9",
    Features = new[] { "RoslynAST-8Rules", "RAGVectorMemory", "SelfHealing", "ParallelAgents", "AnalysisHistory" },
    Timestamp = DateTimeOffset.UtcNow
}));

// ── POST /api/v1/analyze — Deterministik AST (0 AI) ────────────────────────
app.MapPost("/api/v1/analyze", ([FromBody] AnalysisRequestDto req,
    ICSharpAstAnalyzer analyzer,
    IAnalysisHistoryService history) =>
{
    var id = req.RequestId ?? Guid.NewGuid().ToString();
    var result = analyzer.AnalyzeCode(id, req.FilePath ?? "sample.cs", req.SourceCode ?? "");
    history.Record(new AnalysisHistoryEntry(
        id, req.FilePath ?? "sample.cs", req.Language ?? "CSHARP",
        DateTimeOffset.UtcNow, result.Violations.Count,
        result.ExecutionTimeMs, false, false, false));
    return Results.Ok(result);
});

// ── POST /api/v1/debate — Paralel Ajan Tartışması (FAZ D) ──────────────────
app.MapPost("/api/v1/debate", async ([FromBody] AnalysisRequestDto req,
    ICSharpAstAnalyzer analyzer,
    IArbiterAgent arbiter,
    IAnalysisHistoryService history) =>
{
    var id = req.RequestId ?? Guid.NewGuid().ToString();
    var astResult = analyzer.AnalyzeCode(id, req.FilePath ?? "sample.cs", req.SourceCode ?? "");
    var ctx = new DebateContext(id, req.FilePath ?? "sample.cs",
        req.SourceCode ?? "", req.Language ?? "CSHARP", astResult);
    var consensus = await arbiter.ConductDebateAsync(ctx);
    history.Record(new AnalysisHistoryEntry(
        id, req.FilePath ?? "sample.cs", req.Language ?? "CSHARP",
        DateTimeOffset.UtcNow, astResult.Violations.Count,
        astResult.ExecutionTimeMs, false, true, !consensus.RequiresHumanApproval));
    return Results.Ok(consensus);
});

// ── POST /api/v1/remediate — Self-Healing AST Rewriter ─────────────────────
app.MapPost("/api/v1/remediate", ([FromBody] AnalysisRequestDto req,
    ISelfHealingRemediationEngine engine,
    IAnalysisHistoryService history) =>
{
    var id = req.RequestId ?? Guid.NewGuid().ToString();
    var result = engine.RemediateSourceCode(id, req.FilePath ?? "sample.cs", req.SourceCode ?? "");
    history.Record(new AnalysisHistoryEntry(
        id, req.FilePath ?? "sample.cs", req.Language ?? "CSHARP",
        DateTimeOffset.UtcNow, 0, 0, result.Healed, false, false));
    return Results.Ok(result);
});

// ── GET /api/v1/history — Son 20 analiz kaydı ──────────────────────────────
app.MapGet("/api/v1/history", ([FromQuery] int count, IAnalysisHistoryService history) =>
    Results.Ok(history.GetRecent(count > 0 ? count : 20)));

// ── GET /api/v1/history/stats — İstatistikler ──────────────────────────────
app.MapGet("/api/v1/history/stats", (IAnalysisHistoryService history) =>
    Results.Ok(history.GetStats()));

// ── DELETE /api/v1/history — Geçmişi temizle ───────────────────────────────
app.MapDelete("/api/v1/history", (IAnalysisHistoryService history) =>
{
    history.Clear();
    return Results.Ok(new { Cleared = true });
});

// ── GET /api/v1/memory/rules — RAG Vektör Hafızası ─────────────────────────
app.MapGet("/api/v1/memory/rules", async ([FromQuery] string query, IVectorMemoryService mem) =>
    Results.Ok(await mem.FindRelevantRulesAsync(query ?? "Clean Architecture")));

app.Run();

public class AnalysisRequestDto
{
    public string? RequestId { get; set; }
    public string? FilePath { get; set; }
    public string? SourceCode { get; set; }
    public string? Language { get; set; }
    public bool EnableAiAgents { get; set; }
}
