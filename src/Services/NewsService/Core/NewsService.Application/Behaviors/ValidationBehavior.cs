using FluentValidation;
using MediatR;
// FluentValidation'ın kendi ValidationException'ı ile isim çakışıyor —
// burada her zaman Shared.Exceptions.ValidationException kastediliyor.
using ValidationException = Shared.Exceptions.ValidationException;

namespace NewsService.Application.Behaviors;

/// <summary>
/// Handler'a ulaşmadan önce ilgili command/query için kayıtlı FluentValidation
/// validator'larını çalıştırır. Hata varsa Shared.Exceptions.ValidationException
/// fırlatır — GlobalExceptionHandler bunu zaten 400 + errors sözlüğüne çeviriyor,
/// ayrı bir hata yönetimi eklemeye gerek kalmıyor.
///
/// IdentityService'te aynı adlı bir ikizi var. Shared bir servisin iç işleyişini
/// değil, yalnızca servis sınırını geçen sözleşmeleri taşıdığı için bilinçli
/// olarak ortaklaştırılmadı. Doğrulama hatasının nasıl raporlandığını
/// değiştiren bir düzeltme yaparken diğerine de bakılmalı; aksi halde iki
/// servis aynı hataya farklı yanıt üretir.
/// </summary>
public class ValidationBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
    where TRequest : notnull
{
    private readonly IEnumerable<IValidator<TRequest>> _validators;

    public ValidationBehavior(IEnumerable<IValidator<TRequest>> validators)
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

        var failures = (await Task.WhenAll(_validators.Select(v => v.ValidateAsync(context, cancellationToken))))
            .SelectMany(result => result.Errors)
            .Where(failure => failure is not null)
            .ToList();

        if (failures.Count > 0)
        {
            var errors = failures
                .GroupBy(failure => failure.PropertyName)
                .ToDictionary(group => group.Key, group => group.Select(f => f.ErrorMessage).ToArray());

            throw new ValidationException(errors);
        }

        return await next();
    }
}
