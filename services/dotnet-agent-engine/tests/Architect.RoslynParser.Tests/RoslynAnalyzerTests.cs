using Architect.Core.Models;
using Architect.RoslynParser.Services;
using FluentAssertions;
using Xunit;

namespace Architect.RoslynParser.Tests;

public class RoslynAnalyzerTests
{
    private readonly CSharpAstAnalyzer _analyzer = new();

    [Fact]
    public void Should_Detect_AsyncVoid_Method_As_Critical_Violation()
    {
        // Arrange
        var testCode = @"
            using System;
            using System.Threading.Tasks;

            public class PaymentService
            {
                public async void ProcessPaymentAsync()
                {
                    await Task.Delay(100);
                }
            }";

        // Act
        var result = _analyzer.AnalyzeCode("req-1", "PaymentService.cs", testCode);

        // Assert
        result.Success.Should().BeTrue();
        result.Violations.Should().Contain(v => v.RuleId == "ARCH-CS-001" && v.Severity == AnalysisSeverity.Critical);
        result.Violations.First(v => v.RuleId == "ARCH-CS-001").SuggestedFix.Should().Contain("async Task");
    }

    [Fact]
    public void Should_Detect_EmptyCatchBlock_As_Warning()
    {
        // Arrange
        var testCode = @"
            public class DataProcessor
            {
                public void Run()
                {
                    try
                    {
                        int.Parse(""abc"");
                    }
                    catch
                    {
                    }
                }
            }";

        // Act
        var result = _analyzer.AnalyzeCode("req-2", "DataProcessor.cs", testCode);

        // Assert
        result.Success.Should().BeTrue();
        result.Violations.Should().Contain(v => v.RuleId == "ARCH-CS-002" && v.Severity == AnalysisSeverity.Warning);
    }

    [Fact]
    public void Should_Detect_Hardcoded_Secret_In_Code()
    {
        // Arrange
        var testCode = @"
            public class CloudClient
            {
                private string apiKey = ""sk_live_1234567890abcdef"";
                private string dbPassword = ""SuperSecretP@ssword123"";
            }";

        // Act
        var result = _analyzer.AnalyzeCode("req-3", "CloudClient.cs", testCode);

        // Assert
        result.Success.Should().BeTrue();
        result.Violations.Should().Contain(v => v.RuleId == "ARCH-SEC-001" && v.Category == RuleCategory.Security);
    }

    [Fact]
    public void Should_Detect_Clean_Architecture_Violation_In_Domain_Layer()
    {
        // Arrange
        var testCode = @"
            namespace MyApp.Domain.Entities;

            using Microsoft.AspNetCore.Mvc;
            using Microsoft.EntityFrameworkCore;

            public class Order
            {
                public int Id { get; set; }
            }";

        // Act
        var result = _analyzer.AnalyzeCode("req-4", "Order.cs", testCode);

        // Assert
        result.Success.Should().BeTrue();
        result.Violations.Should().Contain(v => v.RuleId == "ARCH-CA-001" && v.Category == RuleCategory.CleanArchitecture);
    }

    [Fact]
    public void Should_Calculate_Accurate_Cyclomatic_Complexity()
    {
        // Arrange
        var testCode = @"
            public class DecisionMaker
            {
                public void Check(int a, int b, bool flag)
                {
                    if (a > 0 && b > 0)
                    {
                        while (a > 0) a--;
                    }
                    else if (flag || b < 0)
                    {
                        var x = flag ? 1 : 2;
                    }
                }
            }";

        // Act
        var result = _analyzer.AnalyzeCode("req-5", "DecisionMaker.cs", testCode);

        // Assert
        result.Success.Should().BeTrue();
        result.Metrics.CyclomaticComplexity.Should().BeGreaterThan(5);
        result.Metrics.MethodCount.Should().Be(1);
        result.Metrics.ClassCount.Should().Be(1);
    }
}
