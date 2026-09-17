namespace Pharmaceutical.Core.Settings;

public class JwtSettings
{
    public const string SectionName = "JwtSettings";
    public string Secret { get; set; } = string.Empty;
    public string Issuer { get; set; } = "Pharmaceutical.WebAPI";
    public string Audience { get; set; } = "Pharmaceutical.Blazor";
    public int ExpirationMinutes { get; set; } = 480;
}
