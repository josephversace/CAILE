using IIM.Core.Mediator;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;

namespace IIM.Application.Behaviors
{
    /// <summary>
    /// Pipeline behavior that validates requests
    /// </summary>
    [PipelineOrder(2)]
    public class ValidationBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
        where TRequest : IRequest<TResponse>
    {
        private readonly ILogger<ValidationBehavior<TRequest, TResponse>> _logger;

        /// <summary>
        /// Initializes the validation behavior
        /// </summary>
        public ValidationBehavior(ILogger<ValidationBehavior<TRequest, TResponse>> logger)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        /// <summary>
        /// Validates the request before processing
        /// </summary>
        public async Task<TResponse> Handle(
            TRequest request,
            RequestHandlerDelegate<TResponse> next,
            CancellationToken cancellationToken)
        {
            var validationErrors = ValidateRequest(request);

            if (validationErrors.Any())
            {
                var requestName = typeof(TRequest).Name;
                var errorMessage = string.Join("; ", validationErrors);

                _logger.LogWarning("Validation failed for {RequestName}: {Errors}",
                    requestName, errorMessage);

                throw new ValidationException($"Validation failed: {errorMessage}");
            }

            return await next();
        }

        /// <summary>
        /// Validates the request properties
        /// </summary>
        private List<string> ValidateRequest(TRequest request)
        {
            var errors = new List<string>();
            var properties = typeof(TRequest).GetProperties();

            foreach (var property in properties)
            {
                var value = property.GetValue(request);

                // Check for required string properties
                var requiredAttr = property.GetCustomAttribute<RequiredAttribute>();
                if (requiredAttr != null)
                {
                    if (value == null || (value is string str && string.IsNullOrWhiteSpace(str)))
                    {
                        errors.Add($"{property.Name} is required");
                    }
                }

                // Check string length
                var stringLengthAttr = property.GetCustomAttribute<StringLengthAttribute>();
                if (stringLengthAttr != null && value is string stringValue)
                {
                    if (stringValue.Length < stringLengthAttr.MinimumLength)
                    {
                        errors.Add($"{property.Name} must be at least {stringLengthAttr.MinimumLength} characters");
                    }
                    if (stringValue.Length > stringLengthAttr.MaximumLength)
                    {
                        errors.Add($"{property.Name} must be no more than {stringLengthAttr.MaximumLength} characters");
                    }
                }

                // Check range for numeric properties
                var rangeAttr = property.GetCustomAttribute<RangeAttribute>();
                if (rangeAttr != null && value != null)
                {
                    var comparable = value as IComparable;
                    if (comparable != null)
                    {
                        if (comparable.CompareTo(rangeAttr.Minimum) < 0 ||
                            comparable.CompareTo(rangeAttr.Maximum) > 0)
                        {
                            errors.Add($"{property.Name} must be between {rangeAttr.Minimum} and {rangeAttr.Maximum}");
                        }
                    }
                }

                // Check email format
                var emailAttr = property.GetCustomAttribute<EmailAddressAttribute>();
                if (emailAttr != null && value is string email && !string.IsNullOrEmpty(email))
                {
                    if (!emailAttr.IsValid(email))
                    {
                        errors.Add($"{property.Name} must be a valid email address");
                    }
                }
            }

            return errors;
        }
    }
}