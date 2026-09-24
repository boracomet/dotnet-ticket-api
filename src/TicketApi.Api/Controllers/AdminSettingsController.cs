using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TicketApi.Application.DTOs;
using TicketApi.Application.Services;
using TicketApi.Domain.Entities;

namespace TicketApi.Api.Controllers;

[ApiController]
[Authorize(Roles = Roles.Admin)]
[Route("api/v1/admin/smtp")]
public class AdminSettingsController : ControllerBase
{
    private readonly AdminSettingsService _settings;

    public AdminSettingsController(AdminSettingsService settings) => _settings = settings;

    [HttpGet]
    public async Task<ActionResult<SmtpSettingsResponse>> Get(CancellationToken ct) =>
        Ok(await _settings.GetAsync(ct));

    [HttpPut]
    public async Task<ActionResult<SmtpSettingsResponse>> Update(
        [FromBody] UpdateSmtpSettingsRequest request, CancellationToken ct) =>
        Ok(await _settings.UpdateAsync(request, ct));

    [HttpPost("test")]
    public async Task<IActionResult> Test([FromBody] SmtpTestRequest request, CancellationToken ct)
    {
        await _settings.SendTestAsync(request.ToEmail, ct);
        return NoContent();
    }
}
