using System.Text.RegularExpressions;
using Eval.Domain;
using Eval.Infrastructure;
using Eval.Logging;

namespace Eval.Business;

public sealed class ModelOutput : IModelOutput
{    
    private readonly IEvalFileAccess _evalFileAccess;
    private readonly IChatCompletionGeneration _chatCompletionGeneration;
    private readonly string _systemPromptPath;
    private readonly string _model;
    private readonly IEvalLogger _logger;
    private bool _disposed = false;

    public ModelOutput(
        IEvalFileAccess evalFileAccess,
        IChatCompletionGeneration chatCompletionGeneration,
        string systemPromptPath,
        string model,
        IEvalLogger logger)    
    {
        _evalFileAccess = evalFileAccess;
        _chatCompletionGeneration = chatCompletionGeneration;
        _systemPromptPath = systemPromptPath;
        _model = model;
        _logger = logger;
    }

    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }
    
    private void Dispose(bool disposing)
    {
        if (_disposed) return;

        if (disposing)
        {            
            _chatCompletionGeneration?.Dispose();            
        }
        
        _disposed = true;        
    }

    ~ModelOutput()
    {
        Dispose(false);
    }

    async Task IModelOutput.GenerateOutputsAsync(
        string inputPromptsPath, 
        string modelOutputDirectoryPath)
    {
        string systemPrompt = await _evalFileAccess.LoadTextFileAsync(_systemPromptPath);

        _logger.LogProcess($"Loading input prompts from: {inputPromptsPath}");

        Dictionary<string, string> inputPrompts = await _evalFileAccess.LoadAllTextFilesAsync(inputPromptsPath);

        _logger.LogProcess($"Found {inputPrompts.Count} input file(s)");

        var validPrompts = inputPrompts.Where(kvp => !string.IsNullOrWhiteSpace(kvp.Value)).ToList();

        int count = 0;
        foreach (var kvp in validPrompts)
        {
            count++;
            string inputFilePath = kvp.Key;
            string userMessage = kvp.Value;
            string title = Path.GetFileNameWithoutExtension(inputFilePath);
            _logger.LogProcess($"Processing {count}/{validPrompts.Count}: Generating output for {title}...");

            AugmentedChatCompletion completion = await _chatCompletionGeneration.GetChatCompletionAsync(
                userMessage, 
                systemPrompt, 
                _model);

            string generatedContent = completion.ChatCompletion?.Choices.FirstOrDefault()?.Message.Content ?? string.Empty;

            if (!string.IsNullOrWhiteSpace(generatedContent))
            {                
                _logger.LogProcess($"Saving to: {title}.md");

                await _evalFileAccess.SaveMarkdownFileAsync(
                    modelOutputDirectoryPath,
                    generatedContent,
                    title);

                _logger.LogProcess($"Created: {title}.md");
            }
        }
        _logger.LogProcess($"Completed processing {count} file(s)");
    }
}