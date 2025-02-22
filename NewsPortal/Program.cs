using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Distributed;
using NewsPortal.Data;
using NewsPortal.Dto;
using NewsPortal.Models;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddDbContext<NewsPortalContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection")));

builder.Services.AddStackExchangeRedisCache(options =>
{
    options.Configuration = builder.Configuration.GetConnectionString("Redis");
    options.InstanceName = "NewsPortal_";
});

var app = builder.Build();

app.MapGet("/", async  (NewsPortalContext context) =>
{
    var articles = await context.Articles.CountAsync(); 
    var comments = await context.Comments.CountAsync();

    return $"Total count: Articles - {articles}; Comments - {comments}";
});

app.MapGet("/articles", async (NewsPortalContext context) =>
{
    var articles = await context.Articles
        .Include(a => a.Comments)
        .Where(a => a.Comments.Average(c => c.Rating) > 50)
        .Select(a => new
        {
            a.Id,
            a.Title,
            a.Content,
            Rating = a.Comments.Average(c => c.Rating)
        })
        .ToListAsync();

    return Results.Ok(articles);
});

app.MapPost("/articles", async (ArticleRequest request, NewsPortalContext context) =>
{
    var article = new Article
    {
        Title = request.Title,
        Content = request.Content
    };

    context.Articles.Add(article);
    await context.SaveChangesAsync();

    return Results.Created($"/articles/{article.Id}", article);
});

app.MapGet("/articles/{id:int}", async (int id, NewsPortalContext context) =>
{
    var article = await context.Articles
        .Include(a => a.Comments)
        .Where(a => a.Id == id)
        .Select(a => new
        {
            a.Id,
            a.Title,
            a.Content,
            Rating = a.Comments.Average(c => c.Rating)
        })
        .FirstOrDefaultAsync();

    return article == null ? Results.NotFound() : Results.Ok(article);
});

app.MapGet("/articles/trending", async (NewsPortalContext context, IDistributedCache cache) =>
{
    const string cacheKey = "trending_article";
    var cachedArticle = await cache.GetStringAsync(cacheKey);

    if (cachedArticle != null)
    {
        return Results.Ok(System.Text.Json.JsonSerializer.Deserialize<object>(cachedArticle));
    }

    var topArticles = await context.Articles
        .Include(a => a.Comments)
        .OrderByDescending(a => a.Comments.Average(c => c.Rating))
        .Take(10)
        .Select(a => new
        {
            a.Id,
            a.Title,
            a.Content,
            Rating = a.Comments.Average(c => c.Rating)
        })
        .ToListAsync();

    if (topArticles.Count == 0)
    {
        return Results.NotFound("No articles found.");
    }

    var random = new Random();
    var trendingArticle = topArticles[random.Next(topArticles.Count)];
 
    var cacheOptions = new DistributedCacheEntryOptions
    {
        AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(1)
    };

    await cache.SetStringAsync(cacheKey, System.Text.Json.JsonSerializer.Serialize(trendingArticle), cacheOptions);

    return Results.Ok(trendingArticle);
});


app.Run();
