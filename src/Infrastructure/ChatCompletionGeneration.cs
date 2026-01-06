using System.Text;
using System.Text.Json;
using Eval.Domain;
using Eval.Infrastructure.Exceptions;

namespace Eval.Infrastructure;

public sealed class ChatCompletionGeneration : IChatCompletionGeneration
{
    private readonly HttpClient _httpClient;
    private readonly string _baseUrl;
    private bool _disposed = false;

    public ChatCompletionGeneration(
        string protocol,
        string host,
        int port)
    {
        _httpClient = new HttpClient();
        _httpClient.Timeout = TimeSpan.FromSeconds(1000);
        _baseUrl = $"{protocol}://{host}:{port}";
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
            _httpClient?.Dispose();
        }
        
        _disposed = true;        
    }

    ~ChatCompletionGeneration()
    {
        Dispose(false);
    }

    async Task<AugmentedChatCompletion> IChatCompletionGeneration.GetChatCompletionAsync(
        string inputText, 
        string? systemPrompt, 
        string model)
    {
        try
        {
            var messagesList = new List<object>();

            if (!string.IsNullOrWhiteSpace(systemPrompt))
            {
                messagesList.Add(new { role = "system", content = systemPrompt });
            }

            messagesList.Add(new { role = "user", content = inputText });

            var requestBody = new Dictionary<string, object?>
            {
                ["messages"] = messagesList.ToArray(),
            };

            if (!string.IsNullOrEmpty(model)) requestBody["model"] = model;

            string json = JsonSerializer.Serialize(requestBody);

            StringContent content = new StringContent(json, Encoding.UTF8, "application/json");

            HttpResponseMessage response = await _httpClient.PostAsync($"{_baseUrl}/v1/chat/completions", content);

            if (!response.IsSuccessStatusCode)
            {
                throw new ChatCompletionGenerationFailedException((int)response.StatusCode);
            }

            string responseJson = await response.Content.ReadAsStringAsync();

            ChatCompletionResponse? chatCompletion = JsonSerializer.Deserialize<ChatCompletionResponse>(responseJson);

            if (chatCompletion == null)
            {
                throw new ChatCompletionDeserializationException();
            }

            string generatedContent = chatCompletion.Choices.FirstOrDefault()?.Message.Content ?? string.Empty;

            generatedContent = ExtractHarmonyMessage(generatedContent);

            return new AugmentedChatCompletion
            {
                InputText = inputText,
                ChatCompletion = chatCompletion,
                ExtractedMessage = generatedContent
            };
        }
        catch (HttpRequestException ex)
        {
            throw new ChatCompletionServiceConnectionException(_baseUrl, ex);
        }
        catch (Exception ex)
        {
            throw new ChatCompletionOperationException(ex.Message, ex);
        }
    }

    private static string ExtractHarmonyMessage(string generatedContent)
    {
        // https://cookbook.openai.com/articles/openai-harmony
        // TODO: Likely needs work to always function correctly.
        // Different responses may have different formats.

        const string harmonyMessageToken = "<|message|>";
        
        int harmonyIndex = generatedContent.IndexOf(harmonyMessageToken);

        if (harmonyIndex >= 0)
        {            
            generatedContent = generatedContent.Substring(harmonyIndex + harmonyMessageToken.Length).Trim();
        }

        return generatedContent;
    }

    private static string ExtractTitleFromMarkdown(string markdown)
    {
        if (string.IsNullOrWhiteSpace(markdown))
        {
            return string.Empty;
        }

        string firstLine = markdown.Split('\n', StringSplitOptions.RemoveEmptyEntries).FirstOrDefault() ?? string.Empty;
        
        // Remove markdown heading syntax (# symbols and leading/trailing whitespace)
        return firstLine.TrimStart('#').Trim();
    }
}
