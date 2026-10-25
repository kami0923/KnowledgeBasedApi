using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using KnowledgeBaseAPI.Application.DTOs;
using KnowledgeBaseAPI.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
namespace KnowledgeBaseAPI.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class DocumentController : ControllerBase
{
    private readonly IDocumentService _documentService;

    public DocumentController(IDocumentService documentService)
    {
        _documentService = documentService;
    }

    private int GetCurrentUserId()
    {
        var userIdClaim =
            User.FindFirstValue(ClaimTypes.NameIdentifier)
            ?? User.FindFirstValue(JwtRegisteredClaimNames.Sub);

        if (!int.TryParse(userIdClaim, out var userId))
            throw new UnauthorizedAccessException("User ID claim is missing or invalid.");

        return userId;
    }
    [HttpPost("upload")]
    [RequestSizeLimit(10 * 1024 * 1024)]
    [Consumes("multipart/form-data")]
    public async Task<ActionResult<DocumentResponseDto>> Upload(
    IFormFile file,
    [FromForm] string? title)
    {
        if (file == null || file.Length == 0)
            return BadRequest(new { message = "No file was provided." });

        try
        {
            var userId = GetCurrentUserId();

            using var stream = file.OpenReadStream();

            var result = await _documentService.UploadAsync(
                stream,
                file.FileName,
                file.ContentType,
                title,
                userId);

            return Ok(result);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpGet]
    public async Task<ActionResult<List<DocumentResponseDto>>> GetAll()
    {
        var userId = GetCurrentUserId();
        var documents = await _documentService.GetAllForUserAsync(userId);
        return Ok(documents);
    }

    [HttpGet("{id}/download")]
    public async Task<IActionResult> Download(int id)
    {
        var userId = GetCurrentUserId();
        var result = await _documentService.DownloadAsync(id, userId);

        if (result == null)
            return NotFound(new { message = "Document not found." });

        var (stream, fileName, contentType) = result.Value;
        return File(stream, contentType, fileName);
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(int id)
    {
        var userId = GetCurrentUserId();
        var deleted = await _documentService.DeleteAsync(id, userId);

        if (!deleted)
            return NotFound(new { message = "Document not found." });

        return NoContent();
    }
}