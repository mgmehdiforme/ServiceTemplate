using FluentValidation;

namespace ServiceTemplate.Application.Orders.CreateOrder;

/// <summary>
/// Validator for CreateOrderCommand enforcing business input invariants.
/// </summary>
public sealed class CreateOrderValidator : AbstractValidator<CreateOrderCommand>
{
    public CreateOrderValidator()
    {
        RuleFor(x => x.CustomerId)
            .NotEmpty().WithMessage("CustomerId is required.")
            .MaximumLength(100).WithMessage("CustomerId must not exceed 100 characters.");

        RuleFor(x => x.Currency)
            .NotEmpty().WithMessage("Currency is required.")
            .Length(3).WithMessage("Currency must be a 3-letter ISO code.");

        RuleFor(x => x.ShippingAddress)
            .NotNull().WithMessage("ShippingAddress is required.");

        When(x => x.ShippingAddress is not null, () =>
        {
            RuleFor(x => x.ShippingAddress.Street)
                .NotEmpty().WithMessage("Street is required.");

            RuleFor(x => x.ShippingAddress.City)
                .NotEmpty().WithMessage("City is required.");

            RuleFor(x => x.ShippingAddress.Country)
                .NotEmpty().WithMessage("Country is required.");
        });

        RuleFor(x => x.Items)
            .NotEmpty().WithMessage("Items collection cannot be empty.");

        RuleForEach(x => x.Items).ChildRules(item =>
        {
            item.RuleFor(i => i.Sku)
                .NotEmpty().WithMessage("Item SKU is required.");

            item.RuleFor(i => i.ProductName)
                .NotEmpty().WithMessage("Item ProductName is required.");

            item.RuleFor(i => i.Quantity)
                .GreaterThan(0).WithMessage("Item Quantity must be greater than zero.");

            item.RuleFor(i => i.UnitPrice)
                .GreaterThan(0m).WithMessage("Item UnitPrice must be greater than zero.");
        });
    }
}
