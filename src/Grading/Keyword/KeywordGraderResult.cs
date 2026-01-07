namespace Eval.Grading.Keyword;

public class KeywordGraderResult
{
    public Dictionary<string, KeywordGraderResultItem> Results { get; set; } = new();
    public int TotalKeywordsMet { get; set; }
    public int TotalKeywords { get; set; }
}

public class KeywordGraderResultItem
{
    public string Keyword { get; set; } = string.Empty;
    public int ExpectedCount { get; set; }
    public int ActualCount { get; set; }
    public bool Met { get; set; }
}
