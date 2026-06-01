using BiliTool.Vn.Domain.Services;
using FluentValidation;
using MediatR;
using Microsoft.Extensions.DependencyInjection;
using System.Reflection;

namespace BiliTool.Vn.Application;

/// <summary>Cấu hình Dependency Injection cho Application Layer</summary>
public static class ApplicationServiceRegistration
{
    public static IServiceCollection AddApplicationServices(this IServiceCollection services)
    {
        // MediatR - Đăng ký tất cả handlers trong assembly này
        services.AddMediatR(cfg =>
            cfg.RegisterServicesFromAssembly(Assembly.GetExecutingAssembly()));

        // FluentValidation - Đăng ký tất cả validators
        services.AddValidatorsFromAssembly(Assembly.GetExecutingAssembly());

        // Pipeline behavior để tự động validate trước khi xử lý command
        services.AddTransient(typeof(IPipelineBehavior<,>),
            typeof(ValidationPipelineBehavior<,>));

        return services;
    }
}

/// <summary>MediatR pipeline để tự động validate trước khi xử lý</summary>
public class ValidationPipelineBehavior<TRequest, TResponse>
    : IPipelineBehavior<TRequest, TResponse>
    where TRequest : notnull
{
    private readonly IEnumerable<IValidator<TRequest>> _validators;

    public ValidationPipelineBehavior(IEnumerable<IValidator<TRequest>> validators)
        => _validators = validators;

    public async Task<TResponse> Handle(
        TRequest request,
        RequestHandlerDelegate<TResponse> next,
        CancellationToken cancellationToken)
    {
        if (!_validators.Any()) return await next();

        var context = new ValidationContext<TRequest>(request);
        var failures = _validators
            .Select(v => v.Validate(context))
            .SelectMany(r => r.Errors)
            .Where(e => e != null)
            .ToList();

        if (failures.Count > 0)
        {
            var cauTrucLoi = failures
                .GroupBy(e => e.PropertyName)
                .ToDictionary(
                    g => g.Key,
                    g => g.Select(e => e.ErrorMessage).ToArray()
                );

            throw new LoiXacThucException(cauTrucLoi);
        }

        return await next();
    }
}

/// <summary>Exception cho lỗi xác thực dữ liệu đầu vào</summary>
public class LoiXacThucException : Exception
{
    public IDictionary<string, string[]> LoiXacThuc { get; }

    public LoiXacThucException(IDictionary<string, string[]> loiXacThuc)
        : base("Dữ liệu đầu vào không hợp lệ.")
        => LoiXacThuc = loiXacThuc;
}
