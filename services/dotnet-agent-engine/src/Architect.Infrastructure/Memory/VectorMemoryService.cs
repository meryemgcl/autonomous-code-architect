using Architect.Core.Models;

namespace Architect.Infrastructure.Memory;

public interface IVectorMemoryService
{
    Task<IReadOnlyList<SemanticSearchResult>> FindRelevantRulesAsync(string query, int topK = 3, CancellationToken cancellationToken = default);
    Task AddRuleAsync(ArchitectureRule rule, CancellationToken cancellationToken = default);
}

public class VectorMemoryService : IVectorMemoryService
{
    private readonly List<ArchitectureRule> _rulesDatabase;

    public VectorMemoryService()
    {
        // Kurumsal Bilgi Bankası (Enterprise Architectural Knowledge Base)
        _rulesDatabase = new List<ArchitectureRule>
        {
            new(
                Id: 1,
                RuleCode: "ENT-ARCH-001",
                RuleName: "Clean Architecture Dependency Inversion",
                Category: "CleanArchitecture",
                Description: "Domain ve Core katmanları daima saf kalmalıdır. EntityFrameworkCore, ASP.NET Core MVC veya SQL kütüphaneleri Domain içerisine referans verilemez.",
                RecommendedFix: "Domain içerisinde Interface tanımlayın, Infrastructure katmanında Repository/Service olarak implemente edin.",
                Embedding: GenerateDeterministicEmbedding("Domain Clean Architecture Dependency Inversion EntityFramework MVC")
            ),
            new(
                Id: 2,
                RuleCode: "ENT-SEC-001",
                RuleName: "Zero Hardcoded Secrets Policy",
                Category: "Security",
                Description: "Kaynak kod içerisinde API Key, parola, connection string veya JWT secret barındırılamaz. OWASP A07:2021 ihlalidir.",
                RecommendedFix: "IConfiguration ile appsettings.json, Azure Key Vault veya ortam değişkenleri (Environment Variables) kullanın.",
                Embedding: GenerateDeterministicEmbedding("Security Secret Password ApiKey Token Vault OWASP")
            ),
            new(
                Id: 3,
                RuleCode: "ENT-ASYNC-001",
                RuleName: "Async Task Best Practice",
                Category: "Performance",
                Description: "'async void' metodlar yakalanamayan istisnalar fırlatarak process çökmesine (Crash) neden olur. Sadece UI event handler'larda istisnadır.",
                RecommendedFix: "Geri dönüş tipini 'async Task' veya 'ValueTask' olarak değiştirin.",
                Embedding: GenerateDeterministicEmbedding("Async Void Task Exception Crash Performance Threading")
            ),
            new(
                Id: 4,
                RuleCode: "ENT-TEST-001",
                RuleName: "Automated Unit Testing Standard",
                Category: "Quality",
                Description: "Tüm kritik iş kuralları xUnit (C#) veya JUnit 5 (Java) ile birim testine tabi tutulmalı, sınır değerleri (Edge Cases) doğrulanmalıdır.",
                RecommendedFix: "AAA (Arrange-Act-Assert) kalıbı ve FluentAssertions kullanarak kapsamlı test senaryoları yazın.",
                Embedding: GenerateDeterministicEmbedding("Unit Test xUnit JUnit Mockito Arrange Act Assert Testing")
            ),
            new(
                Id: 5,
                RuleCode: "ENT-CLEAN-001",
                RuleName: "Single Responsibility & Small Methods",
                Category: "CleanCode",
                Description: "Bir metod tek bir işi yapmalıdır. 30 satırı geçen veya karmaşıklığı 8'den yüksek metodlar alt fonksiyonlara bölünmelidir.",
                RecommendedFix: "Extract Method refactoring tekniği uygulayarak sorumlulukları ayrıştırın.",
                Embedding: GenerateDeterministicEmbedding("Clean Code Single Responsibility Method Length Complexity Refactor")
            )
        };
    }

    public Task<IReadOnlyList<SemanticSearchResult>> FindRelevantRulesAsync(string query, int topK = 3, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(query))
        {
            return Task.FromResult<IReadOnlyList<SemanticSearchResult>>(Array.Empty<SemanticSearchResult>());
        }

        var queryEmbedding = GenerateDeterministicEmbedding(query);

        var results = _rulesDatabase
            .Where(r => r.Embedding != null)
            .Select(r => new SemanticSearchResult(
                Rule: r,
                SimilarityScore: CosineSimilarity(queryEmbedding, r.Embedding!)
            ))
            .OrderByDescending(r => r.SimilarityScore)
            .Take(topK)
            .ToList();

        return Task.FromResult<IReadOnlyList<SemanticSearchResult>>(results);
    }

    public Task AddRuleAsync(ArchitectureRule rule, CancellationToken cancellationToken = default)
    {
        var embedding = rule.Embedding ?? GenerateDeterministicEmbedding($"{rule.RuleName} {rule.Description} {rule.Category}");
        _rulesDatabase.Add(rule with { Embedding = embedding });
        return Task.CompletedTask;
    }

    // Deterministik 128 boyutlu TF-IDF / Hash Vektör Üretici (Canlı OpenAI/Gemini embedding yokken çalışır)
    private static float[] GenerateDeterministicEmbedding(string text)
    {
        const int dimensions = 128;
        var vector = new float[dimensions];
        var words = text.ToLowerInvariant().Split(new[] { ' ', '.', ',', ':', ';', '-', '(', ')' }, StringSplitOptions.RemoveEmptyEntries);

        foreach (var word in words)
        {
            var hash = (uint)word.GetHashCode();
            var index = (int)(hash % dimensions);
            vector[index] += 1.0f;
        }

        // L2 Normalization (Birim vektör)
        var norm = MathF.Sqrt(vector.Sum(x => x * x));
        if (norm > 0)
        {
            for (int i = 0; i < dimensions; i++)
            {
                vector[i] /= norm;
            }
        }

        return vector;
    }

    // Cosine Similarity Hesabı: (A . B) / (||A|| * ||B||)
    private static double CosineSimilarity(float[] vectorA, float[] vectorB)
    {
        if (vectorA.Length != vectorB.Length) return 0.0;

        double dotProduct = 0.0;
        double normA = 0.0;
        double normB = 0.0;

        for (int i = 0; i < vectorA.Length; i++)
        {
            dotProduct += vectorA[i] * vectorB[i];
            normA += vectorA[i] * vectorA[i];
            normB += vectorB[i] * vectorB[i];
        }

        if (normA == 0.0 || normB == 0.0) return 0.0;

        return dotProduct / (Math.Sqrt(normA) * Math.Sqrt(normB));
    }
}
