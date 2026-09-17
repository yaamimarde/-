using Microsoft.AspNetCore.Identity;

namespace Pharmaceutical.Core;

public class AppUser : IdentityUser
{
    public string DisplayName { get; set; } = string.Empty;
}
