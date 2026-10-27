using FluentValidation;
using ServiceTemplate.Application.Common.Exceptions;

namespace ServiceTemplate.Application.Common.Behaviours;

/// <summary>
/// Wolverine middleware policy validating messages against registered FluentValidation validators.
/// </summary>
public static class ValidationMiddleware
{
    public static async Task BeforeAsync<TMessage>(
        TMessage message,
        IEnumerable<IValidator<TMessage>> validators,
        CancellationToken cancellationToken)
    {
        if (message is null)
        {
            return;
        }

        var validatorList = validators.ToList();
        if (validatorList.Count == 0)
        {
            return;
        }

        var context = new ValidationContext<TMessage>(message);
        var validationResults = await Task.WhenAll(
            validatorList.Select(v => v.ValidateAsync(context, cancellationToken)));

        var failures = validationResults
            .SelectMany(r => r.Errors)
            .Where(f => f is not null)
            .ToList();

        if (failures.Count != 0)
        {
            throw new Common.Exceptions.ValidationException(failures);
        }
    }
}
