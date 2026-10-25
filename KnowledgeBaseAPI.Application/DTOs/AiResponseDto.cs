using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace KnowledgeBaseAPI.Application.DTOs;

public class AiResponseDto
{
    public string Text { get; set; } = string.Empty;
    public string Model { get; set; } = string.Empty;
    public int PromptTokens { get; set; }
    public int ResponseTokens { get; set; }
    public int TotalTokens { get; set; }
}
