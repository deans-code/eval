namespace Eval.Grading.Keyword;

public class KeywordAnalysis
{
    public Dictionary<string, KeywordResult> Results { get; set; } = new();
    public int TotalKeywordsMet { get; set; }
    public int TotalKeywords { get; set; }
}

public class KeywordResult
{
    public string Keyword { get; set; } = string.Empty;
    public int ExpectedCount { get; set; }
    public int ActualCount { get; set; }
    public bool Met { get; set; }
}
