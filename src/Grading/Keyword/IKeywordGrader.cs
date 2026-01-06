namespace Eval.Grading.Keyword;

public interface IKeywordGrader
{
    Task<(KeywordAnalysis analysis, double finalScore)> GradeAsync(string modelOutputContent);
}
