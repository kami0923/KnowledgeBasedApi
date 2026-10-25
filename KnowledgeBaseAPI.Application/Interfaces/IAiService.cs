using KnowledgeBaseAPI.Application.DTOs;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace KnowledgeBaseAPI.Application.Interfaces;

public interface IAiService
{
    Task<AiResponseDto> GenerateAsync(
        string prompt,
        string? systemInstruction = null,
        CancellationToken cancellationToken = default);

}
