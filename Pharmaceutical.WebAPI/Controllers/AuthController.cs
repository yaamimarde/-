using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using Pharmaceutical.Core;
using Pharmaceutical.Core.DTOs;
using Pharmaceutical.Core.Settings;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

namespace Pharmaceutical.WebAPI.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AuthController : ControllerBase
{
    private readonly UserManager<AppUser> _userManager;
    private readonly SignInManager<AppUser> _signInManager;
    private readonly JwtSettings _jwtSettings;

    public AuthController(
        UserManager<AppUser> userManager,
        SignInManager<AppUser> signInManager,
        IOptions<JwtSettings> jwtSettings)
    {
        _userManager = userManager;
        _signInManager = signInManager;
        _jwtSettings = jwtSettings.Value;
    }

    [HttpPost("login")]
    public async Task<IActionResult> Login([FromBody] LoginDto model)
    {
        var user = await _userManager.FindByNameAsync(model.Username);
        if (user == null)
            return Unauthorized(ApiResponse<object>.Fail("用户名或密码错误"));

        var result = await _signInManager.CheckPasswordSignInAsync(user, model.Password, false);
        if (!result.Succeeded)
            return Unauthorized(ApiResponse<object>.Fail("用户名或密码错误"));

        var roles = await _userManager.GetRolesAsync(user);
        var token = GenerateJwtToken(user, roles);

        return Ok(ApiResponse<LoginResponseDto>.Ok(new LoginResponseDto
        {
            Token = token,
            Username = user.UserName ?? model.Username,
            DisplayName = user.DisplayName,
            Roles = roles.ToList()
        }, "登录成功"));
    }

    [HttpGet("me")]
    [Authorize]
    public IActionResult Me()
    {
        var username = User.FindFirstValue(ClaimTypes.Name) ?? string.Empty;
        var displayName = User.FindFirstValue("display_name") ?? username;
        var role = User.FindFirst(ClaimTypes.Role)?.Value ?? "Viewer";
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? string.Empty;

        return Ok(ApiResponse<UserDto>.Ok(new UserDto
        {
            Id = userId,
            Username = username,
            DisplayName = displayName,
            Role = role
        }));
    }

    [HttpPost("register")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> Register([FromBody] RegisterDto model)
    {
        var existing = await _userManager.FindByNameAsync(model.Username);
        if (existing != null)
            return BadRequest(ApiResponse<object>.Fail("用户名已存在"));

        var user = new AppUser
        {
            UserName = model.Username,
            DisplayName = model.DisplayName ?? model.Username
        };

        var result = await _userManager.CreateAsync(user, model.Password);
        if (!result.Succeeded)
            return BadRequest(ApiResponse<object>.Fail(string.Join("; ", result.Errors.Select(e => e.Description))));

        var role = model.Role is "Admin" or "Operator" or "Viewer" ? model.Role : "Viewer";
        await _userManager.AddToRoleAsync(user, role);

        return Ok(ApiResponse<object>.Ok(null, "用户注册成功"));
    }

    private string GenerateJwtToken(AppUser user, IList<string> roles)
    {
        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, user.Id),
            new(ClaimTypes.Name, user.UserName ?? string.Empty),
            new("display_name", user.DisplayName)
        };

        claims.AddRange(roles.Select(r => new Claim(ClaimTypes.Role, r)));

        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_jwtSettings.Secret));
        var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);
        var expires = DateTime.UtcNow.AddMinutes(_jwtSettings.ExpirationMinutes);

        var token = new JwtSecurityToken(
            issuer: _jwtSettings.Issuer,
            audience: _jwtSettings.Audience,
            claims: claims,
            expires: expires,
            signingCredentials: creds);

        return new JwtSecurityTokenHandler().WriteToken(token);
    }
}
