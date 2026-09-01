using Architect.Infrastructure.Memory;
using FluentAssertions;
using Xunit;

namespace Architect.RoslynParser.Tests;

public class VectorMemoryTests
{
    private readonly VectorMemoryService _memoryService = new();

    [Fact]
    public async Task Should_Find_CleanArchitecture_Rules_When_Querying_Domain_Dependencies()
    {
        // Arrange
        var query = "Clean Architecture Domain Infrastructure Dependency Inversion";

        // Act
        var results = await _memoryService.FindRelevantRulesAsync(query, topK: 2);

        // Assert
        results.Should().NotBeEmpty();
        results[0].Rule.RuleCode.Should().Be("ENT-ARCH-001");
        results[0].SimilarityScore.Should().BeGreaterThan(0.5);
    }

    [Fact]
    public async Task Should_Find_Security_Rules_When_Querying_Secrets()
    {
        // Arrange
        var query = "Password Secret ApiKey Token Security Leak";

        // Act
        var results = await _memoryService.FindRelevantRulesAsync(query, topK: 1);

        // Assert
        results.Should().NotBeEmpty();
        results[0].Rule.RuleCode.Should().Be("ENT-SEC-001");
    }

    [Fact]
    public async Task Should_Return_Empty_When_Query_Is_Empty()
    {
        // Act
        var results = await _memoryService.FindRelevantRulesAsync(string.Empty);

        // Assert
        results.Should().BeEmpty();
    }
}
