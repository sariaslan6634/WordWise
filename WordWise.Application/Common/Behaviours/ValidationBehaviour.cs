using FluentValidation;
using MediatR;
using WordWise.Application.Common.Exceptions;
using ValidationException = WordWise.Application.Common.Exceptions.ValidationException;

namespace WordWise.Application.Common.Behaviours
{
    public class ValidationBehaviour<TRequest, TResponse>
         : IPipelineBehavior<TRequest, TResponse>
         where TRequest : IRequest<TResponse>
    {
        private readonly IEnumerable<IValidator<TRequest>> _validators;

        public ValidationBehaviour(IEnumerable<IValidator<TRequest>> validators)
        {
            _validators = validators;
        }

        public async Task<TResponse> Handle(
            TRequest request,
            RequestHandlerDelegate<TResponse> next,
            CancellationToken cancellationToken)
        {
            if (!_validators.Any())
                return await next();

            var context = new ValidationContext<TRequest>(request);

            var failures = _validators
                .Select(v => v.Validate(context))
                .SelectMany(result => result.Errors)
                .Where(f => f != null)
                .GroupBy(f => f.PropertyName)
                .ToDictionary(
                    g => g.Key,
                    g => g.Select(f => f.ErrorMessage).ToArray()
                );

            if (failures.Any())
                throw new ValidationException(failures);

            return await next();
        }
    }
}
