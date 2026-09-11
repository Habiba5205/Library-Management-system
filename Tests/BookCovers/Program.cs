using Lib_System.Services;
using Microsoft.AspNetCore.Http;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using Lib_System.Data;
using Lib_System.Models;
using Lib_System.Repositories;
using Lib_System.ViewModels;
using Microsoft.EntityFrameworkCore;

var checks = 0;
void Check(bool condition, string name)
{
    if (!condition) throw new Exception(name);
    checks++;
    Console.WriteLine($"PASS: {name}");
}
async Task Reject(byte[] bytes, string filename, string name)
{
    using var stream = new MemoryStream(bytes);
    try
    {
        await BookCoverProcessor.PrepareAsync(new FormFile(stream, 0, stream.Length, "CoverUpload", filename));
        throw new Exception($"Accepted invalid upload: {name}");
    }
    catch (ArgumentException) { checks++; Console.WriteLine($"PASS: {name}"); }
}
foreach (var extension in new[] { "jpg", "png", "webp" })
{
    using var image = new Image<Rgba32>(1000, 1500, Color.Teal);
    using var source = new MemoryStream();
    if (extension == "jpg") await image.SaveAsJpegAsync(source);
    else if (extension == "png") await image.SaveAsPngAsync(source);
    else await image.SaveAsWebpAsync(source);
    source.Position = 0;
    var result = await BookCoverProcessor.PrepareAsync(new FormFile(source, 0, source.Length, "CoverUpload", $"cover.{extension}"));
    using var decoded = Image.Load(result);
    Check(decoded.Width <= 900 && decoded.Height <= 1200, $"{extension}: decoded and resized");
    Check(Image.DetectFormat(result).Name == "PNG", $"{extension}: normalized PNG");
}
await Reject([], "cover.png", "Empty upload rejected");
await Reject(new byte[BookCoverProcessor.MaxBytes + 1], "cover.jpg", "Oversized upload rejected");
await Reject("<script>alert(1)</script>"u8.ToArray(), "cover.jpg", "Disguised non-image rejected");
await Reject("<svg></svg>"u8.ToArray(), "cover.svg", "SVG rejected");
using (var large = new Image<Rgba32>(6001, 1))
using (var source = new MemoryStream())
{
    await large.SaveAsPngAsync(source);
    await Reject(source.ToArray(), "large.png", "Excessive dimensions rejected");
    await Reject(source.ToArray(), "cover.exe", "Non-image filename rejected");
}
var databaseName = "LibSystemCoverTests_" + Guid.NewGuid().ToString("N");
var options = new DbContextOptionsBuilder<ApplicationDbContext>()
    .UseSqlServer($@"Server=(localdb)\MSSQLLocalDB;Database={databaseName};Trusted_Connection=True;TrustServerCertificate=True").Options;
await using var db = new ApplicationDbContext(options);
try
{
    await db.Database.MigrateAsync();
    var category = new Category { Name = "Cover test" };
    var author = new Author { Name = "Cover test" };
    db.AddRange(category, author);
    await db.SaveChangesAsync();
    var service = new BookService(new BookRepository(db), new CategoryRepository(db), new AuthorRepository(db));
    using var image = new Image<Rgba32>(10, 20, Color.Teal);
    using var imageStream = new MemoryStream();
    await image.SaveAsPngAsync(imageStream);
    var original = imageStream.ToArray();
    var vm = new BookFormViewModel
    {
        ISBN = "cover-test", Title = "Cover test", CategoryId = category.CategoryId,
        SelectedAuthorIds = [author.AuthorId], PreparedCover = original
    };
    await service.CreateAsync(vm);
    db.ChangeTracker.Clear();
    var book = await db.Books.SingleAsync();
    Check(book.CoverImage!.SequenceEqual(original), "Cover persisted with new book");
    vm.BookId = book.BookId;
    vm.PreparedCover = null;
    vm.Title = "Edited title";
    await service.UpdateAsync(book.BookId, vm);
    db.ChangeTracker.Clear();
    Check((await db.Books.SingleAsync()).CoverImage!.SequenceEqual(original), "Edit without upload preserves cover");
    using var replacementStream = new MemoryStream();
    await image.SaveAsJpegAsync(replacementStream);
    replacementStream.Position = 0;
    vm.PreparedCover = await BookCoverProcessor.PrepareAsync(new FormFile(replacementStream, 0, replacementStream.Length, "CoverUpload", "replace.jpg"));
    await service.UpdateAsync(book.BookId, vm);
    db.ChangeTracker.Clear();
    Check((await db.Books.SingleAsync()).CoverImage!.SequenceEqual(vm.PreparedCover), "Replacement saved");
    vm.PreparedCover = null;
    vm.RemoveCover = true;
    await service.UpdateAsync(book.BookId, vm);
    db.ChangeTracker.Clear();
    Check((await db.Books.SingleAsync()).CoverImage == null, "Cover removed");
    await service.DeleteAsync(book.BookId);
    Check(!await db.Books.AnyAsync(), "Deleting book removes its cover record");
}
finally
{
    await db.Database.EnsureDeletedAsync();
}
Console.WriteLine($"{checks} cover checks passed.");
