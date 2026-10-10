namespace NewsService.Application.Features.Queries.Article;

public class FeedOptions
{
    public const string SectionName = "Feed";

    // Yalnızca herkese açık akışa uygulanır; 0 pencereyi kapatır.
    public int FreshnessDays { get; set; } = 7;
}
