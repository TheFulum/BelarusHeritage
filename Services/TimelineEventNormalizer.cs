using BelarusHeritage.Models.Domain;

namespace BelarusHeritage.Services;

public static class TimelineEventNormalizer
{
    public static void Normalize(TimelineEvent evt)
    {
        evt.TitleRu = CoalesceTitle(evt.TitleRu);
        evt.TitleBe = CoalesceTitle(evt.TitleBe);
        evt.TitleEn = CoalesceTitle(evt.TitleEn);

        if (string.IsNullOrEmpty(evt.TitleBe))
            evt.TitleBe = evt.TitleRu;
        if (string.IsNullOrEmpty(evt.TitleEn))
            evt.TitleEn = evt.TitleRu;

        // DB columns are NOT NULL — never persist null.
        evt.TitleRu ??= string.Empty;
        evt.TitleBe ??= evt.TitleRu;
        evt.TitleEn ??= evt.TitleRu;

        evt.BodyRu = NullIfWhiteSpace(evt.BodyRu);
        evt.BodyEn = NullIfWhiteSpace(evt.BodyEn);
    }

    public static void ApplyPosted(TimelineEvent existing, TimelineEvent posted)
    {
        Normalize(posted);

        existing.Year = posted.Year;
        existing.ObjectId = posted.ObjectId;
        existing.TitleRu = posted.TitleRu;
        existing.TitleBe = posted.TitleBe;
        existing.TitleEn = posted.TitleEn;
        existing.BodyRu = posted.BodyRu;
        existing.BodyEn = posted.BodyEn;
        existing.IsPublished = posted.IsPublished;
        existing.SortOrder = posted.SortOrder;
        if (!string.IsNullOrWhiteSpace(posted.ImageUrl))
            existing.ImageUrl = posted.ImageUrl;
    }

    private static string CoalesceTitle(string? value) =>
        string.IsNullOrWhiteSpace(value) ? string.Empty : value.Trim();

    private static string? NullIfWhiteSpace(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}
