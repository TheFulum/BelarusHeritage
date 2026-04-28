using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Processing;

namespace BelarusHeritage.Services;

public class FileService
{
    private readonly IWebHostEnvironment _env;
    private readonly long _maxFileSize;

    public FileService(IWebHostEnvironment env, IConfiguration configuration)
    {
        _env = env;
        var maxMb = configuration.GetSection("App:MaxUploadSizeMb").Get<int?>() ?? 10;
        _maxFileSize = maxMb * 1024 * 1024;
    }

    public async Task<FileUploadResult> UploadImageAsync(IFormFile file, string folder, int? objectId = null)
    {
        if (file == null || file.Length == 0)
            throw new ArgumentException("No file provided");

        if (file.Length > _maxFileSize)
            throw new ArgumentException($"File size exceeds {_maxFileSize / 1024 / 1024}MB limit");

        var allowedExtensions = new[] { ".jpg", ".jpeg", ".png", ".webp" };
        var extension = Path.GetExtension(file.FileName).ToLower();
        if (!allowedExtensions.Contains(extension))
            throw new ArgumentException("Invalid file type. Allowed: jpg, jpeg, png, webp");

        var uploadPath = Path.Combine(_env.WebRootPath, "uploads", folder);
        if (objectId.HasValue)
            uploadPath = Path.Combine(uploadPath, objectId.Value.ToString());

        Directory.CreateDirectory(uploadPath);

        var fileName = $"{Guid.NewGuid()}{extension}";
        var filePath = Path.Combine(uploadPath, fileName);
        var relativePath = Path.Combine("uploads", folder, fileName);
        if (objectId.HasValue)
            relativePath = Path.Combine("uploads", folder, objectId.Value.ToString(), fileName);

        // Save original
        using (var stream = new FileStream(filePath, FileMode.Create))
        {
            await file.CopyToAsync(stream);
        }

        // Create thumbnail (400px max)
        var thumbPath = Path.Combine(uploadPath, $"thumb_{fileName}");
        await CreateThumbnailAsync(filePath, thumbPath, 400);

        var normalizedRelativePath = relativePath.Replace(Path.DirectorySeparatorChar, '/');
        var slashIndex = normalizedRelativePath.LastIndexOf('/');
        var thumbRelativePath = slashIndex >= 0
            ? normalizedRelativePath.Insert(slashIndex + 1, "thumb_")
            : $"thumb_{normalizedRelativePath}";

        return new FileUploadResult
        {
            FileName = fileName,
            Url = normalizedRelativePath,
            ThumbUrl = thumbRelativePath,
            OriginalPath = filePath,
            ThumbPath = thumbPath
        };
    }

    public async Task CreateThumbnailAsync(string originalPath, string thumbPath, int maxWidth)
    {
        using var image = await Image.LoadAsync(originalPath);

        if (image.Width > maxWidth)
        {
            var ratio = (double)maxWidth / image.Width;
            var newHeight = (int)(image.Height * ratio);
            image.Mutate(x => x.Resize(maxWidth, newHeight));
        }

        await image.SaveAsync(thumbPath);
    }

    public async Task<string> ResizeAndSaveImageAsync(IFormFile file, string folder, int objectId, int maxWidth = 1920)
    {
        var result = await UploadImageAsync(file, folder, objectId);

        var resizedPath = Path.Combine(_env.WebRootPath, result.Url.Replace('/', Path.DirectorySeparatorChar));
        using var image = await Image.LoadAsync(resizedPath);

        if (image.Width > maxWidth)
        {
            var ratio = (double)maxWidth / image.Width;
            var newHeight = (int)(image.Height * ratio);
            image.Mutate(x => x.Resize(maxWidth, newHeight));
            await image.SaveAsync(resizedPath);
        }

        return result.Url;
    }

    public bool DeleteFile(string relativePath)
    {
        try
        {
            var filePath = Path.Combine(_env.WebRootPath, relativePath.Replace('/', Path.DirectorySeparatorChar));
            if (File.Exists(filePath))
            {
                File.Delete(filePath);
                // Try to delete thumb
                var thumbPath = filePath.Insert(filePath.LastIndexOf('.'), "_thumb");
                if (File.Exists(thumbPath))
                    File.Delete(thumbPath);
                return true;
            }
            return false;
        }
        catch
        {
            return false;
        }
    }

    public string GetOrnamentImage(string dataUrl)
    {
        var folder = "ornaments";
        var uploadPath = Path.Combine(_env.WebRootPath, "uploads", folder);
        Directory.CreateDirectory(uploadPath);

        var fileName = $"{Guid.NewGuid()}.png";
        var filePath = Path.Combine(uploadPath, fileName);

        // Remove data URL prefix
        if (dataUrl.Contains(','))
            dataUrl = dataUrl.Substring(dataUrl.IndexOf(',') + 1);

        var bytes = Convert.FromBase64String(dataUrl);
        File.WriteAllBytes(filePath, bytes);

        return $"uploads/{folder}/{fileName}";
    }
}

public class FileUploadResult
{
    public string FileName { get; set; } = string.Empty;
    public string Url { get; set; } = string.Empty;
    public string? ThumbUrl { get; set; }
    public string OriginalPath { get; set; } = string.Empty;
    public string? ThumbPath { get; set; }
}
