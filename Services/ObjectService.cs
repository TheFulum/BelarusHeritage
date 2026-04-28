using BelarusHeritage.Data;
using BelarusHeritage.Models.Domain;
using Microsoft.EntityFrameworkCore;

namespace BelarusHeritage.Services;

public class ObjectService
{
    private readonly AppDbContext _context;

    public ObjectService(AppDbContext context)
    {
        _context = context;
    }

    public async Task<List<HeritageObject>> GetFeaturedObjectsAsync(int count = 8)
    {
        return await _context.HeritageObjects
            .Include(o => o.Category)
            .Include(o => o.Region)
            .Include(o => o.Location)
            .Include(o => o.Images.Where(i => i.IsMain))
            .Where(o => o.IsFeatured && o.Status == ObjectStatus.Published && !o.IsDeleted)
            .OrderByDescending(o => o.RatingAvg)
            .Take(count)
            .ToListAsync();
    }

    public async Task<List<HeritageObject>> GetObjectsAsync(
        int? categoryId = null,
        int? regionId = null,
        int? tagId = null,
        short? centuryStart = null,
        short? centuryEnd = null,
        PreservationStatus? preservationStatus = null,
        bool? hasImages = null,
        bool? visitable = null,
        string sortBy = "name",
        int page = 1,
        int pageSize = 12)
    {
        var query = _context.HeritageObjects
            .Include(o => o.Category)
            .Include(o => o.Region)
            .Include(o => o.Location)
            .Include(o => o.Images.Where(i => i.IsMain))
            .Where(o => o.Status == ObjectStatus.Published && !o.IsDeleted);

        if (categoryId.HasValue)
            query = query.Where(o => o.CategoryId == categoryId.Value);

        if (regionId.HasValue)
            query = query.Where(o => o.RegionId == regionId.Value);

        if (tagId.HasValue)
            query = query.Where(o => o.TagMaps.Any(t => t.TagId == tagId.Value));

        if (centuryStart.HasValue)
            query = query.Where(o => o.CenturyStart >= centuryStart.Value);

        if (centuryEnd.HasValue)
            query = query.Where(o => o.CenturyEnd <= centuryEnd.Value);

        if (preservationStatus.HasValue)
            query = query.Where(o => o.PreservationStatus == preservationStatus.Value);

        if (hasImages == true)
            query = query.Where(o => o.MainImageUrl != null);

        if (visitable == true)
            query = query.Where(o => o.IsVisitable);

        query = sortBy switch
        {
            "rating" => query.OrderByDescending(o => o.RatingAvg),
            "date" => query.OrderByDescending(o => o.CreatedAt),
            "century" => query.OrderBy(o => o.CenturyStart),
            _ => query.OrderBy(o => o.NameRu)
        };

        return await query
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();
    }

    public async Task<int> GetObjectsCountAsync(
        int? categoryId = null,
        int? regionId = null,
        int? tagId = null)
    {
        var query = _context.HeritageObjects
            .Where(o => o.Status == ObjectStatus.Published && !o.IsDeleted);

        if (categoryId.HasValue)
            query = query.Where(o => o.CategoryId == categoryId.Value);

        if (regionId.HasValue)
            query = query.Where(o => o.RegionId == regionId.Value);

        if (tagId.HasValue)
            query = query.Where(o => o.TagMaps.Any(t => t.TagId == tagId.Value));

        return await query.CountAsync();
    }

    public async Task<HeritageObject?> GetObjectBySlugAsync(string slug)
    {
        return await _context.HeritageObjects
            .Include(o => o.Category)
            .Include(o => o.Region)
            .Include(o => o.ArchStyle)
            .Include(o => o.Location)
            .Include(o => o.Images.OrderBy(i => i.SortOrder))
            .Include(o => o.TagMaps).ThenInclude(t => t.Tag)
            .Include(o => o.RelatedFrom).ThenInclude(r => r.Related).ThenInclude(rel => rel!.Category)
            .FirstOrDefaultAsync(o => o.Slug == slug && !o.IsDeleted);
    }

    public async Task<List<HeritageObject>> GetRelatedObjectsAsync(int objectId, int count = 5)
    {
        var relatedIds = await _context.ObjectRelations
            .Where(r => r.ObjectId == objectId)
            .Select(r => r.RelatedId)
            .Take(count)
            .ToListAsync();

        if (relatedIds.Any())
        {
            return await _context.HeritageObjects
                .Include(o => o.Category)
                .Include(o => o.Images.Where(i => i.IsMain))
                .Where(o => relatedIds.Contains(o.Id) && !o.IsDeleted)
                .ToListAsync();
        }

        return new List<HeritageObject>();
    }

    public async Task<List<HeritageObject>> GetNearbyObjectsAsync(int objectId, decimal lat, decimal lng, double radiusKm = 30, int count = 5)
    {
        var objects = await _context.HeritageObjects
            .Include(o => o.Category)
            .Include(o => o.Location)
            .Include(o => o.Images.Where(i => i.IsMain))
            .Where(o => o.Status == ObjectStatus.Published && !o.IsDeleted && o.Id != objectId)
            .ToListAsync();

        return objects
            .Select(o =>
            {
                o.Location = o.Location ?? new ObjectLocation();
                var distance = CalculateHaversineDistance(
                    (double)lat, (double)lng,
                    (double)o.Location.Lat, (double)o.Location.Lng);
                return new { Object = o, Distance = distance };
            })
            .Where(x => x.Distance <= radiusKm)
            .OrderBy(x => x.Distance)
            .Take(count)
            .Select(x => x.Object)
            .ToList();
    }

    private static double CalculateHaversineDistance(double lat1, double lng1, double lat2, double lng2)
    {
        const double earthRadius = 6371;
        var dLat = ToRadians(lat2 - lat1);
        var dLng = ToRadians(lng2 - lng1);
        var a = Math.Sin(dLat / 2) * Math.Sin(dLat / 2) +
                Math.Cos(ToRadians(lat1)) * Math.Cos(ToRadians(lat2)) *
                Math.Sin(dLng / 2) * Math.Sin(dLng / 2);
        var c = 2 * Math.Atan2(Math.Sqrt(a), Math.Sqrt(1 - a));
        return earthRadius * c;
    }

    private static double ToRadians(double degrees) => degrees * Math.PI / 180;

    public async Task<HeritageObject> CreateObjectAsync(HeritageObject obj, int createdByUserId)
    {
        var postedLocation = obj.Location;
        obj.Location = null;

        obj.Slug = GenerateSlug(obj.NameRu);
        obj.CreatedBy = createdByUserId;
        obj.UpdatedBy = createdByUserId;
        obj.CreatedAt = DateTime.UtcNow;
        obj.UpdatedAt = DateTime.UtcNow;

        _context.HeritageObjects.Add(obj);
        await _context.SaveChangesAsync();

        if (postedLocation != null && HasUsableCoordinates(postedLocation))
        {
            _context.ObjectLocations.Add(new ObjectLocation
            {
                ObjectId = obj.Id,
                Lat = postedLocation.Lat,
                Lng = postedLocation.Lng,
                AddressRu = postedLocation.AddressRu,
                AddressBe = postedLocation.AddressBe,
                AddressEn = postedLocation.AddressEn,
                MapZoom = postedLocation.MapZoom == 0 ? (byte)15 : postedLocation.MapZoom
            });
            await _context.SaveChangesAsync();
        }

        return obj;
    }

    public async Task<HeritageObject> UpdateObjectAsync(HeritageObject obj, int updatedByUserId)
    {
        var existing = await _context.HeritageObjects
            .Include(o => o.Location)
            .FirstOrDefaultAsync(o => o.Id == obj.Id);

        if (existing == null)
            throw new InvalidOperationException($"HeritageObject {obj.Id} not found");

        // Copy scalar fields from the posted model (skips navigation properties)
        _context.Entry(existing).CurrentValues.SetValues(obj);
        existing.UpdatedBy = updatedByUserId;
        existing.UpdatedAt = DateTime.UtcNow;

        // Handle 1:1 Location: update if exists, create if not — never let EF
        // try to INSERT a duplicate row for the same ObjectId.
        if (obj.Location != null && HasUsableCoordinates(obj.Location))
        {
            if (existing.Location == null)
            {
                existing.Location = new ObjectLocation
                {
                    ObjectId = existing.Id,
                    Lat = obj.Location.Lat,
                    Lng = obj.Location.Lng,
                    AddressRu = obj.Location.AddressRu,
                    AddressBe = obj.Location.AddressBe,
                    AddressEn = obj.Location.AddressEn,
                    MapZoom = obj.Location.MapZoom == 0 ? (byte)15 : obj.Location.MapZoom
                };
            }
            else
            {
                existing.Location.Lat = obj.Location.Lat;
                existing.Location.Lng = obj.Location.Lng;
                existing.Location.AddressRu = obj.Location.AddressRu;
                existing.Location.AddressBe = obj.Location.AddressBe;
                existing.Location.AddressEn = obj.Location.AddressEn;
                if (obj.Location.MapZoom != 0)
                    existing.Location.MapZoom = obj.Location.MapZoom;
            }
        }

        await _context.SaveChangesAsync();
        return existing;
    }

    private static bool HasUsableCoordinates(ObjectLocation location)
    {
        return !(location.Lat == 0m && location.Lng == 0m);
    }

    public async Task SoftDeleteObjectAsync(int objectId, int deletedByUserId)
    {
        var obj = await _context.HeritageObjects.FindAsync(objectId);
        if (obj != null)
        {
            obj.IsDeleted = true;
            obj.Status = ObjectStatus.Draft;
            obj.UpdatedBy = deletedByUserId;
            obj.UpdatedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();
        }
    }

    public async Task RestoreObjectAsync(int objectId)
    {
        var obj = await _context.HeritageObjects.FindAsync(objectId);
        if (obj != null)
        {
            obj.IsDeleted = false;
            obj.Status = ObjectStatus.Draft;
            obj.UpdatedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();
        }
    }

    private static string GenerateSlug(string name)
    {
        var slug = name.ToLowerInvariant()
            .Replace(" ", "-")
            .Replace("'", "")
            .Replace("\"", "")
            .Replace("?", "")
            .Replace("!", "");

        // Replace Cyrillic with Latin (basic)
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
}
