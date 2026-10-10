using Shared.Exceptions;

namespace NewsService.Application.Exceptions;

public static class NewsErrors
{
    extension(NotFoundException)
    {
        public static NotFoundException Article(object id) => new(
            ErrorCodes.Article.NotFound,
            "Makale bulunamadı.",
            $"'{id}' ID'sine sahip makale sistemde mevcut değil veya silinmiş olabilir.");

        public static NotFoundException Category(object id) => new(
            ErrorCodes.Category.NotFound,
            "Kategori bulunamadı.",
            $"'{id}' ID'sine sahip kategori sistemde mevcut değil.");

        public static NotFoundException Tag(object id) => new(
            ErrorCodes.Tag.NotFound,
            "Etiket bulunamadı.",
            $"'{id}' ID'sine sahip etiket sistemde mevcut değil.");
    }

    extension(ConflictException)
    {
        public static ConflictException ArticleAlreadyPublished(object id) => new(
            ErrorCodes.Article.AlreadyPublished,
            "Makale zaten yayınlanmış.",
            $"'{id}' ID'li makale daha önce yayınlanmış, tekrar yayınlanamaz.");

        public static ConflictException CategoryAlreadyExists(string name) => new(
            ErrorCodes.Category.AlreadyExists,
            "Bu adda bir kategori zaten var.",
            $"'{name}' adlı kategori mevcut. Farklı bir ad seçin.");

        public static ConflictException CategoryInUse(string name, int articleCount) => new(
            ErrorCodes.Category.InUse,
            "Bu kategori silinemez.",
            $"'{name}' kategorisine bağlı {articleCount} haber var. " +
            "Silmeden önce bu haberleri başka bir kategoriye taşıyın.");

        public static ConflictException TagAlreadyExists(string name) => new(
            ErrorCodes.Tag.AlreadyExists,
            "Bu adda bir etiket zaten var.",
            $"'{name}' adlı etiket mevcut. Farklı bir ad seçin.");
    }

    extension(ForbiddenException)
    {
        public static ForbiddenException NotArticleAuthor() => new(
            ErrorCodes.Auth.Forbidden,
            "Bu haberi düzenleyemezsiniz.",
            "Editörler yalnızca kendi yazdıkları haberleri düzenleyebilir. " +
            "Başka bir yazarın haberinde değişiklik yapılması gerekiyorsa bir yöneticiye başvurun.");

        public static ForbiddenException ArticleAlreadyPublished() => new(
            ErrorCodes.Auth.Forbidden,
            "Yayındaki haber düzenlenemez.",
            "Haber yayına alındıktan sonra içeriğini yalnızca yöneticiler değiştirebilir. " +
            "Bir düzeltme gerekiyorsa bir yöneticiye başvurun.");
    }
}
