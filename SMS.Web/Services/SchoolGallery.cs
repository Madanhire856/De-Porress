using System.Buffers.Binary;
using System.Text.Json;
using SMS.Lib;

namespace SMS.Web.Services;

public sealed record GalleryPhoto
{
    public Guid Id { get; init; }
    public string Caption { get; init; } = "";
    public string Album { get; init; } = "School life";
    public int Order { get; init; }
    public bool Published { get; init; }
    public bool Archived { get; init; }
    public string ContentType { get; init; } = "image/jpeg";
    public string ThumbnailType { get; init; } = "image/jpeg";
    public DateTimeOffset CreatedAt { get; init; }
    public DateTimeOffset UpdatedAt { get; init; }
    public Guid? UpdatedBy { get; init; }
}

public sealed record GalleryImage(byte[] Bytes, string ContentType);
public sealed class GalleryValidationException(string message) : Exception(message);

public interface IGalleryStore
{
    Task<List<GalleryPhoto>> ListAsync(bool includeDrafts, CancellationToken ct = default);
    Task<GalleryPhoto?> FindAsync(Guid id, bool includeDrafts, CancellationToken ct = default);
    Task<GalleryPhoto> CreateAsync(GalleryPhoto photo, IFormFile image, IFormFile? thumbnail, CancellationToken ct = default);
    Task<bool> UpdateAsync(Guid id, Func<GalleryPhoto, GalleryPhoto> update, CancellationToken ct = default);
    Task<GalleryImage?> ImageAsync(Guid id, bool thumbnail, bool includeDrafts, CancellationToken ct = default);
}

public sealed class GalleryPermissionException : Exception { }

public static class SchoolGallery
{
    public static bool CanManage(WorkspaceAccess access) => access.CanOpen("Config", "/Gallery/Index");
    public static bool CanCreate(WorkspaceAccess access) => CanManage(access) && access.Can(FeatureRights.CreateGalleryPhotos);
    public static bool CanEdit(WorkspaceAccess access) => CanManage(access) && access.Can(FeatureRights.EditGalleryPhotos);
    public static bool CanPublish(WorkspaceAccess access) => CanManage(access) && access.Can(FeatureRights.PublishGalleryPhotos);
    public static bool CanArchive(WorkspaceAccess access) => CanManage(access) && access.Can(FeatureRights.ArchiveGalleryPhotos);
    public const int MaximumImageBytes = 2 * 1024 * 1024;
    public const int MaximumThumbnailBytes = 256 * 1024;

    public static string ValidateImage(byte[] data, bool thumbnail = false)
    {
        if (data.Length == 0 || data.Length > (thumbnail ? MaximumThumbnailBytes : MaximumImageBytes))
            throw new GalleryValidationException(thumbnail ? "The thumbnail is too large." : "Choose a photo under 2 MB. Large photos can be resized automatically in your browser.");
        int width = 0, height = 0;
        string type;
        if (data.Length >= 33 && data.AsSpan(0, 8).SequenceEqual(new byte[] { 137, 80, 78, 71, 13, 10, 26, 10 })
            && data.AsSpan(12, 4).SequenceEqual("IHDR"u8)
            && data.AsSpan(data.Length - 8, 4).SequenceEqual("IEND"u8))
        {
            width = BinaryPrimitives.ReadInt32BigEndian(data.AsSpan(16, 4));
            height = BinaryPrimitives.ReadInt32BigEndian(data.AsSpan(20, 4));
            type = "image/png";
        }
        else if (data.Length >= 4 && data[0] == 255 && data[1] == 216 && data[^2] == 255 && data[^1] == 217)
        {
            type = "image/jpeg";
            int offset = 2;
            while (offset + 3 < data.Length)
            {
                if (data[offset++] != 255) break;
                while (offset < data.Length && data[offset] == 255) offset++;
                if (offset >= data.Length) break;
                int marker = data[offset++];
                if (marker == 218 || marker == 217) break;
                if (marker == 1 || marker is >= 208 and <= 215) continue;
                if (offset + 2 > data.Length) break;
                int length = BinaryPrimitives.ReadUInt16BigEndian(data.AsSpan(offset, 2));
                if (length < 2 || offset + length > data.Length) break;
                if (marker is >= 192 and <= 207 && marker is not (196 or 200 or 204) && length >= 8)
                {
                    height = BinaryPrimitives.ReadUInt16BigEndian(data.AsSpan(offset + 3, 2));
                    width = BinaryPrimitives.ReadUInt16BigEndian(data.AsSpan(offset + 5, 2));
                    break;
                }
                offset += length;
            }
        }
        else throw new GalleryValidationException("Choose a valid JPEG or PNG photo. Other file types are not supported.");
        if (width <= 0 || height <= 0 || (long)width * height > 25_000_000 || (thumbnail && (width > 768 || height > 768)))
            throw new GalleryValidationException("The photo dimensions are too large or the file is invalid. Choose a smaller image.");
        return type;
    }
}

// Photos and metadata live on the same persistent EFS volume already mounted by the app.
// Each photo commits as one directory; metadata updates use atomic replacement.
public sealed class GalleryStore : IGalleryStore
{
    private readonly string _root;
    private readonly string _webRoot;
    private readonly ILogger<GalleryStore> _logger;
    private readonly SemaphoreSlim _gate = new(1, 1);
    private bool _initialized;

    public GalleryStore(IWebHostEnvironment environment, IConfiguration configuration, ILogger<GalleryStore> logger)
    {
        var configured = configuration["Gallery:StoragePath"];
        _root = Path.GetFullPath(string.IsNullOrWhiteSpace(configured)
            ? Path.Combine(environment.ContentRootPath, "App_Data", "Gallery") : configured);
        _webRoot = environment.WebRootPath;
        _logger = logger;
    }

    private string Folder(Guid id) => Path.Combine(_root, id.ToString("N"));

    private async Task InitializeAsync(CancellationToken ct)
    {
        if (_initialized) return;
        await _gate.WaitAsync(ct);
        try
        {
            if (_initialized) return;
            Directory.CreateDirectory(_root);
            var seeds = new[] {
                (Guid.Parse("f51ee032-74eb-444c-8339-74c0f7330101"), "front_view.png", "Our school and gardens", "School life"),
                (Guid.Parse("f51ee032-74eb-444c-8339-74c0f7330102"), "side_view.png", "Classrooms and green spaces at Zhombe Mission", "School life"),
                (Guid.Parse("f51ee032-74eb-444c-8339-74c0f7330103"), "bus.jpeg", "Our school bus", "School life")
            };
            foreach (var (id, filename, caption, album) in seeds)
            {
                var source = Path.Combine(_webRoot, "Images", filename);
                if (Directory.Exists(Folder(id)) || !File.Exists(source)) continue;
                var bytes = await File.ReadAllBytesAsync(source, ct);
                var type = SchoolGallery.ValidateImage(bytes);
                await CommitAsync(new GalleryPhoto { Id = id, Caption = caption, Album = album, Published = true,
                    ContentType = type, ThumbnailType = type, CreatedAt = DateTimeOffset.UtcNow, UpdatedAt = DateTimeOffset.UtcNow }, bytes, bytes, ct);
            }
            _initialized = true;
        }
        finally { _gate.Release(); }
    }

    private async Task CommitAsync(GalleryPhoto photo, byte[] image, byte[] thumbnail, CancellationToken ct)
    {
        var pending = Path.Combine(_root, ".pending-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(pending);
        try
        {
            await File.WriteAllBytesAsync(Path.Combine(pending, "image"), image, ct);
            await File.WriteAllBytesAsync(Path.Combine(pending, "thumbnail"), thumbnail, ct);
            await File.WriteAllTextAsync(Path.Combine(pending, "metadata.json"), JsonSerializer.Serialize(photo), ct);
            try { Directory.Move(pending, Folder(photo.Id)); }
            catch (IOException) when (Directory.Exists(Folder(photo.Id))) { }
        }
        finally { if (Directory.Exists(pending)) Directory.Delete(pending, true); }
    }

    private async Task<GalleryPhoto?> ReadAsync(Guid id, CancellationToken ct)
    {
        var metadata = Path.Combine(Folder(id), "metadata.json");
        if (!File.Exists(metadata)) return null;
        try
        {
            // Share-delete permits atomic metadata replacement while other tasks are reading.
            await using var stream = new FileStream(metadata, FileMode.Open, FileAccess.Read, FileShare.ReadWrite | FileShare.Delete);
            var result = await JsonSerializer.DeserializeAsync<GalleryPhoto>(stream, cancellationToken: ct);
            return result?.Id == id ? result : null;
        }
        catch (JsonException ex) { _logger.LogError(ex, "Gallery metadata is invalid for photo {PhotoId}", id); return null; }
    }

    public async Task<List<GalleryPhoto>> ListAsync(bool includeDrafts, CancellationToken ct = default)
    {
        await InitializeAsync(ct);
        var result = new List<GalleryPhoto>();
        foreach (var folder in Directory.EnumerateDirectories(_root))
        {
            if (!Guid.TryParseExact(Path.GetFileName(folder), "N", out var id)) continue;
            var photo = await ReadAsync(id, ct);
            if (photo != null && (includeDrafts || photo.Published && !photo.Archived)) result.Add(photo);
        }
        return result.OrderBy(x => x.Order).ThenByDescending(x => x.CreatedAt).ThenBy(x => x.Id).ToList();
    }

    public async Task<GalleryPhoto?> FindAsync(Guid id, bool includeDrafts, CancellationToken ct = default)
    {
        await InitializeAsync(ct);
        var photo = await ReadAsync(id, ct);
        return photo != null && (includeDrafts || photo.Published && !photo.Archived) ? photo : null;
    }

    public async Task<GalleryPhoto> CreateAsync(GalleryPhoto photo, IFormFile image, IFormFile? thumbnail, CancellationToken ct = default)
    {
        await InitializeAsync(ct);
        static async Task<byte[]> ReadUploadAsync(IFormFile upload, int limit, CancellationToken token)
        {
            if (upload.Length <= 0 || upload.Length > limit) throw new GalleryValidationException("The photo is empty or too large. Choose a photo under 2 MB.");
            await using var source = upload.OpenReadStream();
            using var target = new MemoryStream();
            var buffer = new byte[81920];
            int count;
            while ((count = await source.ReadAsync(buffer, token)) != 0)
            {
                if (target.Length + count > limit) throw new GalleryValidationException("The photo is too large.");
                await target.WriteAsync(buffer.AsMemory(0, count), token);
            }
            return target.ToArray();
        }
        var bytes = await ReadUploadAsync(image, SchoolGallery.MaximumImageBytes, ct);
        var type = SchoolGallery.ValidateImage(bytes);
        var thumb = thumbnail == null ? bytes : await ReadUploadAsync(thumbnail, SchoolGallery.MaximumThumbnailBytes, ct);
        var thumbType = thumbnail == null ? type : SchoolGallery.ValidateImage(thumb, true);
        photo = photo with { Id = Guid.NewGuid(), ContentType = type, ThumbnailType = thumbType,
            CreatedAt = DateTimeOffset.UtcNow, UpdatedAt = DateTimeOffset.UtcNow, Archived = false };
        await CommitAsync(photo, bytes, thumb, ct);
        return photo;
    }

    public async Task<bool> UpdateAsync(Guid id, Func<GalleryPhoto, GalleryPhoto> update, CancellationToken ct = default)
    {
        await InitializeAsync(ct);
        await _gate.WaitAsync(ct);
        try
        {
            var photo = await ReadAsync(id, ct);
            if (photo == null) return false;
            var updated = update(photo) with { Id = id, ContentType = photo.ContentType,
                ThumbnailType = photo.ThumbnailType, CreatedAt = photo.CreatedAt, UpdatedAt = DateTimeOffset.UtcNow };
            var temporary = Path.Combine(Folder(id), Guid.NewGuid().ToString("N") + ".tmp");
            try
            {
                await File.WriteAllTextAsync(temporary, JsonSerializer.Serialize(updated), ct);
                File.Move(temporary, Path.Combine(Folder(id), "metadata.json"), true);
            }
            finally { if (File.Exists(temporary)) File.Delete(temporary); }
            return true;
        }
        finally { _gate.Release(); }
    }

    public async Task<GalleryImage?> ImageAsync(Guid id, bool thumbnail, bool includeDrafts, CancellationToken ct = default)
    {
        var photo = await FindAsync(id, includeDrafts, ct);
        if (photo == null) return null;
        var file = Path.Combine(Folder(id), thumbnail ? "thumbnail" : "image");
        if (!File.Exists(file)) return null;
        return new GalleryImage(await File.ReadAllBytesAsync(file, ct), thumbnail ? photo.ThumbnailType : photo.ContentType);
    }
}
