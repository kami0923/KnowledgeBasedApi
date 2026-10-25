using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace KnowledgeBaseAPI.Infrastructure.Settings;

public class GeminiSettings
{
    public const string SectionName = "Gemini";

    public string ApiKey { get; set; } = string.Empty;
    public string BaseUrl { get; set; } = "https://generativelanguage.googleapis.com/v1beta/";
    public string Model { get; set; } = "gemini-2.5-flash";
    public double Temperature { get; set; } = 0.2;
}
