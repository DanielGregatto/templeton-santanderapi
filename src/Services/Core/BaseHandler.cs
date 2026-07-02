using Domain.Contracts.Common;
using FluentValidation;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Services.Core
{
    public abstract class BaseHandler
    {
        /// <summary>
        /// Validates the request using the provided validator. If validation fails, returns a Result with validation errors; otherwise, returns null.
        /// </summary>
        /// <typeparam name="TRequest">The type of the request to validate.</typeparam>
        /// <typeparam name="TResponse">The type of the response.</typeparam>
        /// <param name="validator">The validator to use for validation.</param>
        /// <param name="request">The request to validate.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>A Result with validation errors if validation fails; otherwise, null.</returns>
        protected async Task<Result<TResponse>?> ValidateAsync<TRequest, TResponse>(
            IValidator<TRequest> validator,
            TRequest request,
            CancellationToken cancellationToken)
        {
            var validationResult = await validator.ValidateAsync(request, cancellationToken);

            if (!validationResult.IsValid)
            {
                var errors = validationResult.Errors
                    .Select(e => Error.Validation(e.ErrorMessage, e.PropertyName))
                    .ToArray();
                return Result<TResponse>.ValidationFailure(errors);
            }

            return null;
        }
    }
}
