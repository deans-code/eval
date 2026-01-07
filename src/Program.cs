using Eval.Business;
using Eval.Config.ConfigModels;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Eval.Infrastructure;
using Eval.Interface;
using Eval.Grading.Sentiment;
using Eval.Grading.Markdown;
using Eval.Grading.Keyword;
using Eval.Grading.LlmAsAJudge;
using Eval.Logging;

namespace Eval;

public sealed class Program
{
    private static async Task Main(string[] args)
    {
        IHost host = BuildHost(args);

        await host.RunAsync();
    }

    private static IHost BuildHost(string[] args)
    {
        return Host.CreateDefaultBuilder(args)
            .ConfigureAppConfiguration((context, config) =>
            {
                config.AddJsonFile("Config/business.modeloutput.settings.json", optional: false, reloadOnChange: true);
                config.AddJsonFile("Config/business.outputgrading.settings.json", optional: false, reloadOnChange: true);
                config.AddJsonFile("Config/grading.keyword.settings.json", optional: false, reloadOnChange: true);
                config.AddJsonFile("Config/grading.llmasajudge.settings.json", optional: false, reloadOnChange: true);
                config.AddJsonFile("Config/grading.markdown.settings.json", optional: false, reloadOnChange: true);
                config.AddJsonFile("Config/grading.sentiment.settings.json", optional: false, reloadOnChange: true);
                config.AddJsonFile("Config/infrastructure.chatcompletion.settings.json", optional: false, reloadOnChange: true);
            })
            .ConfigureLogging(logging =>
            {
                logging.SetMinimumLevel(LogLevel.Error);
            })
            .ConfigureServices((context, services) =>
            {
                var businessModelOutputSettings = new BusinessModelOutputSettings();
                var businessOutputGradingSettings = new BusinessOutputGradingSettings();
                var gradingKeywordSettings = new GradingKeywordSettings();
                var gradingLlmAsAJudgeSettings = new GradingLlmAsAJudgeSettings();
                var gradingMarkdownSettings = new GradingMarkdownSettings();
                var gradingSentimentSettings = new GradingSentimentSettings();
                var infrastructureChatCompletionSettings = new InfrastructureChatCompletionSettings();

                context.Configuration.GetSection("business.modeloutput.settings").Bind(businessModelOutputSettings);
                context.Configuration.GetSection("business.outputgrading.settings").Bind(businessOutputGradingSettings);
                context.Configuration.GetSection("grading.keyword.settings").Bind(gradingKeywordSettings);
                context.Configuration.GetSection("grading.llmasajudge.settings").Bind(gradingLlmAsAJudgeSettings);
                context.Configuration.GetSection("grading.markdown.settings").Bind(gradingMarkdownSettings);
                context.Configuration.GetSection("grading.sentiment.settings").Bind(gradingSentimentSettings);
                context.Configuration.GetSection("infrastructure.chatcompletion.settings").Bind(infrastructureChatCompletionSettings);
                
                services.AddSingleton(businessModelOutputSettings);
                services.AddSingleton(businessOutputGradingSettings);
                services.AddSingleton(gradingKeywordSettings);
                services.AddSingleton(gradingLlmAsAJudgeSettings);
                services.AddSingleton(gradingMarkdownSettings);
                services.AddSingleton(gradingSentimentSettings);
                services.AddSingleton(infrastructureChatCompletionSettings);

                _ = services.AddSingleton<IChatCompletionGeneration>(provider =>
                {
                    var settings = provider.GetRequiredService<InfrastructureChatCompletionSettings>();

                    return new ChatCompletionGeneration(
                        settings.ChatCompletionApi.Protocol,
                        settings.ChatCompletionApi.Host,
                        settings.ChatCompletionApi.Port);
                });

                services.AddSingleton<IEvalFileAccess, EvalFileAccess>();
                services.AddSingleton<IEvalLogger, EvalLogger>();
                
                services.AddSingleton<ISentimentGrader>(provider =>
                {
                    var evalFileAccess = provider.GetRequiredService<IEvalFileAccess>();
                    var chatCompletionGeneration = provider.GetRequiredService<IChatCompletionGeneration>();
                    var gradingSentimentSettings = provider.GetRequiredService<GradingSentimentSettings>();
                    var logger = provider.GetRequiredService<IEvalLogger>();
                    
                    var targetScores = new SentimentGraderResult
                    {
                        Anger = gradingSentimentSettings.TargetSentimentScores.Anger,
                        Fear = gradingSentimentSettings.TargetSentimentScores.Fear,
                        Anticipation = gradingSentimentSettings.TargetSentimentScores.Anticipation,
                        Trust = gradingSentimentSettings.TargetSentimentScores.Trust,
                        Surprise = gradingSentimentSettings.TargetSentimentScores.Surprise,
                        Sadness = gradingSentimentSettings.TargetSentimentScores.Sadness,
                        Joy = gradingSentimentSettings.TargetSentimentScores.Joy,
                        Disgust = gradingSentimentSettings.TargetSentimentScores.Disgust
                    };
                    
                    return new SentimentGrader(
                        evalFileAccess, 
                        chatCompletionGeneration, 
                        gradingSentimentSettings.SystemPrompt,
                        targetScores,
                        gradingSentimentSettings.Model,
                        logger);
                });
                
                services.AddSingleton<IMarkdownGrader>(provider =>
                {                    
                    var gradingMarkdownSettings = provider.GetRequiredService<GradingMarkdownSettings>();
                    var logger = provider.GetRequiredService<IEvalLogger>();
                    
                    return new MarkdownGrader(                        
                        gradingMarkdownSettings.ValidityWeight,
                        gradingMarkdownSettings.VarietyWeight,
                        logger);
                });
                
                services.AddSingleton<IKeywordGrader>(provider =>
                {
                    var evalFileAccess = provider.GetRequiredService<IEvalFileAccess>();
                    var logger = provider.GetRequiredService<IEvalLogger>();
                    var gradingKeywordSettings = provider.GetRequiredService<GradingKeywordSettings>();
                    
                    return new KeywordGrader(evalFileAccess, logger, gradingKeywordSettings.ExpectedKeywords);
                });
                
                services.AddSingleton<ILlmAsAJudgeGrader>(provider =>
                {
                    var evalFileAccess = provider.GetRequiredService<IEvalFileAccess>();
                    var chatCompletionGeneration = provider.GetRequiredService<IChatCompletionGeneration>();
                    var gradingLlmAsAJudgeSettings = provider.GetRequiredService<GradingLlmAsAJudgeSettings>();
                    var logger = provider.GetRequiredService<IEvalLogger>();
                    
                    return new LlmAsAJudgeGrader(
                        evalFileAccess,
                        chatCompletionGeneration,
                        gradingLlmAsAJudgeSettings.SystemPromptPath,
                        gradingLlmAsAJudgeSettings.Model,
                        logger);
                });
                
                services.AddSingleton<IModelOutput>(provider =>
                {
                    var evalFileAccess = provider.GetRequiredService<IEvalFileAccess>();
                    var chatCompletionGeneration = provider.GetRequiredService<IChatCompletionGeneration>();
                    var businessModelOutputSettings = provider.GetRequiredService<BusinessModelOutputSettings>();
                    var logger = provider.GetRequiredService<IEvalLogger>();
                    
                    return new ModelOutput(
                        evalFileAccess, 
                        chatCompletionGeneration, 
                        businessModelOutputSettings.SystemPrompt,
                        businessModelOutputSettings.LargeLanguageModel,
                        logger);
                });

                services.AddSingleton<IOutputGrading>(provider =>
                {
                    var evalFileAccess = provider.GetRequiredService<IEvalFileAccess>();
                    var chatCompletionGeneration = provider.GetRequiredService<IChatCompletionGeneration>();
                    var sentimentGrader = provider.GetRequiredService<ISentimentGrader>();
                    var markdownGrader = provider.GetRequiredService<IMarkdownGrader>();
                    var keywordGrader = provider.GetRequiredService<IKeywordGrader>();
                    var llmAsAJudgeGrader = provider.GetRequiredService<ILlmAsAJudgeGrader>();
                    var businessOutputGradingSettings = provider.GetRequiredService<BusinessOutputGradingSettings>();
                    var logger = provider.GetRequiredService<IEvalLogger>();
                    
                    return new OutputGrading(
                        evalFileAccess, 
                        chatCompletionGeneration, 
                        sentimentGrader, 
                        markdownGrader,
                        keywordGrader,
                        llmAsAJudgeGrader,
                        businessOutputGradingSettings.SentimentWeight,
                        businessOutputGradingSettings.MarkdownWeight,
                        businessOutputGradingSettings.KeywordWeight,
                        businessOutputGradingSettings.LlmAsAJudgeScoreWeight,
                        logger);
                });

                services.AddHostedService<ConsoleService>(provider =>
                {
                    var modelOutput = provider.GetRequiredService<IModelOutput>();
                    var outputGrading = provider.GetRequiredService<IOutputGrading>();
                    var businessModelOutputSettings = provider.GetRequiredService<BusinessModelOutputSettings>();
                    var gradingLlmAsAJudgeSettings = provider.GetRequiredService<GradingLlmAsAJudgeSettings>();
                    
                    return new ConsoleService(
                        modelOutput,
                        outputGrading,
                        businessModelOutputSettings.InputPromptsPath,
                        businessModelOutputSettings.ModelOutputPath,
                        businessModelOutputSettings.SystemPrompt,
                        gradingLlmAsAJudgeSettings.ExampleOutputsPath);
                });
            })
            .Build();
    }
}