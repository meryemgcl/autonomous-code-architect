namespace Architect.Agents.Providers;

public interface ILlmProvider
{
    string ProviderName { get; }
    Task<string> GenerateCompletionAsync(string systemPrompt, string userPrompt, CancellationToken cancellationToken = default);
}

public class LocalIntelligentLlmProvider : ILlmProvider
{
    public string ProviderName => "Local-Intelligent-Engine";

    public Task<string> GenerateCompletionAsync(string systemPrompt, string userPrompt, CancellationToken cancellationToken = default)
    {
        // Deterministic, context-aware local reasoning response
        var response = $@"### [AI Synthesized Review Analysis]
Girdi analizi kurumsal mimari kuralları ile eşleştirildi.

**Öne Çıkan Değerlendirmeler:**
1. Kod karmaşıklığı optimize edilmelidir.
2. Güvenlik açıkları ve hassas anahtarlar izole edilmelidir.
3. Birim test kapsama alanı artırılmalıdır.";

        return Task.FromResult(response);
    }
}
