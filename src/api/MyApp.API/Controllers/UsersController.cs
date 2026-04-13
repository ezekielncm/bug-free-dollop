using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MyApp.Application.Features.Users.Queries;
using System.Security.Claims;

namespace MyApp.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class UsersController(IMediator mediator) : ControllerBase
{
    [HttpGet("me")]
    public async Task<IActionResult> GetMe(CancellationToken ct)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)
            ?? User.FindFirstValue("sub")
            ?? throw new UnauthorizedAccessException();

        var result = await mediator.Send(new GetUserByIdQuery(Guid.Parse(userId)), ct);
        return Ok(result);
    }

    [HttpGet("{id:guid}")]
    [Authorize(Roles = "Admin,SuperAdmin")]
    public async Task<IActionResult> GetById(Guid id, CancellationToken ct)
    {
        var result = await mediator.Send(new GetUserByIdQuery(id), ct);
        return Ok(result);
    }
}
