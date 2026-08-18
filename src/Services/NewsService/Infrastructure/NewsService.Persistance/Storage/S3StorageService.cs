using System.Net;
using Amazon.S3;
using Amazon.S3.Model;
using Microsoft.Extensions.Options;
using NewsService.Application.Interfaces;
using NewsService.Application.Media;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Processing;
using Shared.Exceptions;

namespace NewsService.Persistance.Storage;

/// <summary>
/// S3 API'si üzerinden çalışan depo. Yerelde MinIO, üretimde AWS S3 —
/// aradaki fark yalnızca yapılandırma.
/// </summary>
public sealed class S3StorageService : IStorageService, IDisposable
{
    /// <summary>Henüz bir makaleye bağlanmamış yüklemeler. Lifecycle kuralı burayı 1 gün sonra temizler.</summary>
    private const string StagingPrefix = "staging/";

    /// <summary>Doğrulanmış ve kalıcılaşmış medya.</summary>
    private const string ArticlesPrefix = "articles/";

    private readonly StorageOptions _options;
    private readonly IAmazonS3 _internalClient;
    private readonly IAmazonS3 _publicClient;

    public S3StorageService(IOptions<StorageOptions> options)
    {
        _options = options.Value;
        _internalClient = CreateClient(_options.Endpoint);
        _publicClient = CreateClient(_options.PublicEndpoint);
    }

    private IAmazonS3 CreateClient(string serviceUrl) => new AmazonS3Client(
        _options.AccessKey,
        _options.SecretKey,
        new AmazonS3Config
        {
            ServiceURL = serviceUrl,
            // ServiceURL http olsa bile SDK, UseHttp açıkça verilmezse adresi
            // https'e çevirir; MinIO yerelde TLS konuşmadığı için imzalı URL
            // tarayıcıda bağlantı hatasına düşer.
            UseHttp = serviceUrl.StartsWith("http://", StringComparison.OrdinalIgnoreCase),
            // MinIO "host/bucket" biçimini kullanır; AWS'in varsayılanı olan
            // "bucket.host" alt alan adı biçimi MinIO'da çözümlenemez.
            ForcePathStyle = true,
            AuthenticationRegion = "us-east-1",
        });

    public PresignedUpload CreateUploadUrl(string contentType, long sizeBytes)
    {
        if (!MediaPolicy.IsAllowed(contentType))
        {
            throw new BusinessException(
                ErrorCodes.Media.TypeNotAllowed,
                "Bu dosya türü yüklenemez.",
                $"İzin verilen türler: {string.Join(", ", MediaPolicy.AllowedContentTypes)}.");
        }

        if (sizeBytes <= 0 || sizeBytes > MediaPolicy.MaxSizeBytes)
        {
            throw new BusinessException(
                ErrorCodes.Media.TooLarge,
                "Dosya boyutu sınırı aşıyor.",
                $"En fazla {MediaPolicy.MaxSizeBytes / (1024 * 1024)} MB yükleyebilirsiniz.");
        }

        var key = $"{StagingPrefix}{Guid.NewGuid():N}{MediaPolicy.ExtensionFor(contentType)}";

        // ContentType imzaya dahil edilir: client PUT sırasında aynı başlığı
        // göndermezse imza doğrulaması başarısız olur.
        var url = _publicClient.GetPreSignedURL(new GetPreSignedUrlRequest
        {
            BucketName = _options.Bucket,
            Key = key,
            Verb = HttpVerb.PUT,
            Expires = DateTime.UtcNow.AddSeconds(_options.UploadUrlTtlSeconds),
            ContentType = contentType,
        });

        return new PresignedUpload(MatchEndpointScheme(url), key, contentType, _options.UploadUrlTtlSeconds);
    }

    public async Task<string> CommitAsync(string stagingKey, CancellationToken cancellationToken = default)
    {
        // Anahtar client'tan geliyor; staging dışına çıkan bir değerle
        // depodaki başka bir nesnenin taşınması engellenir.
        if (string.IsNullOrWhiteSpace(stagingKey)
            || !stagingKey.StartsWith(StagingPrefix, StringComparison.Ordinal)
            || stagingKey.Contains("..", StringComparison.Ordinal))
        {
            throw new BusinessException(
                ErrorCodes.Media.InvalidKey,
                "Geçersiz yükleme anahtarı.",
                "Anahtar bu oturumda üretilen geçici yükleme adresine ait olmalı.");
        }

        GetObjectMetadataResponse head;
        try
        {
            head = await _internalClient.GetObjectMetadataAsync(_options.Bucket, stagingKey, cancellationToken);
        }
        catch (AmazonS3Exception ex) when (ex.StatusCode == HttpStatusCode.NotFound)
        {
            throw new BusinessException(
                ErrorCodes.Media.NotFound,
                "Yüklenen dosya bulunamadı.",
                "Yükleme tamamlanmamış veya geçici dosya süresi dolmuş olabilir.");
        }

        // Buradan itibaren beyana değil, depodaki gerçek dosyaya bakılır.
        var actualType = head.Headers.ContentType;

        if (head.ContentLength > MediaPolicy.MaxSizeBytes)
        {
            await DiscardAsync(stagingKey, cancellationToken);
            throw new BusinessException(
                ErrorCodes.Media.TooLarge,
                "Dosya boyutu sınırı aşıyor.",
                $"En fazla {MediaPolicy.MaxSizeBytes / (1024 * 1024)} MB yükleyebilirsiniz.");
        }

        if (!MediaPolicy.IsAllowed(actualType))
        {
            await DiscardAsync(stagingKey, cancellationToken);
            throw new BusinessException(
                ErrorCodes.Media.TypeNotAllowed,
                "Bu dosya türü yüklenemez.",
                $"İzin verilen türler: {string.Join(", ", MediaPolicy.AllowedContentTypes)}.");
        }

        if (!await HasMatchingSignatureAsync(stagingKey, actualType!, cancellationToken))
        {
            await DiscardAsync(stagingKey, cancellationToken);
            throw new BusinessException(
                ErrorCodes.Media.CorruptContent,
                "Dosya içeriği türüyle uyuşmuyor.",
                "Dosya bozuk olabilir veya uzantısı gerçek içeriğini yansıtmıyor.");
        }

        // Temel anahtar uzantısız: gerçek dosyalar "-400.webp" gibi eklerle duruyor.
        var baseKey = $"{ArticlesPrefix}{DateTime.UtcNow:yyyy/MM}/{Path.GetFileNameWithoutExtension(stagingKey)}";

        await WriteVariantsAsync(stagingKey, baseKey, cancellationToken);
        await DiscardAsync(stagingKey, cancellationToken);

        return baseKey;
    }

    public string? ResolvePublicUrl(string? storedValue)
    {
        if (string.IsNullOrWhiteSpace(storedValue))
            return null;

        // Editörün elle yapıştırdığı dış adresler ve mevcut kayıtlar bozulmadan geçer.
        if (storedValue.StartsWith("http://", StringComparison.OrdinalIgnoreCase)
            || storedValue.StartsWith("https://", StringComparison.OrdinalIgnoreCase))
        {
            return storedValue;
        }

        var key = storedValue.TrimStart('/');

        // Varyantlı kayıtlarda tek adres istendiğinde orta boy verilir.
        if (MediaVariants.HasVariants(key))
            key = MediaVariants.FileKey(key, MediaVariants.DefaultWidth);

        return $"{PublicBase}/{key}";
    }

    public string? ResolveSrcset(string? storedValue)
    {
        if (string.IsNullOrWhiteSpace(storedValue))
            return null;

        // Dış adreslerin ve varyant öncesi yüklenen tek dosyalı kayıtların
        // farklı boyları yok; srcset üretmek 404'e yol açardı.
        if (storedValue.StartsWith("http://", StringComparison.OrdinalIgnoreCase)
            || storedValue.StartsWith("https://", StringComparison.OrdinalIgnoreCase))
        {
            return null;
        }

        var key = storedValue.TrimStart('/');
        if (!MediaVariants.HasVariants(key))
            return null;

        return string.Join(", ", MediaVariants.Widths.Select(width =>
            $"{PublicBase}/{MediaVariants.FileKey(key, width)} {width}w"));
    }

    private string PublicBase => $"{_options.PublicEndpoint.TrimEnd('/')}/{_options.Bucket}";

    /// <summary>
    /// SDK, ServiceURL http olsa bile imzalı adresi https olarak üretebiliyor;
    /// MinIO yerelde TLS konuşmadığı için bu adres tarayıcıda bağlantı hatasına düşer.
    /// Şemayı yapılandırmadaki değere sabitlemek güvenli: SigV4 imzası host, path,
    /// query ve başlıkları kapsar — şema imzanın parçası değildir, host değişmediği
    /// sürece imza geçerli kalır.
    /// </summary>
    private string MatchEndpointScheme(string url)
    {
        var wantsHttp = _options.PublicEndpoint.StartsWith("http://", StringComparison.OrdinalIgnoreCase);

        if (wantsHttp && url.StartsWith("https://", StringComparison.OrdinalIgnoreCase))
            return string.Concat("http://", url.AsSpan("https://".Length));

        return url;
    }

    /// <summary>
    /// Yüklenen görseli her hedef genişlikte WebP olarak yeniden üretip depoya yazar.
    ///
    /// Kaynak hedeften darsa büyütülmez — büyütmek dosyayı şişirir, kaliteyi
    /// artırmaz. Bu durumda dosya istenen adla ama kaynak boyutunda kaydedilir;
    /// tarayıcı geniş ekranda onu seçerse bugünkü davranışın aynısı olur, yani
    /// bir gerileme yaratmaz.
    /// </summary>
    private async Task WriteVariantsAsync(string sourceKey, string baseKey, CancellationToken cancellationToken)
    {
        using var source = await _internalClient.GetObjectAsync(_options.Bucket, sourceKey, cancellationToken);
        using var buffer = new MemoryStream();
        await source.ResponseStream.CopyToAsync(buffer, cancellationToken);

        buffer.Position = 0;
        using var original = await Image.LoadAsync(buffer, cancellationToken);

        foreach (var width in MediaVariants.Widths)
        {
            // Clone: her boy aynı kaynaktan türesin. Mutate ile küçültseydik
            // sonraki tur zaten küçülmüş görüntüyü tekrar küçültürdü.
            using var resized = original.Width > width
                ? original.Clone(context => context.Resize(new ResizeOptions
                {
                    Size = new Size(width, 0),
                    Mode = ResizeMode.Max
                }))
                : null;

            var image = resized ?? original;

            using var encoded = new MemoryStream();
            await image.SaveAsWebpAsync(encoded, cancellationToken);
            encoded.Position = 0;

            await _internalClient.PutObjectAsync(new PutObjectRequest
            {
                BucketName = _options.Bucket,
                Key = MediaVariants.FileKey(baseKey, width),
                InputStream = encoded,
                ContentType = "image/webp"
            }, cancellationToken);
        }
    }

    /// <summary>İlk baytları okuyup dosyanın gerçekten iddia edilen türde olduğunu doğrular.</summary>
    private async Task<bool> HasMatchingSignatureAsync(string key, string contentType, CancellationToken cancellationToken)
    {
        using var response = await _internalClient.GetObjectAsync(new GetObjectRequest
        {
            BucketName = _options.Bucket,
            Key = key,
            ByteRange = new ByteRange(0, 15),
        }, cancellationToken);

        var buffer = new byte[16];
        var read = await response.ResponseStream.ReadAsync(buffer, cancellationToken);

        return MediaPolicy.MatchesMagicBytes(contentType, buffer.AsSpan(0, read));
    }

    private Task DiscardAsync(string key, CancellationToken cancellationToken) =>
        _internalClient.DeleteObjectAsync(_options.Bucket, key, cancellationToken);

    public void Dispose()
    {
        _internalClient.Dispose();
        _publicClient.Dispose();
    }
}
