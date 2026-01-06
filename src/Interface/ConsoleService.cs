using Eval.Business;
using Eval.Config.ConfigModels;
using Eval.Interface.Exceptions;
using Microsoft.Extensions.Hosting;

namespace Eval.Interface;

public sealed class ConsoleService : BackgroundService
{
    private readonly IModelOutput _modelOutput;
    private readonly IOutputGrading _outputGrading;
    private readonly string _inputPromptsPath;
    private readonly string _modelOutputPath;
    private readonly string _systemPrompt;
    private readonly string _exampleOutputsPath;
    private bool _disposed = false;    

    public ConsoleService(
        IModelOutput modelOutput,
        IOutputGrading outputGrading,
        string inputPromptsPath,
        string modelOutputPath,
        string systemPrompt,
        string exampleOutputsPath)
    {
        _modelOutput = modelOutput;
        _outputGrading = outputGrading;
        _inputPromptsPath = inputPromptsPath;
        _modelOutputPath = modelOutputPath;
        _systemPrompt = systemPrompt;
        _exampleOutputsPath = exampleOutputsPath;
    }
    
    public override void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
        base.Dispose();
    }
    
    private void Dispose(bool disposing)
    {
        if (_disposed) return;

        if (disposing)
        {            
            _modelOutput?.Dispose();
            _outputGrading?.Dispose();
        }
        
        _disposed = true;        
    }

    ~ConsoleService()
    {
        Dispose(false);
    }

    protected async override Task ExecuteAsync(CancellationToken stoppingToken)
    {
        bool continueRunning = true;

        while (continueRunning)
        {
            Console.WriteLine("Choose an option:");
            Console.WriteLine("1. Generate output");
            Console.WriteLine("2. Grade a selected model output");
            Console.WriteLine("3. Exit application");
            Console.Write("Your choice: ");

            string? choice = Console.ReadLine();

            switch (choice)
            {
                case "1":
                    await CreateModelOutputFiles();
                    break;
                case "2":
                    await GradeSelectedModelOutput();
                    break;
                case "3":
                    continueRunning = false;
                    Console.WriteLine("Closing application.");
                    break;
                default:
                    Console.WriteLine("Invalid choice.");
                    break;
            }

            Console.WriteLine();
        }
    }

    private async Task CreateModelOutputFiles()
    {
        try
        {
            await _modelOutput.GenerateOutputsAsync(                                
                _inputPromptsPath,
                _modelOutputPath);
        }
        catch (ModelOutputFilesCreationException)
        {
            throw;
        }
        catch (Exception ex)
        {
            throw new ModelOutputFilesCreationException(ex.Message, ex);
        }
    }

    private async Task GradeSelectedModelOutput()
    {
        try
        {
            string inputPromptsPath = Path.Combine(
                Directory.GetCurrentDirectory(),
                _inputPromptsPath);

            string modelOutputPath = Path.Combine(
                Directory.GetCurrentDirectory(), 
                _modelOutputPath);

            if (!Directory.Exists(modelOutputPath))
            {
                Console.WriteLine($"Model output directory not found: {modelOutputPath}");
                return;
            }

            string[] modelOutputFiles = Directory.GetFiles(modelOutputPath, "*.*");

            if (modelOutputFiles.Length == 0)
            {
                Console.WriteLine("No model output files found to grade.");
                return;
            }

            Console.WriteLine("\nAvailable model outputs:");
            for (int i = 0; i < modelOutputFiles.Length; i++)
            {
                Console.WriteLine($"{i + 1}. {Path.GetFileName(modelOutputFiles[i])}");
            }

            Console.Write("\nSelect a model output to grade (enter number): ");
            string? selection = Console.ReadLine();

            if (int.TryParse(selection, out int selectedIndex) && selectedIndex > 0 && selectedIndex <= modelOutputFiles.Length)
            {
                string selectedFile = modelOutputFiles[selectedIndex - 1];
                string selectedFileNameWithoutExtension = Path.GetFileNameWithoutExtension(selectedFile);
                Console.WriteLine($"\nGrading: {Path.GetFileName(selectedFile)}");
                
                string? matchingInputPromptFile = null;
                if (Directory.Exists(inputPromptsPath))
                {
                    string[] inputPromptFiles = Directory.GetFiles(inputPromptsPath, "*.*");

                    matchingInputPromptFile = inputPromptFiles
                        .FirstOrDefault(f => 
                            Path
                                .GetFileNameWithoutExtension(f)
                                .Equals(selectedFileNameWithoutExtension, StringComparison.OrdinalIgnoreCase));
                }

                if (matchingInputPromptFile == null)
                {
                    Console.WriteLine($"Warning: No matching input prompt file found for {selectedFileNameWithoutExtension}");
                    return;
                }

                Console.WriteLine($"Using input prompt: {Path.GetFileName(matchingInputPromptFile)}");

                string modelOutputSystemPrompt = Path.Combine(
                    Directory.GetCurrentDirectory(),
                    _systemPrompt);

                string sampleOutputsDirectory = Path.Combine(
                    Directory.GetCurrentDirectory(),
                    _exampleOutputsPath);
                    
                var sampleOutputPaths = Directory.GetFiles(sampleOutputsDirectory, "*.md");

                await _outputGrading.PerformGradingAsync(
                    selectedFile, 
                    matchingInputPromptFile,
                    modelOutputSystemPrompt,
                    sampleOutputPaths);
            }
            else
            {
                Console.WriteLine("Invalid selection.");
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error grading model output: {ex.Message}");
        }
    }
}