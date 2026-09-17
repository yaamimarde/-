using FluentValidation;
using Pharmaceutical.Core.DTOs;

namespace Pharmaceutical.WebAPI.Validators;

public class DrugCreateDtoValidator : AbstractValidator<DrugCreateDto>
{
    public DrugCreateDtoValidator()
    {
        RuleFor(x => x.DrugId).NotEmpty().MaximumLength(50);
        RuleFor(x => x.DrugName).NotEmpty().MaximumLength(200);
        RuleFor(x => x.PurchasePrice).GreaterThanOrEqualTo(0);
        RuleFor(x => x.RetailPrice).GreaterThanOrEqualTo(0);
        RuleFor(x => x.StockQuantity).GreaterThanOrEqualTo(0);
    }
}

public class DrugUpdateDtoValidator : AbstractValidator<DrugUpdateDto>
{
    public DrugUpdateDtoValidator()
    {
        RuleFor(x => x.DrugName).NotEmpty().MaximumLength(200);
        RuleFor(x => x.PurchasePrice).GreaterThanOrEqualTo(0);
        RuleFor(x => x.RetailPrice).GreaterThanOrEqualTo(0);
    }
}

public class SupplierCreateDtoValidator : AbstractValidator<SupplierCreateDto>
{
    public SupplierCreateDtoValidator()
    {
        RuleFor(x => x.Name).NotEmpty().MaximumLength(200);
    }
}

public class StockInDtoValidator : AbstractValidator<StockInDto>
{
    public StockInDtoValidator()
    {
        RuleFor(x => x.DrugId).NotEmpty();
        RuleFor(x => x.Quantity).GreaterThan(0);
    }
}

public class StockOutDtoValidator : AbstractValidator<StockOutDto>
{
    public StockOutDtoValidator()
    {
        RuleFor(x => x.DrugId).NotEmpty();
        RuleFor(x => x.Quantity).GreaterThan(0);
    }
}
