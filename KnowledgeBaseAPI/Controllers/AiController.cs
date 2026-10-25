using KnowledgeBaseAPI.Application.DTOs;
using KnowledgeBaseAPI.Application.Exceptions;
using KnowledgeBaseAPI.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace KnowledgeBaseAPI.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class AiController : ControllerBase
{
    private readonly IAiService _aiService;

    public AiController(IAiService aiService)
    {
        _aiService = aiService;
    }

    [HttpPost("ask")]
    public async Task<ActionResult<AiResponseDto>> Ask(
        AskAiRequestDto request,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.Prompt))
            return BadRequest(new { message = "Prompt is required." });

        if (request.Prompt.Length > 4000)
            return BadRequest(new { message = "Prompt is too long (max 4000 characters)." });

        try
        {
            var result = await _aiService.GenerateAsync(
                request.Prompt, request.SystemInstruction, cancellationToken);
            return Ok(result);
        }
        catch (AiServiceException ex)
        {
            var status = ex.StatusCode == 429
                ? StatusCodes.Status429TooManyRequests
                : StatusCodes.Status502BadGateway;

            return StatusCode(status, new { message = ex.Message });
        }
    }
}