using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using server.Date;
using server.Models;
using server.Service;

namespace server.Controllers;

public record RegisterRequest(string Email, string Password, string Username);
public record LoginRequest(string Email, string Password);

[ApiController]
[Route("api/auth")]
public class AuthController : ControllerBase
{
    private readonly AppDbContext _db;
    private readonly PasswordHasher<User> _hasher;
    private readonly TokenService _tokenService;

    public AuthController(AppDbContext db, PasswordHasher<User> hasher, TokenService tokenService)
    {
        _db = db;
        _hasher = hasher;
        _tokenService = tokenService;
    }

    //todo logout

    [HttpPost("refresh")]
    public async Task<IActionResult> Refresh()
    {
        var token = Request.Cookies["refresh_token"];

        if (string.IsNullOrEmpty(token))
            return Unauthorized();

        var refreshToken = await _db.RefreshToken
            .Include(rt => rt.Session)
            .ThenInclude(s => s.User)
            .FirstOrDefaultAsync(rt => rt.Token == token);

        if (refreshToken == null)
            return Unauthorized();

        if (refreshToken.IsRevoked)
            return Unauthorized();

        if (refreshToken.ExpiresAt <= DateTime.UtcNow)
            return Unauthorized();

        if (refreshToken.RevokedAt != null)
            return Unauthorized();

        var user = refreshToken.Session.User;

        var accessToken = _tokenService.GenerateAccessToken(user.Id.ToString());

        Response.Cookies.Append("access_token", accessToken, new CookieOptions
        {
            HttpOnly = true,
            Secure = true,
            SameSite = SameSiteMode.None,
            Expires = DateTime.UtcNow.AddMinutes(15)
        });

        return Ok();
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

        var user = new User
        {
            Email = email,
            Username = string.IsNullOrWhiteSpace(req.Username) ? null : req.Username.Trim(),
            CreatedAtUtc = DateTime.UtcNow,
            LastSeenUtc = DateTime.UtcNow
        };
        user.PasswordHash = _hasher.HashPassword(user, req.Password);

        _db.Users.Add(user);
        await _db.SaveChangesAsync(); 

        return await IssueTokens(user);
    }

    [HttpPost("login")]
    public async Task<IActionResult> Login(LoginRequest req)
    {
        var email = (req.Email ?? "").Trim().ToLowerInvariant();
        var user = await _db.Users.FirstOrDefaultAsync(u => u.Email == email);
        if (user is null) 
            return Unauthorized("Invalid email or password.");

        var ok = _hasher.VerifyHashedPassword(user, user.PasswordHash, req.Password);
        if (ok == PasswordVerificationResult.Failed)
            return Unauthorized("Invalid email or password.");

        return await IssueTokens(user);
    }

    private async Task<IActionResult> IssueTokens(User user)
    {
        var session = new Session
        {
            UserId = user.Id,
            CreatedAt = DateTime.UtcNow,
            IpAddress = HttpContext.Connection.RemoteIpAddress?.ToString(),
            UserAgent = Request.Headers["User-Agent"].ToString()
        };

        _db.Session.Add(session);
        await _db.SaveChangesAsync();

        var refreshJwt = _tokenService.GenerateRefreshToken();

        var refreshToken = new RefreshToken
        {
            SessionId = session.Id,
            ExpiresAt = DateTime.UtcNow.AddDays(7),
            IsRevoked = false,
            Token = refreshJwt
        };

        _db.RefreshToken.Add(refreshToken);
        await _db.SaveChangesAsync();

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
            user.Id,
            user.Email,
            user.Username
        });
    }
}