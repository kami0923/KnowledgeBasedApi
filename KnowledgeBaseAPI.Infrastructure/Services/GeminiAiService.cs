using KnowledgeBaseAPI.Application.DTOs;
using KnowledgeBaseAPI.Application.Exceptions;
using KnowledgeBaseAPI.Application.Interfaces;
using KnowledgeBaseAPI.Infrastructure.Settings;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace KnowledgeBaseAPI.Infrastructure.Services;

public class GeminiAiService: IAiService
{
    private readonly HttpClient _httpClient;
    private readonly GeminiSettings _settings;
    private readonly ILogger<GeminiAiService> _logger;

    public GeminiAiService(
        HttpClient httpClient,
        IOptions<GeminiSettings> options,
        ILogger<GeminiAiService> logger)
    {
        _httpClient = httpClient;
        _settings = options.Value;
        _logger = logger;
    }

    public async Task<AiResponseDto> GenerateAsync(
        string prompt,
        string? systemInstruction = null,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(_settings.ApiKey))
            throw new AiServiceException(
                "The Gemini API key is not configured. Add it to User Secrets as Gemini:ApiKey.");

        // Build the JSON body Gemini expects
        var requestBody = new Dictionary<string, object>
        {
            ["contents"] = new[]
            {
                new { role = "user", parts = new[] { new { text = prompt } } }
            },
            ["generationConfig"] = new { temperature = _settings.Temperature }
        };

        if (!string.IsNullOrWhiteSpace(systemInstruction))
        {
            requestBody["systemInstruction"] = new
            {
                parts = new[] { new { text = systemInstruction } }
            };
        }

        var url = $"models/{_settings.Model}:generateContent";

        string body;
        HttpStatusCode statusCode;
        try
        {
            using var response = await _httpClient.PostAsJsonAsync(url, requestBody, cancellationToken);
            statusCode = response.StatusCode;
            body = await response.Content.ReadAsStringAsync(cancellationToken);
        }
        catch (TaskCanceledException) when (!cancellationToken.IsCancellationRequested)
        {
            // HttpClient timeout (not the caller cancelling)
            throw new AiServiceException("The AI service took too long to respond. Please try again.");
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "Could not reach the Gemini API.");
            throw new AiServiceException("Could not reach the AI service.");
        }

        if ((int)statusCode is < 200 or >= 300)
        {
            // Full detail goes to OUR logs, never to the client
            _logger.LogWarning("Gemini returned {StatusCode}: {Body}", (int)statusCode, body);

            var message = statusCode == HttpStatusCode.TooManyRequests
                ? "AI rate limit reached. Please wait a moment and try again."
                : "The AI service returned an error.";

            throw new AiServiceException(message, (int)statusCode);
        }

        return ParseResponse(body);
    }

    private AiResponseDto ParseResponse(string body)
    {
        using var doc = JsonDocument.Parse(body);
        var root = doc.RootElement;

        if (!root.TryGetProperty("candidates", out var candidates) || candidates.GetArrayLength() == 0)
            throw new AiServiceException(
                "The AI returned no answer. The request may have been blocked by safety filters.");

        var text = new StringBuilder();
        if (candidates[0].TryGetProperty("content", out var content) &&
            content.TryGetProperty("parts", out var parts))
        {
            foreach (var part in parts.EnumerateArray())
            {
                if (part.TryGetProperty("text", out var t))
                    text.Append(t.GetString());
            }
        }

        int promptTokens = 0, responseTokens = 0, totalTokens = 0;
        if (root.TryGetProperty("usageMetadata", out var usage))
        {
            if (usage.TryGetProperty("promptTokenCount", out var p)) promptTokens = p.GetInt32();
            if (usage.TryGetProperty("candidatesTokenCount", out var c)) responseTokens = c.GetInt32();
            if (usage.TryGetProperty("totalTokenCount", out var tt)) totalTokens = tt.GetInt32();
        }

        return new AiResponseDto
        {
            Text = text.ToString(),
            Model = _settings.Model,
            PromptTokens = promptTokens,
            ResponseTokens = responseTokens,
            TotalTokens = totalTokens
        };
    }

}
