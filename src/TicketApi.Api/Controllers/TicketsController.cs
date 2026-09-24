using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TicketApi.Application.Common;
using TicketApi.Application.DTOs;
using TicketApi.Application.Services;
using TicketApi.Domain.Enums;

namespace TicketApi.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/v1/tickets")]
public class TicketsController : ControllerBase
{
    private readonly TicketService _tickets;

    public TicketsController(TicketService tickets) => _tickets = tickets;

    private Guid UserId
    {
        get
        {
            var raw = User.FindFirstValue(ClaimTypes.NameIdentifier)
                ?? User.FindFirstValue(System.IdentityModel.Tokens.Jwt.JwtRegisteredClaimNames.Sub)
                ?? throw new InvalidOperationException("Missing user id claim");
            return Guid.Parse(raw);
        }
    }

    private string Role => User.FindFirstValue(ClaimTypes.Role) ?? "User";

    [HttpGet]
    public async Task<ActionResult<PagedResult<TicketResponse>>> List(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 10,
        [FromQuery] TicketStatus? status = null,
        [FromQuery] TicketPriority? priority = null,
        [FromQuery] string? search = null,
        CancellationToken ct = default)
    {
        var query = new TicketQuery(page, pageSize, status, priority, search);
        return Ok(await _tickets.ListAsync(query, UserId, Role, ct));
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<TicketResponse>> Get(Guid id, CancellationToken ct) =>
        Ok(await _tickets.GetAsync(id, UserId, Role, ct));

    [HttpPost]
    [RequestSizeLimit(20_000_000)]
    [RequestFormLimits(MultipartBodyLengthLimit = 20_000_000)]
    public async Task<ActionResult<TicketResponse>> Create(
        [FromForm] string title,
        [FromForm] string? description,
        [FromForm] TicketPriority priority,
        [FromForm] List<IFormFile>? files,
        CancellationToken ct)
    {
        var uploads = new List<ReplyFileUpload>();
        foreach (var file in files ?? [])
        {
            await using var buffer = new MemoryStream();
            await file.CopyToAsync(buffer, ct);
            uploads.Add(new ReplyFileUpload(file.FileName, file.ContentType, buffer.ToArray()));
        }

        var created = await _tickets.CreateAsync(
            UserId, Role, new CreateTicketRequest(title, description ?? string.Empty, priority), uploads, ct);
        return CreatedAtAction(nameof(Get), new { id = created.Id }, created);
    }

    [HttpGet("{id:guid}/files/{fileId:guid}")]
    public async Task<IActionResult> DownloadTicketFile(Guid id, Guid fileId, CancellationToken ct)
    {
        var file = await _tickets.OpenTicketAttachmentAsync(id, fileId, UserId, Role, ct);
        return File(file.Content, file.ContentType, file.FileName);
    }

    [HttpPut("{id:guid}")]
    public async Task<ActionResult<TicketResponse>> Update(
        Guid id, [FromBody] UpdateTicketRequest request, CancellationToken ct) =>
        Ok(await _tickets.UpdateAsync(id, UserId, Role, request, ct));

    [HttpGet("{id:guid}/replies")]
    public async Task<ActionResult<IReadOnlyList<TicketReplyResponse>>> ListReplies(Guid id, CancellationToken ct) =>
        Ok(await _tickets.ListRepliesAsync(id, UserId, Role, ct));

    [HttpPost("{id:guid}/replies")]
    [RequestSizeLimit(20_000_000)]
    [RequestFormLimits(MultipartBodyLengthLimit = 20_000_000)]
    public async Task<ActionResult<TicketReplyResponse>> AddReply(
        Guid id, [FromForm] string? body, [FromForm] List<IFormFile>? files, CancellationToken ct)
    {
        var uploads = new List<ReplyFileUpload>();
        foreach (var file in files ?? [])
        {
            await using var buffer = new MemoryStream();
            await file.CopyToAsync(buffer, ct);
            uploads.Add(new ReplyFileUpload(file.FileName, file.ContentType, buffer.ToArray()));
        }

        var created = await _tickets.AddReplyAsync(
            id, UserId, Role, new CreateTicketReplyRequest(body), uploads, ct);
        return CreatedAtAction(nameof(ListReplies), new { id }, created);
    }

    [HttpGet("{id:guid}/replies/{replyId:guid}/files/{fileId:guid}")]
    public async Task<IActionResult> DownloadFile(Guid id, Guid replyId, Guid fileId, CancellationToken ct)
    {
        var file = await _tickets.OpenAttachmentAsync(id, replyId, fileId, UserId, Role, ct);
        return File(file.Content, file.ContentType, file.FileName);
    }

    [HttpPatch("{id:guid}/status")]
    public async Task<ActionResult<TicketResponse>> ChangeStatus(
        Guid id, [FromBody] ChangeStatusRequest request, CancellationToken ct) =>
        Ok(await _tickets.ChangeStatusAsync(id, UserId, Role, request, ct));

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id, CancellationToken ct)
    {
        await _tickets.DeleteAsync(id, UserId, Role, ct);
        return NoContent();
    }
}
