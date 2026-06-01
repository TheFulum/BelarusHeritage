namespace BelarusHeritage.Services;

/// <summary>
/// Shared RU → BE/EN fallback for catalog entities and timeline events.
/// </summary>
public static class LocalizedNameNormalizer
{
    public static void FillNameLocales(ref string nameRu, ref string nameBe, ref string nameEn)
    {
        nameRu = (nameRu ?? "").Trim();
        nameBe = (nameBe ?? "").Trim();
        nameEn = (nameEn ?? "").Trim();

        if (string.IsNullOrEmpty(nameBe))
            nameBe = nameRu;
        if (string.IsNullOrEmpty(nameEn))
            nameEn = nameRu;
    }

    public static void FillSlug(ref string slug, string nameRu, int maxLength = 80)
    {
        slug = (slug ?? "").Trim().ToLowerInvariant();
        if (string.IsNullOrEmpty(slug) && !string.IsNullOrEmpty(nameRu))
            slug = HeritageObjectNormalizer.GenerateSlug(nameRu);

        if (slug.Length > maxLength)
            slug = slug[..maxLength].TrimEnd('-');
    }
}
