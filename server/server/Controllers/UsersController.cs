using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using server.DTO;
using server.Extensions;
using server.Service;
using server.Service.User;
using server.Service.Wallet;

namespace server.Controllers;

[ApiController]
[Authorize]
[Route("api/users")]
public class UsersController : ControllerBase
{
    private readonly IUserService _userService;
    private readonly IWalletService _walletService;

    public UsersController(
        IUserService userService,
        IWalletService walletService)
    {
        _userService = userService;
        _walletService = walletService;
    }

    [HttpGet("{id:int}")]
    public async Task<IActionResult> GetUser(int id, CancellationToken cancellationToken)
    {
        var authenticatedUserId = User.GetUserId();

        if (!authenticatedUserId.HasValue)
        {
            return Unauthorized();
        }

        try
        {
            var user = await _userService.GetByIdAsync(
                authenticatedUserId.Value,
                id,
                cancellationToken);

            return Ok(user);
        }
        catch (KeyNotFoundException)
        {
            return NotFound();
        }
        catch (UnauthorizedAccessException)
        {
            return Forbid();
        }
    }

    [HttpPost("{userId:int}/wallets")]
    public async Task<IActionResult> CreateWallet(
        int userId,
        CreateWalletRequest request,
        CancellationToken cancellationToken)
    {
        var authenticatedUserId = User.GetUserId();

        if (!authenticatedUserId.HasValue)
        {
            return Unauthorized();
        }

        try
        {
            var wallet = await _walletService.CreateAsync(
                authenticatedUserId.Value,
                userId,
                request.Name,
                request.Address,
                request.Chain,
                cancellationToken);

            return CreatedAtAction(nameof(GetWallets), new { userId }, wallet);
        }
        catch (ExternalServiceException ex)
        {
            return ToErrorResponse(ex);
        }
        catch (ArgumentException ex)
        {
            return BadRequest(ex.Message);
        }
        catch (KeyNotFoundException)
        {
            return NotFound();
        }
        catch (UnauthorizedAccessException)
        {
            return Forbid();
        }
    }

    [HttpGet("{userId:int}/wallets")]
    public async Task<IActionResult> GetWallets(int userId, CancellationToken cancellationToken)
    {
        var authenticatedUserId = User.GetUserId();

        if (!authenticatedUserId.HasValue)
        {
            return Unauthorized();
        }

        try
        {
            var wallets = await _walletService.GetByUserAsync(
                authenticatedUserId.Value,
                userId,
                cancellationToken);

            return Ok(wallets);
        }
        catch (KeyNotFoundException)
        {
            return NotFound();
        }
        catch (UnauthorizedAccessException)
        {
            return Forbid();
        }
    }

    private IActionResult ToErrorResponse(ExternalServiceException exception)
    {
        return exception.StatusCode switch
        {
            400 => BadRequest(exception.Message),
            429 => StatusCode(StatusCodes.Status429TooManyRequests, exception.Message),
            _ => StatusCode(StatusCodes.Status503ServiceUnavailable, exception.Message)
        };
    }
}
