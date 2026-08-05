namespace Theatre.Api.Services;

public static class MediaStoragePath
{
    public static bool Exists(IWebHostEnvironment environment, string fileUrl)
    {
        if (!TryResolve(environment, fileUrl, out var path)) return true;
        return File.Exists(path);
    }

    public static bool TryResolve(IWebHostEnvironment environment, string fileUrl, out string path)
    {
        path = string.Empty;
        var cleanUrl = Uri.UnescapeDataString(fileUrl.Split('?', '#')[0]).Replace('\\', '/');
        var uploadIndex = cleanUrl.IndexOf("/uploads/", StringComparison.OrdinalIgnoreCase);
        if (uploadIndex < 0) return false;
        var relative = cleanUrl[(uploadIndex + 1)..].Replace('/', Path.DirectorySeparatorChar);
        var webRoot = Path.GetFullPath(environment.WebRootPath ?? Path.Combine(environment.ContentRootPath, "wwwroot"));
        var candidate = Path.GetFullPath(Path.Combine(webRoot, relative));
        if (!candidate.StartsWith(webRoot + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase)) return false;
        path = candidate;
        return true;
    }
}
