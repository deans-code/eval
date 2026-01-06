namespace Eval.Grading.Sentiment;

public interface ISentimentGrader
{
    Task<(SentimentScores scores, double finalScore)> GradeAsync(
        string modelOutputContent);    
}
