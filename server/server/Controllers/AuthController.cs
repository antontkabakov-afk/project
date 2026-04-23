using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using server.Date;
using server.Models;
using server.Service;
using static System.Collections.Specialized.BitVector32;

namespace server.Controllers;

public record RegisterRequest(string Email, string Password, string Username);
public record LoginRequest(string Email, string Password);

[ApiController]
[Route("api/auth")]
public class AuthController : ControllerBase
{
    private readonly AppDbContext _db;
    private readonly PasswordHasher<User> _userHasher;
    private readonly TokenService _tokenService;

    public AuthController(
        AppDbContext db, 
        PasswordHasher<User> userHasher,
        TokenService tokenService)
    {
        _db = db;
        _userHasher = userHasher;
        _tokenService = tokenService;
    }

    [HttpPost("logout")]
    public async Task<IActionResult> Logout()
    {
        var refreshToken = await GetRefreshToken();

        if (refreshToken == null)
        {
            return Unauthorized();
        }

        var session = refreshToken.Session;

        session.RevokedAt = DateTime.UtcNow;

        var refreshTokens = await _db.RefreshToken
            .Where(x => x.SessionId == session.Id)
            .ToListAsync();

        foreach (var i in refreshTokens)
        {
            i.RevokedAt = DateTime.UtcNow;
            i.IsRevoked = true;
        }

        await _db.SaveChangesAsync();

        var cookieOptions = new CookieOptions
        {
            HttpOnly = true,
            Secure = true,
            SameSite = SameSiteMode.None,
            Expires = DateTime.UtcNow.AddDays(-1)
        };

        Response.Cookies.Append("refresh_token", "", cookieOptions);
        Response.Cookies.Append("access_token", "", cookieOptions);

        return Ok();
    }

    [HttpPost("refresh")]
    public async Task<IActionResult> Refresh()
    {
        var refreshToken = await GetRefreshToken();

        if (refreshToken == null || refreshToken.Session.RevokedAt != null)
        {
            return Unauthorized();
        }

        var activeTokens = await _db.RefreshToken
            .Where(x => x.SessionId == refreshToken.SessionId && !x.IsRevoked)
            .ToListAsync();

        foreach (var t in activeTokens)
        {
            t.IsRevoked = true;
            t.RevokedAt = DateTime.UtcNow;
        }

        return await IssueTokens(refreshToken.Session.User, refreshToken.Session,refreshToken);
    }

    [HttpPost("register")]
    public async Task<IActionResult> Register(RegisterRequest req)
    {
        var email = (req.Email ?? "").Trim().ToLowerInvariant();

        if (string.IsNullOrWhiteSpace(email)) 
            return BadRequest("Email is required.");
        if (string.IsNullOrWhiteSpace(req.Password) || req.Password.Length < 6)
            return BadRequest("Password must be at least 6 characters.");
        if (await _db.Users.AnyAsync(u => u.Email == email))
            return Conflict("Email already exists.");

        var tx = await _db.Database.BeginTransactionAsync();

        var user = new User
        {
            Email = email,
            Username = string.IsNullOrWhiteSpace(req.Username) ? string.Empty : req.Username.Trim(),
            CreatedAtUtc = DateTime.UtcNow,
            LastSeenUtc = DateTime.UtcNow
        };

        user.PasswordHash = _userHasher.HashPassword(user, req.Password);

        _db.Users.Add(user);
        await _db.SaveChangesAsync();

        var session = new Session
        {
            UserId = user.Id,
            CreatedAt = DateTime.UtcNow,
            IpAddress = HttpContext.Connection.RemoteIpAddress?.ToString(),
            UserAgent = Request.Headers["User-Agent"].ToString()
        };

        _db.Session.Add(session);
        await _db.SaveChangesAsync();

        var result = await IssueTokens(user, session, null);

        await tx.CommitAsync();

        return result;
    }

    [HttpPost("login")]
    public async Task<IActionResult> Login(LoginRequest req)
    {
        var email = (req.Email ?? "").Trim().ToLowerInvariant();
        var user = await _db.Users.FirstOrDefaultAsync(u => u.Email == email);
        if (user is null) 
            return Unauthorized("Invalid email or password.");

        var ok = _userHasher.VerifyHashedPassword(user, user.PasswordHash, req.Password);
        if (ok == PasswordVerificationResult.Failed)
            return Unauthorized("Invalid email or password.");

        user.LastSeenUtc = DateTime.UtcNow;

        var session = new Session
        {
            UserId = user.Id,
            CreatedAt = DateTime.UtcNow,
            IpAddress = HttpContext.Connection.RemoteIpAddress?.ToString(),
            UserAgent = Request.Headers["User-Agent"].ToString()
        };

        _db.Session.Add(session);
        await _db.SaveChangesAsync();

        return await IssueTokens(user, session,null);
    }

    private async Task<RefreshToken?> GetRefreshToken()
    {
        var token = Request.Cookies["refresh_token"];

        if (string.IsNullOrEmpty(token))
            return null;

        var refreshToken = await _db.RefreshToken
            .Include(rt => rt.Session)
            .ThenInclude(s => s.User)
            .FirstOrDefaultAsync(rt => rt.Token == TokenHasher.Hash(token));

        if (refreshToken == null) return null;

        if (refreshToken.IsRevoked ||
            refreshToken.RevokedAt != null ||
            refreshToken.ExpiresAt < DateTime.UtcNow)
        {
            refreshToken.Session.RevokedAt = DateTime.UtcNow;

            var tokens = await _db.RefreshToken
                .Where(x => x.SessionId == refreshToken.SessionId)
                .ToListAsync();

            foreach (var t in tokens)
            {
                t.IsRevoked = true;
                t.RevokedAt = DateTime.UtcNow;
            }

            await _db.SaveChangesAsync();

            return null;
        }

        return refreshToken;
    }

    private async Task<IActionResult> IssueTokens(User user, Session session, RefreshToken? refreshToken)
    {
        bool exists;
        string refreshJwt;

        do
        {
            refreshJwt = _tokenService.GenerateRefreshToken();

            exists = await _db.RefreshToken
                .AnyAsync(rt => rt.Token == TokenHasher.Hash(refreshJwt));

        } while (exists);

        var newRefreshToken = new RefreshToken
        {
            SessionId = session.Id,
            ExpiresAt = DateTime.UtcNow.AddDays(7),
            IsRevoked = false,
            Token = TokenHasher.Hash(refreshJwt),
        };

        _db.RefreshToken.Add(newRefreshToken);
        await _db.SaveChangesAsync();

        if (refreshToken != null)
        {
            refreshToken.ReplacedByTokenId = newRefreshToken.Id.ToString();
            await _db.SaveChangesAsync();
        }

        var accessToken = _tokenService.GenerateAccessToken(user.Id.ToString());

        Response.Cookies.Append("access_token", accessToken, new CookieOptions
        {
            HttpOnly = true,
            Secure = true,
            SameSite = SameSiteMode.None,
            Expires = DateTime.UtcNow.AddMinutes(15)
        });

        Response.Cookies.Append("refresh_token", refreshJwt, new CookieOptions
        {
            HttpOnly = true,
            Secure = true,
            SameSite = SameSiteMode.None,
            Expires = DateTime.UtcNow.AddDays(7)
        });

        return Ok(new
        {
            isSuccess = true,
            user.Email,
            user.Username
        });
    }
}
