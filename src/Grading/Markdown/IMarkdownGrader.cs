namespace Eval.Grading.Markdown;

public interface IMarkdownGrader
{
    Task<(MarkdownValidation validation, double finalScore)> GradeAsync(string modelOutputContent);
}
