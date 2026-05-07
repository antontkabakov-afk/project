using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using server.Date;
using server.Models;
using server.Service;
using System.ComponentModel.DataAnnotations;
using System.Net;
using server.Service.Setting;

namespace server.Controllers;

public record RegisterRequest(
    [Required, EmailAddress, MaxLength(255)] string Email,
    [Required, MinLength(8), MaxLength(128)] string Password,
    [MaxLength(50)] string? Username);

public record LoginRequest(
    [Required, EmailAddress, MaxLength(255)] string Email,
    [Required, MaxLength(128)] string Password);

[ApiController]
[Route("api/auth")]
public class AuthController : ControllerBase
{
    private readonly AppDbContext _db;
    private readonly PasswordHasher<User> _userHasher;
    private readonly TokenService _tokenService;
    private readonly AuthCookieSettings _authCookieSettings;

    public AuthController(
        AppDbContext db,
        PasswordHasher<User> userHasher,
        TokenService tokenService,
        AuthCookieSettings authCookieSettings)
    {
        _db = db;
        _userHasher = userHasher;
        _tokenService = tokenService;
        _authCookieSettings = authCookieSettings;
    }

    [HttpPost("logout")]
    public async Task<IActionResult> Logout(CancellationToken cancellationToken)
    {
        var refreshToken = await GetRefreshToken(cancellationToken);

        if (refreshToken != null)
        {
            var session = refreshToken.Session;
            session.RevokedAt = DateTime.UtcNow;

            var refreshTokens = await _db.RefreshToken
                .Where(x => x.SessionId == session.Id)
                .ToListAsync(cancellationToken);

            foreach (var refreshTokenEntry in refreshTokens)
            {
                refreshTokenEntry.RevokedAt = DateTime.UtcNow;
                refreshTokenEntry.IsRevoked = true;
            }

            await _db.SaveChangesAsync(cancellationToken);
        }

        ClearAuthCookies();

        return NoContent();
    }

    [HttpPost("refresh")]
    public async Task<IActionResult> Refresh(CancellationToken cancellationToken)
    {
        var refreshToken = await GetRefreshToken(cancellationToken);

        if (refreshToken == null || refreshToken.Session.RevokedAt != null)
        {
            ClearAuthCookies();
            return Unauthorized();
        }

        var activeTokens = await _db.RefreshToken
            .Where(x => x.SessionId == refreshToken.SessionId && !x.IsRevoked)
            .ToListAsync(cancellationToken);

        foreach (var activeToken in activeTokens)
        {
            activeToken.IsRevoked = true;
            activeToken.RevokedAt = DateTime.UtcNow;
        }

        return await IssueTokens(
            refreshToken.Session.User,
            refreshToken.Session,
            refreshToken,
            cancellationToken);
    }

    [HttpPost("register")]
    public async Task<IActionResult> Register(RegisterRequest req, CancellationToken cancellationToken)
    {
        var email = (req.Email ?? string.Empty).Trim().ToLowerInvariant();

        if (string.IsNullOrWhiteSpace(email))
        {
            return BadRequest("Email is required.");
        }

        if (!new EmailAddressAttribute().IsValid(email))
        {
            return BadRequest("Email format is invalid.");
        }

        if (string.IsNullOrWhiteSpace(req.Password) || req.Password.Length < 8)
        {
            return BadRequest("Password must be at least 8 characters.");
        }

        if (req.Username?.Trim().Length > 50)
        {
            return BadRequest("Username must be 50 characters or fewer.");
        }

        IActionResult? result = null;
        var strategy = _db.Database.CreateExecutionStrategy();

        await strategy.ExecuteAsync(async () =>
        {
            if (await _db.Users.AnyAsync(u => u.Email == email, cancellationToken))
            {
                result = Conflict("Email already exists.");
                return;
            }

            var user = new User
            {
                Email = email,
                Username = string.IsNullOrWhiteSpace(req.Username) ? string.Empty : req.Username.Trim(),
                CreatedAtUtc = DateTime.UtcNow,
                LastSeenUtc = DateTime.UtcNow
            };

            user.PasswordHash = _userHasher.HashPassword(user, req.Password);

            _db.Users.Add(user);
            await _db.SaveChangesAsync(cancellationToken);

            var session = new Session
            {
                UserId = user.Id,
                CreatedAt = DateTime.UtcNow,
                IpAddress = HttpContext.Connection.RemoteIpAddress?.ToString(),
                UserAgent = Request.Headers["User-Agent"].ToString()
            };

            _db.Session.Add(session);
            await _db.SaveChangesAsync(cancellationToken);

            result = await IssueTokens(user, session, null, cancellationToken);
        });

        return result ?? throw new InvalidOperationException("Registration did not produce a response.");
    }

    [HttpPost("login")]
    public async Task<IActionResult> Login(LoginRequest req, CancellationToken cancellationToken)
    {
        var email = (req.Email ?? string.Empty).Trim().ToLowerInvariant();
        var user = await _db.Users.FirstOrDefaultAsync(u => u.Email == email, cancellationToken);

        if (user is null || string.IsNullOrWhiteSpace(user.PasswordHash))
        {
            return Unauthorized("Invalid email or password.");
        }

        PasswordVerificationResult ok;

        try
        {
            ok = _userHasher.VerifyHashedPassword(user, user.PasswordHash, req.Password);
        }
        catch (FormatException)
        {
            return Unauthorized("Invalid email or password.");
        }

        if (ok == PasswordVerificationResult.Failed)
        {
            return Unauthorized("Invalid email or password.");
        }

        user.LastSeenUtc = DateTime.UtcNow;

        var session = new Session
        {
            UserId = user.Id,
            CreatedAt = DateTime.UtcNow,
            IpAddress = HttpContext.Connection.RemoteIpAddress?.ToString(),
            UserAgent = Request.Headers["User-Agent"].ToString()
        };

        _db.Session.Add(session);
        await _db.SaveChangesAsync(cancellationToken);

        return await IssueTokens(user, session, null, cancellationToken);
    }

    private async Task<RefreshToken?> GetRefreshToken(CancellationToken cancellationToken)
    {
        var token = Request.Cookies["refresh_token"];

        if (string.IsNullOrEmpty(token))
        {
            return null;
        }

        var refreshToken = await _db.RefreshToken
            .Include(rt => rt.Session)
            .ThenInclude(s => s.User)
            .FirstOrDefaultAsync(rt => rt.Token == TokenHasher.Hash(token), cancellationToken);

        if (refreshToken == null)
        {
            return null;
        }

        if (refreshToken.IsRevoked ||
            refreshToken.RevokedAt != null ||
            refreshToken.ExpiresAt < DateTime.UtcNow)
        {
            refreshToken.Session.RevokedAt = DateTime.UtcNow;

            var tokens = await _db.RefreshToken
                .Where(x => x.SessionId == refreshToken.SessionId)
                .ToListAsync(cancellationToken);

            foreach (var tokenEntry in tokens)
            {
                tokenEntry.IsRevoked = true;
                tokenEntry.RevokedAt = DateTime.UtcNow;
            }

            await _db.SaveChangesAsync(cancellationToken);
            ClearAuthCookies();

            return null;
        }

        return refreshToken;
    }

    private async Task<IActionResult> IssueTokens(
        User user,
        Session session,
        RefreshToken? refreshToken,
        CancellationToken cancellationToken)
    {
        bool exists;
        string refreshJwt;

        do
        {
            refreshJwt = _tokenService.GenerateRefreshToken();

            exists = await _db.RefreshToken
                .AnyAsync(rt => rt.Token == TokenHasher.Hash(refreshJwt), cancellationToken);
        }
        while (exists);

        var newRefreshToken = new RefreshToken
        {
            SessionId = session.Id,
            ExpiresAt = DateTime.UtcNow.AddDays(7),
            IsRevoked = false,
            Token = TokenHasher.Hash(refreshJwt),
        };

        _db.RefreshToken.Add(newRefreshToken);
        await _db.SaveChangesAsync(cancellationToken);

        if (refreshToken != null)
        {
            refreshToken.ReplacedByTokenId = newRefreshToken.Id.ToString();
            await _db.SaveChangesAsync(cancellationToken);
        }

        var accessToken = _tokenService.GenerateAccessToken(user.Id.ToString());
        var accessTokenExpiresAt = DateTime.UtcNow.AddMinutes(15);
        var refreshTokenExpiresAt = DateTime.UtcNow.AddDays(7);

        Response.Cookies.Append("access_token", accessToken, BuildCookieOptions(accessTokenExpiresAt));
        Response.Cookies.Append("refresh_token", refreshJwt, BuildCookieOptions(refreshTokenExpiresAt));

        return Ok(new
        {
            isSuccess = true,
            id = user.Id,
            email = user.Email,
            username = user.Username
        });
    }

    private CookieOptions BuildCookieOptions(DateTime expiresAt)
    {
        var secure = _authCookieSettings.Secure && Request.IsHttps;

        return new CookieOptions
        {
            Domain = ResolveCookieDomain(),
            Expires = expiresAt,
            HttpOnly = true,
            IsEssential = true,
            Path = "/",
            SameSite = ResolveSameSite(secure),
            Secure = secure
        };
    }

    private SameSiteMode ResolveSameSite(bool secure)
    {
        return !secure && _authCookieSettings.SameSite == SameSiteMode.None
            ? SameSiteMode.Lax
            : _authCookieSettings.SameSite;
    }

    private string? ResolveCookieDomain()
    {
        var configuredDomain = _authCookieSettings.Domain;
        var requestHost = Request.Host.Host;

        if (string.IsNullOrWhiteSpace(configuredDomain) ||
            string.IsNullOrWhiteSpace(requestHost) ||
            requestHost.Equals("localhost", StringComparison.OrdinalIgnoreCase) ||
            IPAddress.TryParse(requestHost, out _))
        {
            return null;
        }

        var normalizedDomain = configuredDomain.Trim().TrimStart('.');

        if (string.IsNullOrWhiteSpace(normalizedDomain))
        {
            return null;
        }

        return requestHost.Equals(normalizedDomain, StringComparison.OrdinalIgnoreCase) ||
            requestHost.EndsWith($".{normalizedDomain}", StringComparison.OrdinalIgnoreCase)
            ? normalizedDomain
            : null;
    }

    private void ClearAuthCookies()
    {
        Response.Cookies.Append("refresh_token", string.Empty, BuildCookieOptions(DateTime.UtcNow.AddDays(-1)));
        Response.Cookies.Append("access_token", string.Empty, BuildCookieOptions(DateTime.UtcNow.AddDays(-1)));
    }
}
