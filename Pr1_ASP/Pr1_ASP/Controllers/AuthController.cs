using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Pr1_ASP.Dtos;
using Pr1_ASP.Services;

namespace Pr1_ASP.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AuthController : ControllerBase
{
    private readonly IUserService _userService;
    private readonly ITokenService _tokenService;

    public AuthController(IUserService userService, ITokenService tokenService)
    {
        _userService = userService;
        _tokenService = tokenService;
    }

    [HttpPost("register")]
    public async Task<IActionResult> Register([FromBody] RegisterDto dto, CancellationToken ct)
    {
        try
        {
            var (user, roleName) = await _userService.RegisterAsync(dto, ct);
            var token = _tokenService.CreateAccessToken(user, roleName);
            return Ok(new LoginResponseDto { AccessToken = token, ExpiresInMinutes = 60 });
        }
        catch (ConflictException ex) { return Conflict(new { error = ex.Message }); }
        catch (InvalidOperationException ex) { return BadRequest(new { error = ex.Message }); }
    }

    [EnableRateLimiting("LoginPolicy")]
    [HttpPost("login")]
    public async Task<IActionResult> Login([FromBody] LoginDto dto, CancellationToken ct)
    {
        var result = await _userService.ValidateCredentialsAsync(dto, ct);
        if (result == null) return Unauthorized(new { error = "Неверный логин или пароль." });

        var token = _tokenService.CreateAccessToken(result.Value.User, result.Value.RoleName);
        return Ok(new LoginResponseDto { AccessToken = token, ExpiresInMinutes = 60 });
    }
}