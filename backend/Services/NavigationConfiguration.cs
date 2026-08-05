using System.Text.Json;
using Theatre.Api.DTOs;

namespace Theatre.Api.Services;

public static class NavigationConfiguration
{
    public static readonly string[] RouteKeys = ["home", "about", "shows", "news", "pitf", "gallery", "location", "contact"];
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    public static NavigationConfigurationDto Default() => new(
        RouteKeys.Select((key, index) => new NavigationItemDto(key, index, key != "location", key is "home" or "about" or "shows" or "news" or "gallery" or "location")).ToList(),
        [
            new("sq", new() { ["home"] = "Ballina", ["about"] = "Për Ne", ["shows"] = "Shfaqjet", ["news"] = "Lajme", ["pitf"] = "PITF", ["gallery"] = "Galeria", ["location"] = "Lokacioni", ["contact"] = "Kontakti" }, "Rezervo Tani", "Linqe", "Vizito", "Na Ndiq", "Newsletter", "Njoftohu i pari për shfaqjet më të fundit", "Lokacioni"),
            new("en", new() { ["home"] = "Home", ["about"] = "About", ["shows"] = "Shows", ["news"] = "News", ["pitf"] = "PITF", ["gallery"] = "Gallery", ["location"] = "Location", ["contact"] = "Contact" }, "Reserve Now", "Links", "Visit", "Follow Us", "Newsletter", "Be the first to hear about our latest performances", "Location")
        ]);

    public static NavigationConfigurationDto Read(string? json)
    {
        if (string.IsNullOrWhiteSpace(json)) return Default();
        try
        {
            var saved = JsonSerializer.Deserialize<NavigationConfigurationDto>(json, JsonOptions);
            if (saved is null) return Default();
            var defaults = Default();
            var items = RouteKeys.Select((key, index) =>
                saved.Items.FirstOrDefault(x => x.RouteKey == key)
                ?? defaults.Items.First(x => x.RouteKey == key) with { SortOrder = index }).ToList();
            var translations = defaults.Translations.Select(fallback =>
            {
                var existing = saved.Translations.FirstOrDefault(x => x.LanguageCode == fallback.LanguageCode);
                if (existing is null) return fallback;
                var labels = new Dictionary<string, string>(fallback.Labels);
                foreach (var label in existing.Labels) labels[label.Key] = label.Value;
                return existing with { Labels = labels };
            }).ToList();
            return new(items, translations);
        }
        catch (JsonException) { return Default(); }
    }

    public static string Write(NavigationConfigurationDto value) => JsonSerializer.Serialize(value, JsonOptions);
}
