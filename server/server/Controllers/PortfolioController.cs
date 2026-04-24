using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using server.Extensions;
using server.Models;
using server.Service;

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
    public async Task<IActionResult> GetAssets(CancellationToken cancellationToken)
    {
        var userId = User.GetUserId();

        if (!userId.HasValue)
        {
            return Unauthorized();
        }

        try
        {
            var latestSnapshot = await _portfolioSnapshotService.GetLatestSnapshotAsync(
                userId.Value,
                cancellationToken);

            return Ok(latestSnapshot?.Assets ?? Array.Empty<WalletAssetSnapshot>());
        }
        catch (ExternalServiceException ex)
        {
            return ToErrorResponse(ex);
        }
    }

    [HttpGet("history")]
    public async Task<IActionResult> GetHistory([FromQuery] int? days, CancellationToken cancellationToken)
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

        try
        {
            var snapshots = await _portfolioSnapshotService.GetSnapshotsAsync(
                userId.Value,
                days,
                cancellationToken);

            return Ok(snapshots);
        }
        catch (ExternalServiceException ex)
        {
            return ToErrorResponse(ex);
        }
    }

    [HttpGet("statistics")]
    public async Task<IActionResult> GetStatistics([FromQuery] int? days, CancellationToken cancellationToken)
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

        try
        {
            var statistics = await _portfolioSnapshotService.CalculateHistoricalPerformanceAsync(
                userId.Value,
                days,
                cancellationToken);

            return Ok(statistics);
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
