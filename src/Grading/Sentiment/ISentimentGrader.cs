namespace Eval.Grading.Sentiment;

public interface ISentimentGrader
{
    Task<(SentimentGraderResult scores, double finalScore)> GradeAsync(
        string modelOutputContent);    
}
