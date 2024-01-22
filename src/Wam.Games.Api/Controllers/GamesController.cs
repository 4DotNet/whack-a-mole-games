using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Wam.Games.Services;

namespace Wam.Games.Api.Controllers;

[Route("api/[controller]")]
[ApiController]
public class GamesController(IGamesService gamesService) : ControllerBase
{

    [AllowAnonymous]
    [HttpGet("active")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult> Get(CancellationToken cancellationToken)
    {
        var nextGame = await gamesService.GetActive(cancellationToken);
        if (nextGame == null)
        {
            return NotFound();
        }

        return Ok(nextGame);
    }

    [AllowAnonymous]
    [HttpGet("next")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult> GetNext(CancellationToken cancellationToken)
    {
        var nextGame = await gamesService.GetUpcoming(cancellationToken);
        if (nextGame == null)
        {
            return NotFound();
        }

        return Ok(nextGame);
    }

    [HttpPost]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<ActionResult> Create(CancellationToken cancellationToken)
    {
        var nextGame = await gamesService.Create(cancellationToken);
        return Ok(nextGame);
    }

    [HttpGet("{code}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<ActionResult> Get(string code, CancellationToken cancellationToken)
    {
        var nextGame = await gamesService.GetByCode(code,cancellationToken);
        return Ok(nextGame);
    }
    [HttpGet("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<ActionResult> Get(Guid id, CancellationToken cancellationToken)
    {
        var nextGame = await gamesService.Get(id, cancellationToken);
        return Ok(nextGame);
    }

    [HttpGet("{id:guid}/activate")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<ActionResult> Activate(Guid id, CancellationToken cancellationToken)
    {
        var nextGame = await gamesService.Activate(id, cancellationToken);
        return Ok(nextGame);
    }
    [HttpGet("{id:guid}/start")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<ActionResult> Start(Guid id, CancellationToken cancellationToken)
    {
        var nextGame = await gamesService.Start(id, cancellationToken);
        return Ok(nextGame);
    }
    [HttpGet("{id:guid}/finish")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<ActionResult> Finish(Guid id, CancellationToken cancellationToken)
    {
        var nextGame = await gamesService.Finish(id, cancellationToken);
        return Ok(nextGame);
    }
    [HttpGet("{id:guid}/cancel")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<ActionResult> Cancel(Guid id, CancellationToken cancellationToken)
    {
        var nextGame = await gamesService.Cancel(id, cancellationToken);
        return Ok(nextGame);
    }

    [HttpGet("{code}/join")]
    [AllowAnonymous]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<ActionResult> Join(string code, [FromQuery]string?  voucher, CancellationToken cancellationToken)
    {
        var userIdString = HttpContext.Request.Headers["x-user-id"];
        var userId = Guid.Parse(userIdString);
        var response = await gamesService.Join(code, userId, voucher, cancellationToken);
        return Ok(response);
    }

}