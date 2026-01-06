namespace Eval.Config.ConfigModels;

public sealed class GradingKeywordSettings
{
    public List<ExpectedKeyword> ExpectedKeywords { get; set; } = new();
}

public sealed class ExpectedKeyword
{
    public string Keyword { get; set; } = string.Empty;
    public int MinimumOccurrences { get; set; }
}
