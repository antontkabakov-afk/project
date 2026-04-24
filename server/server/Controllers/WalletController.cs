using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using server.Extensions;
using server.Service;
using System.ComponentModel.DataAnnotations;

namespace server.Controllers;

public record ConnectWalletRequest(
    [Required, MaxLength(100)] string WalletAddress,
    string? Chain);

[ApiController]
[Authorize]
[Route("api/wallet")]
public class WalletController : ControllerBase
{
    private readonly IWalletService _walletService;
    private readonly IPortfolioSnapshotService _portfolioSnapshotService;

    public WalletController(
        IWalletService walletService,
        IPortfolioSnapshotService portfolioSnapshotService)
    {
        _walletService = walletService;
        _portfolioSnapshotService = portfolioSnapshotService;
    }

    [HttpGet]
    public async Task<IActionResult> GetWallet(CancellationToken cancellationToken)
    {
        var userId = User.GetUserId();

        if (!userId.HasValue)
        {
            return Unauthorized();
        }

        var walletConnection = await _walletService.GetWalletConnectionAsync(
            userId.Value,
            cancellationToken);

        return Ok(walletConnection);
    }

    [HttpPut]
    public async Task<IActionResult> ConnectWallet(
        ConnectWalletRequest request,
        CancellationToken cancellationToken)
    {
        var userId = User.GetUserId();

        if (!userId.HasValue)
        {
            return Unauthorized();
        }

        try
        {
            var walletConnection = await _walletService.ConnectWalletAsync(
                userId.Value,
                request.WalletAddress,
                request.Chain,
                cancellationToken);

            return Ok(walletConnection);
        }
        catch (ArgumentException ex)
        {
            return BadRequest(ex.Message);
        }
        catch (ExternalServiceException ex)
        {
            return ToErrorResponse(ex);
        }
    }

    [HttpPost("snapshot")]
    public async Task<IActionResult> CreateSnapshot(CancellationToken cancellationToken)
    {
        var userId = User.GetUserId();

        if (!userId.HasValue)
        {
            return Unauthorized();
        }

        try
        {
            var walletConnection = await _walletService.GetWalletConnectionAsync(
                userId.Value,
                cancellationToken);

            if (!walletConnection.IsConnected || string.IsNullOrWhiteSpace(walletConnection.WalletAddress))
            {
                return BadRequest("Connect a wallet before creating snapshots.");
            }

            var snapshot = await _portfolioSnapshotService.CreateSnapshotAsync(
                userId.Value,
                walletConnection.WalletAddress,
                walletConnection.Chain,
                force: true,
                cancellationToken);

            return Ok(snapshot);
        }
        catch (ExternalServiceException ex)
        {
            return ToErrorResponse(ex);
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
