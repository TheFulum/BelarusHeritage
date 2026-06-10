using BelarusHeritage.Models.Domain;

namespace BelarusHeritage.Utilities;

public static class HeritageImageResolver
{
    public static string? ResolveTimelineEventImage(TimelineEvent evt, bool preferThumbnail = true)
    {
        if (!string.IsNullOrWhiteSpace(evt.ImageUrl))
        {
            var resolved = preferThumbnail
                ? ResolveThumbDisplayUrl(null, evt.ImageUrl)
                : ResolveDisplayUrl(evt.ImageUrl, null);
            return string.IsNullOrWhiteSpace(resolved) ? null : resolved;
        }

        return ResolveObjectImage(evt.Object, preferThumbnail);
    }

    public static string? ResolveObjectImage(HeritageObject? obj, bool preferThumbnail = true)
    {
        if (obj == null)
            return null;

        var image = obj.Images?.FirstOrDefault(i => i.IsMain)
                    ?? obj.Images?.OrderBy(i => i.SortOrder).ThenBy(i => i.Id).FirstOrDefault();

        if (image != null)
        {
            var resolved = preferThumbnail
                ? ResolveThumbDisplayUrl(image.Url, image.ThumbUrl)
                : ResolveDisplayUrl(image.Url, image.ThumbUrl);
            if (!string.IsNullOrWhiteSpace(resolved))
                return resolved;
        }

        if (string.IsNullOrWhiteSpace(obj.MainImageUrl))
            return null;

        var main = NormalizeImageUrl(obj.MainImageUrl);
        return string.IsNullOrWhiteSpace(main) ? null : main;
    }

    public static string ResolveThumbDisplayUrl(string? url, string? thumbUrl)
    {
        if (!string.IsNullOrWhiteSpace(thumbUrl))
        {
            var thumb = NormalizeImageUrl(thumbUrl);
            if (!string.IsNullOrWhiteSpace(thumb))
                return thumb;
        }

        return ResolveDisplayUrl(url, null);
    }

    public static string ResolveDisplayUrl(string? url, string? thumbUrl)
    {
        if (!string.IsNullOrWhiteSpace(url) && !IsLegacyThumbPath(url))
            return NormalizeImageUrl(url);

        if (!string.IsNullOrWhiteSpace(thumbUrl) && !IsLegacyThumbPath(thumbUrl))
            return NormalizeImageUrl(thumbUrl);

        if (!string.IsNullOrWhiteSpace(url))
            return NormalizeImageUrl(BuildFullFromLegacyThumb(url));

        if (!string.IsNullOrWhiteSpace(thumbUrl))
            return NormalizeImageUrl(BuildFullFromLegacyThumb(thumbUrl));

        return "";
    }

    public static string NormalizeImageUrl(string? url)
    {
        if (string.IsNullOrWhiteSpace(url))
            return "";

        url = url.Replace('\\', '/');

        if (url.StartsWith("thumb_uploads/", StringComparison.OrdinalIgnoreCase))
        {
            var rest = url.Substring("thumb_uploads/".Length);
            var idx = rest.LastIndexOf('/');
            if (idx >= 0)
            {
                var dir = rest.Substring(0, idx);
                var file = rest.Substring(idx + 1);
                url = $"uploads/{dir}/thumb_{file}";
            }
        }
        else if (url.StartsWith("/thumb_uploads/", StringComparison.OrdinalIgnoreCase))
        {
            var rest = url.Substring("/thumb_uploads/".Length);
            var idx = rest.LastIndexOf('/');
            if (idx >= 0)
            {
                var dir = rest.Substring(0, idx);
                var file = rest.Substring(idx + 1);
                url = $"/uploads/{dir}/thumb_{file}";
            }
        }

        if (url.StartsWith("http://", StringComparison.OrdinalIgnoreCase) ||
            url.StartsWith("https://", StringComparison.OrdinalIgnoreCase) ||
            url.StartsWith("/", StringComparison.Ordinal))
            return url;

        return "/" + url;
    }

    private static bool IsLegacyThumbPath(string value)
    {
        var s = value.Replace('\\', '/');
        return s.Contains("/thumb_", StringComparison.OrdinalIgnoreCase)
               || s.StartsWith("thumb_uploads/", StringComparison.OrdinalIgnoreCase)
               || s.StartsWith("/thumb_uploads/", StringComparison.OrdinalIgnoreCase);
    }

    private static string BuildFullFromLegacyThumb(string value)
    {
        var s = value.Replace('\\', '/');

        if (s.StartsWith("thumb_uploads/", StringComparison.OrdinalIgnoreCase))
            s = "uploads/" + s.Substring("thumb_uploads/".Length);
        else if (s.StartsWith("/thumb_uploads/", StringComparison.OrdinalIgnoreCase))
            s = "/uploads/" + s.Substring("/thumb_uploads/".Length);

        var slash = s.LastIndexOf('/');
        if (slash >= 0)
        {
            var dir = s.Substring(0, slash + 1);
            var file = s.Substring(slash + 1);
            if (file.StartsWith("thumb_", StringComparison.OrdinalIgnoreCase))
                s = dir + file.Substring("thumb_".Length);
        }

        return s;
    }
}
