using Architect.RoslynParser.Remediation;
using FluentAssertions;
using Xunit;

namespace Architect.RoslynParser.Tests;

public class SelfHealingTests
{
    private readonly SelfHealingRemediationEngine _healingEngine = new();

    [Fact]
    public void Should_Automatically_Heal_AsyncVoid_And_EmptyCatch_In_Source_Code()
    {
        // Arrange
        var brokenCode = @"
            public class PaymentService
            {
                public async void ExecutePayment()
                {
                    try
                    {
                        var x = 1;
                    }
                    catch (Exception)
                    {
                    }
                }
            }";

        // Act
        var result = _healingEngine.RemediateSourceCode("heal-1", "PaymentService.cs", brokenCode);

        // Assert
        result.Healed.Should().BeTrue();
        result.AppliedFixes.Should().HaveCount(2);
        result.HealedSourceCode.Should().Contain("public async Task ExecutePayment()");
        result.HealedSourceCode.Should().Contain("throw;");
        result.HealedSourceCode.Should().NotContain("async void");
    }
}
