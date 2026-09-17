namespace Pharmaceutical.Core.DTOs;

public class SupplierDto
{
    public int SupplierId { get; set; }
    public string Name { get; set; } = null!;
    public string ContactPerson { get; set; } = string.Empty;
    public string Phone { get; set; } = string.Empty;
    public string Address { get; set; } = string.Empty;
}

public class SupplierCreateDto
{
    public string Name { get; set; } = null!;
    public string? ContactPerson { get; set; }
    public string? Phone { get; set; }
    public string? Address { get; set; }
}

public class SupplierUpdateDto
{
    public string Name { get; set; } = null!;
    public string? ContactPerson { get; set; }
    public string? Phone { get; set; }
    public string? Address { get; set; }
}
