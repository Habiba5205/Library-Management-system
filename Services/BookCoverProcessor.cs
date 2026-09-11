using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Formats;
using SixLabors.ImageSharp.Processing;

namespace Lib_System.Services;

public static class BookCoverProcessor
{
    public const int MaxBytes = 5 * 1024 * 1024;

    public static async Task<byte[]> PrepareAsync(IFormFile upload)
    {
        if (upload.Length <= 0 || upload.Length > MaxBytes)
            throw new ArgumentException("Choose an image no larger than 5 MB.");

        var extension = Path.GetExtension(upload.FileName).ToLowerInvariant();
        if (extension is not (".jpg" or ".jpeg" or ".png" or ".webp"))
            throw new ArgumentException("Choose a JPG, PNG, or WebP image.");

        using var source = new MemoryStream();
        await using var input = upload.OpenReadStream();
        var buffer = new byte[81920];
        int count;
        while ((count = await input.ReadAsync(buffer)) > 0)
        {
            if (source.Length + count > MaxBytes)
                throw new ArgumentException("Choose an image no larger than 5 MB.");
            await source.WriteAsync(buffer.AsMemory(0, count));
        }
        source.Position = 0;
        try
        {
            var options = new DecoderOptions { MaxFrames = 1, SkipMetadata = true };
            var info = await Image.IdentifyAsync(options, source);
            var format = info.Metadata.DecodedImageFormat?.Name.ToUpperInvariant();
            if (format is not ("JPEG" or "PNG" or "WEBP"))
                throw new ArgumentException("Choose a valid JPG, PNG, or WebP image.");
            if (info.Width > 6000 || info.Height > 6000 || (long)info.Width * info.Height > 16000000)
                throw new ArgumentException("Choose an image up to 6,000 pixels per side and 16 megapixels.");
            source.Position = 0;
            using var image = await Image.LoadAsync(options, source);
            image.Mutate(context => context.Resize(new ResizeOptions
            {
                Size = new Size(900, 1200), Mode = ResizeMode.Max
            }));
            using var output = new MemoryStream();
            await image.SaveAsPngAsync(output);
            return output.ToArray();
        }
        catch (Exception ex) when (ex is UnknownImageFormatException or InvalidImageContentException or NotSupportedException)
        {
            throw new ArgumentException("The image could not be read. Choose a valid JPG, PNG, or WebP image.", ex);
        }
    }
}
