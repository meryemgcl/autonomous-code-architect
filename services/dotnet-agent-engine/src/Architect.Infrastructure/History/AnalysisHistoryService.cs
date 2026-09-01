namespace Architect.Infrastructure.History;

public record AnalysisHistoryEntry(
    string RequestId,
    string FilePath,
    string Language,
    DateTimeOffset AnalyzedAt,
    int ViolationCount,
    long ExecutionTimeMs,
    bool WasHealed,
    bool WasDebated,
    bool ConsensusApproved
);

public record HistoryStats(
    int TotalAnalyses,
    int TotalDebates,
    int TotalHealings,
    int TotalViolations,
    double AverageExecutionMs,
    string MostFrequentViolation,
    IReadOnlyList<AnalysisHistoryEntry> RecentEntries
);

public interface IAnalysisHistoryService
{
    void Record(AnalysisHistoryEntry entry);
    IReadOnlyList<AnalysisHistoryEntry> GetRecent(int count = 20);
    HistoryStats GetStats();
    void Clear();
}

public class AnalysisHistoryService : IAnalysisHistoryService
{
    private readonly LinkedList<AnalysisHistoryEntry> _buffer = new();
    private readonly Lock _lock = new();
    private const int MaxEntries = 50;

    // Violation frequency counter
    private readonly Dictionary<string, int> _violationFrequency = new();

    public void Record(AnalysisHistoryEntry entry)
    {
        lock (_lock)
        {
            _buffer.AddFirst(entry);
            if (_buffer.Count > MaxEntries)
                _buffer.RemoveLast();
        }
    }

    public IReadOnlyList<AnalysisHistoryEntry> GetRecent(int count = 20)
    {
        lock (_lock)
            return _buffer.Take(count).ToList();
    }

    public HistoryStats GetStats()
    {
        lock (_lock)
        {
            var all = _buffer.ToList();
            if (all.Count == 0)
                return new HistoryStats(0, 0, 0, 0, 0.0, "—", Array.Empty<AnalysisHistoryEntry>());

            var totalViolations  = all.Sum(e => e.ViolationCount);
            var totalDebates     = all.Count(e => e.WasDebated);
            var totalHealings    = all.Count(e => e.WasHealed);
            var avgMs            = all.Average(e => e.ExecutionTimeMs);
            var mostFrequent     = _violationFrequency.OrderByDescending(kv => kv.Value).FirstOrDefault().Key ?? "—";

            return new HistoryStats(
                TotalAnalyses: all.Count,
                TotalDebates: totalDebates,
                TotalHealings: totalHealings,
                TotalViolations: totalViolations,
                AverageExecutionMs: Math.Round(avgMs, 1),
                MostFrequentViolation: mostFrequent,
                RecentEntries: all.Take(20).ToList()
            );
        }
    }

    public void Clear()
    {
        lock (_lock)
        {
            _buffer.Clear();
            _violationFrequency.Clear();
        }
    }
}
