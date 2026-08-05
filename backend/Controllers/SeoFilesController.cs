using System.Security;
using System.Text;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Theatre.Api.Data;
using Theatre.Api.Models;

namespace Theatre.Api.Controllers;

[ApiController, Route("")]
public sealed class SeoFilesController(AppDbContext db, IConfiguration configuration) : ControllerBase
{
    [HttpGet("robots.txt")]
    [ResponseCache(Duration = 3600)]
    public ContentResult Robots()
    {
        var root = SiteRoot();
        var basePath = configuration["PublicSite:BasePath"]?.TrimEnd('/') ?? "/Teatri_AAB_Faruk_Begolli";
        return Content($"User-agent: *\nAllow: /\nDisallow: {basePath}/admin\nSitemap: {root}/sitemap.xml\n", "text/plain", Encoding.UTF8);
    }

    [HttpGet("sitemap.xml")]
    [ResponseCache(Duration = 900)]
    public async Task<ContentResult> Sitemap(CancellationToken token)
    {
        var root = SiteRoot();
        var urls = new List<(string Path, DateTimeOffset? Updated)>();
        foreach (var language in new[] { "sq", "en" })
        {
            var prefix = language == "sq" ? "/sq" : "/en";
            urls.AddRange(new[] { "", language == "sq" ? "/per-ne" : "/about", language == "sq" ? "/shfaqjet" : "/shows", language == "sq" ? "/lajme" : "/news", "/pitf", language == "sq" ? "/galeria" : "/gallery", language == "sq" ? "/kontakti" : "/contact", language == "sq" ? "/rezervo" : "/reserve" }.Select(path => (prefix + path, (DateTimeOffset?)null)));
        }

        var shows = await db.Shows.AsNoTracking().Where(x => x.Status == ShowStatus.Published)
            .SelectMany(x => x.Translations.Select(t => new { Language = t.Language.Code, t.Slug, x.UpdatedAt })).ToListAsync(token);
        urls.AddRange(shows.Select(x => ($"/{x.Language}/{(x.Language == "sq" ? "shfaqjet" : "shows")}/{x.Slug}", (DateTimeOffset?)x.UpdatedAt)));
        var news = await db.NewsArticles.AsNoTracking().Where(x => x.IsPublished)
            .SelectMany(x => x.Translations.Select(t => new { Language = t.Language.Code, t.Slug, x.UpdatedAt })).ToListAsync(token);
        urls.AddRange(news.Select(x => ($"/{x.Language}/{(x.Language == "sq" ? "lajme" : "news")}/{x.Slug}", (DateTimeOffset?)x.UpdatedAt)));

        var xml = new StringBuilder("<?xml version=\"1.0\" encoding=\"UTF-8\"?>\n<urlset xmlns=\"http://www.sitemaps.org/schemas/sitemap/0.9\">\n");
        foreach (var item in urls.DistinctBy(x => x.Path))
        {
            xml.Append("  <url><loc>").Append(SecurityElement.Escape(root + item.Path)).Append("</loc>");
            if (item.Updated.HasValue) xml.Append("<lastmod>").Append(item.Updated.Value.UtcDateTime.ToString("yyyy-MM-dd")).Append("</lastmod>");
            xml.Append("</url>\n");
        }
        xml.Append("</urlset>");
        return Content(xml.ToString(), "application/xml", Encoding.UTF8);
    }

    private string SiteRoot()
    {
        var configured = configuration["PublicSite:BaseUrl"]?.TrimEnd('/');
        if (!string.IsNullOrWhiteSpace(configured)) return configured;
        var basePath = configuration["PublicSite:BasePath"]?.TrimEnd('/') ?? "/Teatri_AAB_Faruk_Begolli";
        return $"{Request.Scheme}://{Request.Host}{basePath}";
    }
}
