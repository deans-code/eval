namespace Eval.Grading.Markdown;

public interface IMarkdownGrader
{
    Task<(MarkdownGraderResult validation, double finalScore)> GradeAsync(string modelOutputContent);
}
