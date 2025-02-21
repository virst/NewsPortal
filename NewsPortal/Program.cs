using Microsoft.EntityFrameworkCore;
using NewsPortal.Data;

var builder = WebApplication.CreateBuilder(args);
var app = builder.Build();

builder.Services.AddDbContext<NewsPortalContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection")));

app.MapGet("/", () => "Hello World!");

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

app.MapGet("/articles/{id}", async (int id, NewsPortalContext context) =>
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

app.MapGet("/articles/trending", async (NewsPortalContext context) =>
{
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

    var random = new Random();
    var trendingArticle = topArticles[random.Next(topArticles.Count)];

    return Results.Ok(trendingArticle);
}); 


app.Run();
