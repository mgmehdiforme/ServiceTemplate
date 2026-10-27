using FluentValidation;
using Microsoft.Extensions.DependencyInjection;
using ServiceTemplate.Application.Common.Exceptions;
using Wolverine;

namespace ServiceTemplate.Application.Common.Behaviours;

/// <summary>
/// Wolverine middleware policy validating messages against registered FluentValidation validators.
/// </summary>
public static class ValidationMiddleware
{
    public static async Task BeforeAsync(
        Envelope envelope,
        IServiceProvider serviceProvider,
        CancellationToken cancellationToken)
    {
        if (envelope.Message is null)
        {
            return;
        }

        var message = envelope.Message;
        var messageType = message.GetType();
        var validatorType = typeof(IValidator<>).MakeGenericType(messageType);
        var validators = serviceProvider.GetServices(validatorType).Cast<IValidator>().ToList();

        if (validators.Count == 0)
        {
            return;
        }

        var contextType = typeof(ValidationContext<>).MakeGenericType(messageType);
        var context = (IValidationContext)Activator.CreateInstance(contextType, message)!;

        var failures = new List<FluentValidation.Results.ValidationFailure>();
        foreach (var validator in validators)
        {
            var result = await validator.ValidateAsync(context, cancellationToken);
            if (result?.Errors is not null)
            {
                failures.AddRange(result.Errors.Where(f => f is not null));
            }
        }

        if (failures.Count != 0)
        {
            throw new Common.Exceptions.ValidationException(failures);
        }
    }
}
