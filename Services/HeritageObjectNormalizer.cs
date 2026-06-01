using System.Text.RegularExpressions;
using BelarusHeritage.Models.Domain;

namespace BelarusHeritage.Services;

public static class HeritageObjectNormalizer
{
    public static void Normalize(HeritageObject obj)
    {
        obj.NameRu = Trim(obj.NameRu);
        obj.NameBe = Trim(obj.NameBe);
        obj.NameEn = Trim(obj.NameEn);

        if (string.IsNullOrEmpty(obj.NameBe))
            obj.NameBe = obj.NameRu;
        if (string.IsNullOrEmpty(obj.NameEn))
            obj.NameEn = obj.NameRu;

        obj.Slug = Trim(obj.Slug).ToLowerInvariant();
        if (string.IsNullOrEmpty(obj.Slug) && !string.IsNullOrEmpty(obj.NameRu))
            obj.Slug = GenerateSlug(obj.NameRu);

        obj.Slug = Regex.Replace(obj.Slug, @"[^a-z0-9\-]", "-");
        obj.Slug = Regex.Replace(obj.Slug, @"-+", "-").Trim('-');
        if (obj.Slug.Length > 160)
            obj.Slug = obj.Slug[..160].TrimEnd('-');

        obj.DescriptionRu = NullIfWhiteSpace(obj.DescriptionRu);
        obj.DescriptionBe = NullIfWhiteSpace(obj.DescriptionBe);
        obj.DescriptionEn = NullIfWhiteSpace(obj.DescriptionEn);
        obj.ShortDescRu = NullIfWhiteSpace(obj.ShortDescRu);
        obj.ShortDescBe = NullIfWhiteSpace(obj.ShortDescBe);
        obj.ShortDescEn = NullIfWhiteSpace(obj.ShortDescEn);
        obj.FunFactRu = NullIfWhiteSpace(obj.FunFactRu);
        obj.FunFactBe = NullIfWhiteSpace(obj.FunFactBe);
        obj.FunFactEn = NullIfWhiteSpace(obj.FunFactEn);
        obj.Architect = NullIfWhiteSpace(obj.Architect);
        obj.VisitingHours = NullIfWhiteSpace(obj.VisitingHours);
        obj.EntryFee = NullIfWhiteSpace(obj.EntryFee);

        var shortBe = obj.ShortDescBe;
        var shortEn = obj.ShortDescEn;
        FillOptionalLocale(obj.ShortDescRu, ref shortBe, ref shortEn);
        obj.ShortDescBe = shortBe;
        obj.ShortDescEn = shortEn;

        var factBe = obj.FunFactBe;
        var factEn = obj.FunFactEn;
        FillOptionalLocale(obj.FunFactRu, ref factBe, ref factEn);
        obj.FunFactBe = factBe;
        obj.FunFactEn = factEn;

        if (obj.Location != null)
            NormalizeLocation(obj.Location);
    }

    public static void ApplyPosted(HeritageObject existing, HeritageObject posted)
    {
        existing.Slug = posted.Slug;
        existing.NameRu = posted.NameRu;
        existing.NameBe = posted.NameBe;
        existing.NameEn = posted.NameEn;
        existing.CategoryId = posted.CategoryId;
        existing.RegionId = posted.RegionId;
        existing.ArchStyleId = posted.ArchStyleId;
        existing.CenturyStart = posted.CenturyStart;
        existing.CenturyEnd = posted.CenturyEnd;
        existing.BuildYear = posted.BuildYear;
        existing.DescriptionRu = posted.DescriptionRu;
        existing.DescriptionEn = posted.DescriptionEn;
        existing.ShortDescRu = posted.ShortDescRu;
        existing.ShortDescBe = posted.ShortDescBe;
        existing.ShortDescEn = posted.ShortDescEn;
        existing.FunFactRu = posted.FunFactRu;
        existing.FunFactBe = posted.FunFactBe;
        existing.FunFactEn = posted.FunFactEn;
        existing.Architect = posted.Architect;
        existing.HeritageCategory = posted.HeritageCategory;
        existing.HeritageYear = posted.HeritageYear;
        existing.PreservationStatus = posted.PreservationStatus;
        existing.IsVisitable = posted.IsVisitable;
        existing.VisitingHours = posted.VisitingHours;
        existing.EntryFee = posted.EntryFee;
        existing.Status = posted.Status;
        existing.IsFeatured = posted.IsFeatured;
    }

    public static void NormalizeLocation(ObjectLocation location)
    {
        location.AddressRu = NullIfWhiteSpace(location.AddressRu);
        location.AddressBe = NullIfWhiteSpace(location.AddressBe);
        location.AddressEn = NullIfWhiteSpace(location.AddressEn);

        if (!string.IsNullOrEmpty(location.AddressRu))
        {
            if (string.IsNullOrEmpty(location.AddressBe))
                location.AddressBe = location.AddressRu;
            if (string.IsNullOrEmpty(location.AddressEn))
                location.AddressEn = location.AddressRu;
        }
    }

    public static string GenerateSlug(string name)
    {
        var slug = name.ToLowerInvariant()
            .Replace(" ", "-")
            .Replace("'", "")
            .Replace("\"", "")
            .Replace("?", "")
            .Replace("!", "");

        slug = slug.Replace("а", "a").Replace("б", "b").Replace("в", "v")
            .Replace("г", "g").Replace("д", "d").Replace("е", "e")
            .Replace("ё", "yo").Replace("ж", "zh").Replace("з", "z")
            .Replace("и", "i").Replace("й", "y").Replace("к", "k")
            .Replace("л", "l").Replace("м", "m").Replace("н", "n")
            .Replace("о", "o").Replace("п", "p").Replace("р", "r")
            .Replace("с", "s").Replace("т", "t").Replace("у", "u")
            .Replace("ф", "f").Replace("х", "kh").Replace("ц", "ts")
            .Replace("ч", "ch").Replace("ш", "sh").Replace("щ", "sch")
            .Replace("ъ", "").Replace("ы", "y").Replace("ь", "")
            .Replace("э", "e").Replace("ю", "yu").Replace("я", "ya");

        return slug;
    }

    private static void FillOptionalLocale(string? source, ref string? be, ref string? en)
    {
        if (string.IsNullOrWhiteSpace(source))
            return;

        if (string.IsNullOrWhiteSpace(be))
            be = source.Trim();
        if (string.IsNullOrWhiteSpace(en))
            en = source.Trim();
    }

    private static string Trim(string? value) => (value ?? "").Trim();

    private static string? NullIfWhiteSpace(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return null;
        return value.Trim();
    }
}
