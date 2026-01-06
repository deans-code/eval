namespace Eval.Grading.Markdown;

public class MarkdownValidation
{
    public bool IsValid { get; set; }
    public bool HasHeadings { get; set; }
    public bool HasCodeBlocks { get; set; }
    public bool HasLinks { get; set; }
    public bool HasLists { get; set; }
    public int HeadingCount { get; set; }
    public int CodeBlockCount { get; set; }
    public int LinkCount { get; set; }
    public int ListCount { get; set; }
    public List<string> ValidationErrors { get; set; } = new();
}
