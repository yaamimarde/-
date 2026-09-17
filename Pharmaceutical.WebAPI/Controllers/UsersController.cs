using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Pharmaceutical.Core;
using Pharmaceutical.Core.DTOs;
using System.Security.Claims;

namespace Pharmaceutical.WebAPI.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize(Roles = "Admin")]
public class UsersController : ControllerBase
{
    private readonly UserManager<AppUser> _userManager;
    private readonly RoleManager<IdentityRole> _roleManager;

    public UsersController(UserManager<AppUser> userManager, RoleManager<IdentityRole> roleManager)
    {
        _userManager = userManager;
        _roleManager = roleManager;
    }

    [HttpGet]
    public async Task<IActionResult> GetAll()
    {
        var users = await _userManager.Users.OrderBy(u => u.UserName).ToListAsync();
        var result = new List<UserDto>();
        foreach (var user in users)
        {
            var roles = await _userManager.GetRolesAsync(user);
            result.Add(new UserDto
            {
                Id = user.Id,
                Username = user.UserName ?? string.Empty,
                DisplayName = user.DisplayName,
                Role = roles.FirstOrDefault() ?? "Viewer"
            });
        }
        return Ok(ApiResponse<List<UserDto>>.Ok(result));
    }

    [HttpPut("{id}/role")]
    public async Task<IActionResult> UpdateRole(string id, [FromBody] UpdateUserRoleDto dto)
    {
        var user = await _userManager.FindByIdAsync(id);
        if (user == null)
            return NotFound(ApiResponse<object>.Fail("用户不存在"));

        var role = dto.Role is "Admin" or "Operator" or "Viewer" ? dto.Role : "Viewer";
        var currentRoles = await _userManager.GetRolesAsync(user);

        if (currentRoles.Contains("Admin") && role != "Admin")
        {
            var adminCount = await CountAdminsAsync();
            if (adminCount <= 1)
                return BadRequest(ApiResponse<object>.Fail("不能移除唯一的管理员"));
        }

        await _userManager.RemoveFromRolesAsync(user, currentRoles);
        await _userManager.AddToRoleAsync(user, role);
        return Ok(ApiResponse<object>.Ok(null, "角色已更新"));
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(string id)
    {
        var currentUserId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (id == currentUserId)
            return BadRequest(ApiResponse<object>.Fail("不能删除当前登录用户"));

        var user = await _userManager.FindByIdAsync(id);
        if (user == null)
            return NotFound(ApiResponse<object>.Fail("用户不存在"));

        var roles = await _userManager.GetRolesAsync(user);
        if (roles.Contains("Admin"))
        {
            var adminCount = await CountAdminsAsync();
            if (adminCount <= 1)
                return BadRequest(ApiResponse<object>.Fail("不能删除唯一的管理员"));
        }

        var result = await _userManager.DeleteAsync(user);
        if (!result.Succeeded)
            return BadRequest(ApiResponse<object>.Fail(string.Join("; ", result.Errors.Select(e => e.Description))));

        return Ok(ApiResponse<object>.Ok(null, "用户已删除"));
    }

    [HttpPost("{id}/reset-password")]
    public async Task<IActionResult> ResetPassword(string id, [FromBody] ResetPasswordDto dto)
    {
        if (string.IsNullOrWhiteSpace(dto.NewPassword))
            return BadRequest(ApiResponse<object>.Fail("密码不能为空"));

        var user = await _userManager.FindByIdAsync(id);
        if (user == null)
            return NotFound(ApiResponse<object>.Fail("用户不存在"));

        var token = await _userManager.GeneratePasswordResetTokenAsync(user);
        var result = await _userManager.ResetPasswordAsync(user, token, dto.NewPassword);
        if (!result.Succeeded)
            return BadRequest(ApiResponse<object>.Fail(string.Join("; ", result.Errors.Select(e => e.Description))));

        return Ok(ApiResponse<object>.Ok(null, "密码已重置"));
    }

    private async Task<int> CountAdminsAsync()
    {
        var admins = await _userManager.GetUsersInRoleAsync("Admin");
        return admins.Count;
    }
}
