namespace Architect.Core.Models;

public record ArchitectureRule(
    int Id,
    string RuleCode,
    string RuleName,
    string Category,
    string Description,
    string RecommendedFix,
    float[]? Embedding = null
);

public record SemanticSearchResult(
    ArchitectureRule Rule,
    double SimilarityScore
);
