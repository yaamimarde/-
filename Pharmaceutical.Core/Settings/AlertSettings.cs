namespace Pharmaceutical.Core.Settings;

public class AlertSettings
{
    public const string SectionName = "AlertSettings";
    public int LowStockThreshold { get; set; } = 200;
}
