namespace Pharmaceutical.Core.DTOs;

public class LoginDto
{
    public string Username { get; set; } = null!;
    public string Password { get; set; } = null!;
}

public class RegisterDto
{
    public string Username { get; set; } = null!;
    public string Password { get; set; } = null!;
    public string? DisplayName { get; set; }
    public string Role { get; set; } = "Viewer";
}

public class LoginResponseDto
{
    public string Token { get; set; } = null!;
    public string Username { get; set; } = null!;
    public string DisplayName { get; set; } = string.Empty;
    public List<string> Roles { get; set; } = new();
}

public class UserDto
{
    public string Id { get; set; } = null!;
    public string Username { get; set; } = null!;
    public string DisplayName { get; set; } = string.Empty;
    public string Role { get; set; } = "Viewer";
}

public class UpdateUserRoleDto
{
    public string Role { get; set; } = "Viewer";
}

public class ResetPasswordDto
{
    public string NewPassword { get; set; } = null!;
}
