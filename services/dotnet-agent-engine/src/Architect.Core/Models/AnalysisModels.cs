namespace Architect.Core.Models;

public enum AnalysisSeverity
{
    Info = 1,
    Warning = 2,
    Error = 3,
    Critical = 4
}

public enum RuleCategory
{
    CleanArchitecture,
    SolidPrinciples,
    Security,
    CodeSmell,
    Performance
}

public record CodeViolation(
    string RuleId,
    string RuleName,
    string Description,
    AnalysisSeverity Severity,
    int StartLine,
    int EndLine,
    string SuggestedFix,
    RuleCategory Category
);

public record CodeMetrics(
    int LinesOfCode,
    int CyclomaticComplexity,
    int MaintainabilityIndex,
    int MethodCount,
    int ClassCount
);

public record AnalysisResult(
    string RequestId,
    string FilePath,
    bool Success,
    string? ErrorMessage,
    CodeMetrics Metrics,
    IReadOnlyList<CodeViolation> Violations,
    long ExecutionTimeMs
);
