using Architect.Core.Models;

namespace Architect.Agents.Models;

public record DebateContext(
    string RequestId,
    string FilePath,
    string SourceCode,
    string Language, // "CSHARP" or "JAVA"
    AnalysisResult DeterministicFindings
);

public record AgentOpinion(
    string AgentName,
    string RoleTitle,
    string Stance, // "APPROVE", "REQUEST_CHANGES", "NEEDS_SECURITY_FIX"
    string ArgumentsMarkdown,
    IReadOnlyList<string> ProposedCodePatches
);

public record DebateConsensus(
    string RequestId,
    IReadOnlyList<AgentOpinion> Opinions,
    string FinalConsensusSummary,
    string GeneratedUnitTestCode,
    string SuggestedRefactoredCode,
    bool RequiresHumanApproval
);
