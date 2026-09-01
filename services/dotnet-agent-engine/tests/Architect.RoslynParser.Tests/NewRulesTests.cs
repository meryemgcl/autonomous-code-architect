using Architect.Core.Models;
using Architect.RoslynParser.Rules;
using FluentAssertions;
using Microsoft.CodeAnalysis.CSharp;
using Xunit;

namespace Architect.RoslynParser.Tests;

public class NewRulesTests
{
    // ── ARCH-CS-004: MagicNumber ──

    [Fact]
    public void MagicNumberRule_Should_Detect_Unlabeled_Numeric_Literals()
    {
        var code = @"
public class PricingService
{
    public decimal Calculate(int qty)
    {
        if (qty > 100) return 0.15m;
        if (qty > 50)  return 0.08m;
        return 0m;
    }
}";
        var tree = CSharpSyntaxTree.ParseText(code);
        var rule = new MagicNumberRule();

        var violations = rule.Analyze(tree, semanticModel: null).ToList();

        // 100 ve 50 yakalanmalı (0 ve 1 izin verilir)
        violations.Should().NotBeEmpty();
        violations.Should().Contain(v => v.Description.Contains("100") || v.Description.Contains("50"));
    }

    [Fact]
    public void MagicNumberRule_Should_Allow_Zero_And_One()
    {
        var code = @"
public class Counter
{
    public int Reset() => 0;
    public int Initial() => 1;
}";
        var tree = CSharpSyntaxTree.ParseText(code);
        var rule = new MagicNumberRule();

        var violations = rule.Analyze(tree, semanticModel: null).ToList();

        violations.Should().BeEmpty();
    }

    // ── ARCH-CS-005: NullReturn ──

    [Fact]
    public void NullReturnRule_Should_Detect_Null_Return_In_Public_Method()
    {
        var code = @"
public class OrderRepository
{
    public Order FindById(int id)
    {
        if (id <= 0) return null;
        return new Order();
    }
}
public class Order { }";
        var tree = CSharpSyntaxTree.ParseText(code);
        var rule = new NullReturnFromPublicMethodRule();

        var violations = rule.Analyze(tree, semanticModel: null).ToList();

        violations.Should().NotBeEmpty();
        violations[0].RuleId.Should().Be("ARCH-CS-005");
        violations[0].Description.Should().Contain("FindById");
    }

    [Fact]
    public void NullReturnRule_Should_Not_Flag_Private_Methods()
    {
        var code = @"
public class OrderRepository
{
    private Order InternalFind(int id) => null;
}
public class Order { }";
        var tree = CSharpSyntaxTree.ParseText(code);
        var rule = new NullReturnFromPublicMethodRule();

        var violations = rule.Analyze(tree, semanticModel: null).ToList();

        violations.Should().BeEmpty();
    }

    // ── ARCH-CS-006: StaticMutable ──

    [Fact]
    public void StaticMutableFieldRule_Should_Detect_Static_Non_Readonly_Field()
    {
        var code = @"
public class RequestCounter
{
    private static int _counter = 0;
    private static readonly int _maxRetry = 3;
}";
        var tree = CSharpSyntaxTree.ParseText(code);
        var rule = new StaticMutableFieldRule();

        var violations = rule.Analyze(tree, semanticModel: null).ToList();

        violations.Should().HaveCount(1);
        violations[0].Description.Should().Contain("_counter");
        violations[0].RuleId.Should().Be("ARCH-CS-006");
    }

    [Fact]
    public void StaticMutableFieldRule_Should_Not_Flag_Const_Or_Readonly_Fields()
    {
        var code = @"
public class Config
{
    private const int MaxRetry = 3;
    private static readonly string AppName = ""Architect"";
}";
        var tree = CSharpSyntaxTree.ParseText(code);
        var rule = new StaticMutableFieldRule();

        var violations = rule.Analyze(tree, semanticModel: null).ToList();

        violations.Should().BeEmpty();
    }
}
