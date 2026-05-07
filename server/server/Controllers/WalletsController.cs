using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using server.DTO;
using server.Extensions;
using server.Service;
using server.Service.Wallet;

namespace server.Controllers;

[ApiController]
[Authorize]
[Route("api/wallets")]
public class WalletsController : ControllerBase
{
    private readonly IWalletService _walletService;
    private readonly IWalletSnapshotService _walletSnapshotService;

    public WalletsController(
        IWalletService walletService,
        IWalletSnapshotService walletSnapshotService)
    {
        _walletService = walletService;
        _walletSnapshotService = walletSnapshotService;
    }

    [HttpDelete("{walletId:int}")]
    public async Task<IActionResult> DeleteWallet(int walletId, CancellationToken cancellationToken)
    {
        var authenticatedUserId = User.GetUserId();

        if (_walletService.IsDummyAccount(authenticatedUserId))
        {
            return Unauthorized();
        }

        if (!authenticatedUserId.HasValue)
        {
            return Unauthorized();
        }

        try
        {
            await _walletService.DeleteAsync(
                authenticatedUserId.Value,
                walletId,
                cancellationToken);

            return NoContent();
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

    [HttpPost("{walletId:int}/snapshots")]
    public async Task<IActionResult> CreateSnapshot(
        int walletId,
        CreateWalletSnapshotRequest request,
        CancellationToken cancellationToken)
    {
        var authenticatedUserId = User.GetUserId();

        if (_walletService.IsDummyAccount(authenticatedUserId))
        {
            return Unauthorized();
        }

        if (!authenticatedUserId.HasValue)
        {
            return Unauthorized();
        }

        try
        {
            var snapshot = await _walletSnapshotService.AddAsync(
                authenticatedUserId.Value,
                walletId,
                request.Notes,
                cancellationToken);

            return Ok(snapshot);
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

    [HttpGet("{walletId:int}/snapshots")]
    public async Task<IActionResult> GetSnapshots(int walletId, CancellationToken cancellationToken)
    {
        var authenticatedUserId = User.GetUserId();

        if (_walletService.IsDummyAccount(authenticatedUserId))
        {
            return Unauthorized();
        }

        if (!authenticatedUserId.HasValue)
        {
            return Unauthorized();
        }

        try
        {
            var snapshots = await _walletSnapshotService.GetByWalletAsync(
                authenticatedUserId.Value,
                walletId,
                cancellationToken);

            return Ok(snapshots);
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
