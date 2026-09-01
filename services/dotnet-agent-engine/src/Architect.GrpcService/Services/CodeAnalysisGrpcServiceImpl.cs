using Architect.Agents.Models;
using Architect.Agents.Orchestrator;
using Architect.Contracts.Grpc;
using Architect.RoslynParser.Services;
using Grpc.Core;

namespace Architect.GrpcService.Services;

public class CodeAnalysisGrpcServiceImpl : CodeAnalysisService.CodeAnalysisServiceBase
{
    private readonly ICSharpAstAnalyzer _csharpAnalyzer;
    private readonly IArbiterAgent _arbiterAgent;
    private readonly ILogger<CodeAnalysisGrpcServiceImpl> _logger;

    public CodeAnalysisGrpcServiceImpl(
        ICSharpAstAnalyzer csharpAnalyzer,
        IArbiterAgent arbiterAgent,
        ILogger<CodeAnalysisGrpcServiceImpl> logger)
    {
        _csharpAnalyzer = csharpAnalyzer;
        _arbiterAgent = arbiterAgent;
        _logger = logger;
    }

    public override Task<HealthCheckResponse> CheckHealth(HealthCheckRequest request, ServerCallContext context)
    {
        return Task.FromResult(new HealthCheckResponse
        {
            Status = "SERVING",
            Version = "1.0.0-net9",
            Timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()
        });
    }

    public override Task<CodeAnalysisResponse> AnalyzeCodeSnippet(CodeAnalysisRequest request, ServerCallContext context)
    {
        _logger.LogInformation("gRPC AnalyzeCodeSnippet received for file: {FilePath}", request.FilePath);

        var result = _csharpAnalyzer.AnalyzeCode(
            request.RequestId,
            request.FilePath,
            request.SourceCode
        );

        var response = new CodeAnalysisResponse
        {
            RequestId = result.RequestId,
            Success = result.Success,
            ErrorMessage = result.ErrorMessage ?? string.Empty,
            ExecutionTimeMs = result.ExecutionTimeMs,
            Metrics = new Architect.Contracts.Grpc.CodeMetrics
            {
                LinesOfCode = result.Metrics.LinesOfCode,
                CyclomaticComplexity = result.Metrics.CyclomaticComplexity,
                MaintainabilityIndex = result.Metrics.MaintainabilityIndex,
                MethodCount = result.Metrics.MethodCount,
                ClassCount = result.Metrics.ClassCount
            }
        };

        foreach (var v in result.Violations)
        {
            response.Violations.Add(new Architect.Contracts.Grpc.CodeViolation
            {
                RuleId = v.RuleId,
                RuleName = v.RuleName,
                Description = v.Description,
                Severity = (Architect.Contracts.Grpc.SeverityLevel)(int)v.Severity,
                StartLine = v.StartLine,
                EndLine = v.EndLine,
                SuggestedFix = v.SuggestedFix,
                Category = v.Category.ToString()
            });
        }

        return Task.FromResult(response);
    }

    public override async Task<AgentDebateResponse> ExecuteAgentDebate(AgentDebateRequest request, ServerCallContext context)
    {
        _logger.LogInformation("gRPC ExecuteAgentDebate started for RequestId: {RequestId}", request.RequestId);

        // AST bulgularını domain modeline dönüştür
        var deterministicResult = _csharpAnalyzer.AnalyzeCode(
            request.RequestId,
            request.FilePath,
            request.SourceCode
        );

        var debateContext = new DebateContext(
            RequestId: request.RequestId,
            FilePath: request.FilePath,
            SourceCode: request.SourceCode,
            Language: request.Language == ProgrammingLanguage.LanguageCsharp ? "CSHARP" : "JAVA",
            DeterministicFindings: deterministicResult
        );

        var consensus = await _arbiterAgent.ConductDebateAsync(debateContext);

        var response = new AgentDebateResponse
        {
            RequestId = consensus.RequestId,
            FinalConsensusSummary = consensus.FinalConsensusSummary,
            GeneratedUnitTestCode = consensus.GeneratedUnitTestCode,
            SuggestedRefactoredCode = consensus.SuggestedRefactoredCode,
            RequiresHumanApproval = consensus.RequiresHumanApproval
        };

        foreach (var op in consensus.Opinions)
        {
            var opinionProto = new Architect.Contracts.Grpc.AgentOpinion
            {
                AgentName = op.AgentName,
                RoleTitle = op.RoleTitle,
                Stance = op.Stance,
                ArgumentsMarkdown = op.ArgumentsMarkdown
            };
            opinionProto.ProposedCodePatches.AddRange(op.ProposedCodePatches);
            response.Opinions.Add(opinionProto);
        }

        return response;
    }
}
