using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using server.DTO;
using server.Extensions;
using server.Models;
using server.Service;
using server.Service.Portfolio;

namespace server.Controllers;

[ApiController]
[Authorize]
[Route("api/portfolio")]
public class PortfolioController : ControllerBase
{
    private readonly IPortfolioSnapshotService _portfolioSnapshotService;

    public PortfolioController(IPortfolioSnapshotService portfolioSnapshotService)
    {
        _portfolioSnapshotService = portfolioSnapshotService;
    }

    [HttpGet("assets")]
    public async Task<IActionResult> GetAssets([FromQuery] int? walletId, CancellationToken cancellationToken)
    {
        var userId = User.GetUserId();


        if (!userId.HasValue)
        {
            return Unauthorized();
        }

        if (walletId.HasValue && walletId.Value <= 0)
        {
            return BadRequest("walletId must be greater than zero.");
        }

        try
        {
            var latestSnapshot = await _portfolioSnapshotService.GetLatestSnapshotAsync(
                userId.Value,
                walletId,
                cancellationToken);

            return Ok(latestSnapshot?.Assets ?? Array.Empty<WalletAssetSnapshot>());
        }
        catch (KeyNotFoundException)
        {
            return NotFound();
        }
        catch (UnauthorizedAccessException)
        {
            return Forbid();
        }
        catch (ExternalServiceException ex)
        {
            return ToErrorResponse(ex);
        }
    }

    [HttpGet("history")]
    public async Task<IActionResult> GetHistory(
        [FromQuery] int? days,
        [FromQuery] int? walletId,
        CancellationToken cancellationToken)
    {
        var userId = User.GetUserId();

        if (!userId.HasValue)
        {
            return Unauthorized();
        }

        if (days.HasValue && days.Value <= 0)
        {
            return BadRequest("days must be greater than zero.");
        }

        if (walletId.HasValue && walletId.Value <= 0)
        {
            return BadRequest("walletId must be greater than zero.");
        }

        try
        {
            var snapshots = await _portfolioSnapshotService.GetSnapshotsAsync(
                userId.Value,
                walletId,
                days,
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
        catch (ExternalServiceException ex)
        {
            return ToErrorResponse(ex);
        }
    }

    [HttpGet("statistics")]
    public async Task<IActionResult> GetStatistics(
        [FromQuery] int? days,
        [FromQuery] int? walletId,
        CancellationToken cancellationToken)
    {
        var userId = User.GetUserId();

        if (!userId.HasValue)
        {
            return Unauthorized();
        }

        if (days.HasValue && days.Value <= 0)
        {
            return BadRequest("days must be greater than zero.");
        }

        if (walletId.HasValue && walletId.Value <= 0)
        {
            return BadRequest("walletId must be greater than zero.");
        }

        try
        {
            var statistics = await _portfolioSnapshotService.CalculateHistoricalPerformanceAsync(
                userId.Value,
                walletId,
                days,
                cancellationToken);

            return Ok(statistics);
        }
        catch (KeyNotFoundException)
        {
            return NotFound();
        }
        catch (UnauthorizedAccessException)
        {
            return Forbid();
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
