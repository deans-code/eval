namespace Eval.Grading.Keyword;

public interface IKeywordGrader
{
    Task<(KeywordGraderResult analysis, double finalScore)> GradeAsync(string modelOutputContent);
}
