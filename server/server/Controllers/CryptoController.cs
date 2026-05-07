using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using server.Extensions;
using server.Service;

namespace server.Controllers;

[ApiController]
[Authorize]
[Route("api/crypto")]
public class CryptoController : ControllerBase
{
    private readonly ICryptoPriceSnapshotService _cryptoPriceSnapshotService;

    public CryptoController(ICryptoPriceSnapshotService cryptoPriceSnapshotService)
    {
        _cryptoPriceSnapshotService = cryptoPriceSnapshotService;
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
            var assets = await _cryptoPriceSnapshotService.GetLatestAssetsAsync(cancellationToken);
            return Ok(assets);
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

    [HttpGet("assets/{assetId}/history")]
    public async Task<IActionResult> GetAssetHistory(
        string assetId,
        CancellationToken cancellationToken)
    {
        var userId = User.GetUserId();

        if (!userId.HasValue)
        {
            return Unauthorized();
        }

        try
        {
            var history = await _cryptoPriceSnapshotService.GetAssetHistoryAsync(
                assetId,
                cancellationToken);

            return Ok(history);
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
