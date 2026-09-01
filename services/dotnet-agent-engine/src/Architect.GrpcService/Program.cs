using Architect.Agents.Abstractions;
using Architect.Agents.Models;
using Architect.Agents.Orchestrator;
using Architect.Agents.Specialists;
using Architect.Core.Models;
using Architect.GrpcService.Services;
using Architect.Infrastructure.Memory;
using Architect.RoslynParser.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Server.Kestrel.Core;

var builder = WebApplication.CreateBuilder(args);

// Kestrel Yapılandırması (HTTP/1.1 REST & HTTP/2 gRPC)
builder.WebHost.ConfigureKestrel(options =>
{
    options.ListenAnyIP(5000, listenOptions =>
    {
        listenOptions.Protocols = HttpProtocols.Http1AndHttp2;
    });

    options.ListenAnyIP(5001, listenOptions =>
    {
        listenOptions.Protocols = HttpProtocols.Http2;
    });
});

// Dependency Injection (Clean Architecture)
builder.Services.AddGrpc();
builder.Services.AddOpenApi();

builder.Services.AddSingleton<IVectorMemoryService, VectorMemoryService>();
builder.Services.AddSingleton<ICSharpAstAnalyzer, CSharpAstAnalyzer>();

builder.Services.AddSingleton<IAgent, ReviewerAgent>();
builder.Services.AddSingleton<IAgent, SecurityAgent>();
builder.Services.AddSingleton<IAgent, QaTestWriterAgent>();
builder.Services.AddSingleton<IArbiterAgent, ArbiterAgent>();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

// 1. gRPC Endpoint
app.MapGrpcService<CodeAnalysisGrpcServiceImpl>();

// 2. Health Endpoint
app.MapGet("/api/v1/health", () => Results.Ok(new
{
    Service = "dotnet-agent-engine",
    Status = "Healthy",
    Version = "1.0.0-net9",
    RagVectorMemory = "Active",
    Timestamp = DateTimeOffset.UtcNow
}));

// 3. REST AST Analiz Endpoint'i (AI olmadan anında çalışan mod)
app.MapPost("/api/v1/analyze", ([FromBody] AnalysisRequestDto request, ICSharpAstAnalyzer analyzer) =>
{
    var requestId = request.RequestId ?? Guid.NewGuid().ToString();
    var result = analyzer.AnalyzeCode(requestId, request.FilePath ?? "sample.cs", request.SourceCode ?? string.Empty);
    return Results.Ok(result);
});

// 4. REST Otonom Ajan Tartışması & Uzlaşı Endpoint'i (AutoGPT + RAG Mode)
app.MapPost("/api/v1/debate", async ([FromBody] AnalysisRequestDto request, ICSharpAstAnalyzer analyzer, IArbiterAgent arbiter) =>
{
    var requestId = request.RequestId ?? Guid.NewGuid().ToString();
    var deterministicResult = analyzer.AnalyzeCode(requestId, request.FilePath ?? "sample.cs", request.SourceCode ?? string.Empty);

    var context = new DebateContext(
        RequestId: requestId,
        FilePath: request.FilePath ?? "sample.cs",
        SourceCode: request.SourceCode ?? string.Empty,
        Language: request.Language ?? "CSHARP",
        DeterministicFindings: deterministicResult
    );

    var consensus = await arbiter.ConductDebateAsync(context);
    return Results.Ok(consensus);
});

// 5. RAG Kurumsal Vektör Hafızası Arama Endpoint'i
app.MapGet("/api/v1/memory/rules", async ([FromQuery] string query, IVectorMemoryService vectorMemory) =>
{
    var rules = await vectorMemory.FindRelevantRulesAsync(query ?? "Clean Architecture");
    return Results.Ok(rules);
});

app.Run();

public class AnalysisRequestDto
{
    public string? RequestId { get; set; }
    public string? FilePath { get; set; }
    public string? SourceCode { get; set; }
    public string? Language { get; set; }
    public bool EnableAiAgents { get; set; }
}
