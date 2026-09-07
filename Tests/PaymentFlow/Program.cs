using Lib_System.Data;
using Lib_System.Models;
using Lib_System.Repositories;
using Lib_System.Services;
using Lib_System.ViewModels;
using Microsoft.EntityFrameworkCore;

var databaseName = "LibSystemPaymentTests_" + Guid.NewGuid().ToString("N");
var options = new DbContextOptionsBuilder<ApplicationDbContext>()
    .UseSqlServer($@"Server=(localdb)\MSSQLLocalDB;Database={databaseName};Trusted_Connection=True;TrustServerCertificate=True").Options;
var clock = new TestClock();
await using var db = new ApplicationDbContext(options);
try
{
    await db.Database.EnsureCreatedAsync();
    var member = new User { Name = "Test", Username = "test", Email = "test@example.test", PasswordHash = "test", RoleId = 3, Status = "Active" };
    var category = new Category { Name = "Test" };
    db.Users.Add(member);
    db.Categories.Add(category);
    await db.SaveChangesAsync();
    async Task<int> Book()
    {
        var book = new Book { Title = "Test", ISBN = Guid.NewGuid().ToString("N")[..13], Price = 25, CategoryId = category.CategoryId, AvailabilityStatus = "Available" };
        db.Books.Add(book);
        await db.SaveChangesAsync();
        return book.BookId;
    }
    var workflow = new PaymentWorkflowRepository(db, clock);
    async Task<Payment> Read(int id)
    {
        db.ChangeTracker.Clear();
        return await db.Payments.Include(p => p.Borrowing).ThenInclude(b => b!.Book).SingleAsync(p => p.PaymentId == id);
    }
    void Check(bool condition, string label)
    {
        if (!condition) throw new Exception(label);
        Console.WriteLine("PASS: " + label);
    }
    var bookId = await Book();
    var cash = (await workflow.ReserveAsync(bookId, member.UserId, "Cash"))!.Value;
    var p = await Read(cash);
    Check(p.Status == "Pending" && p.Borrowing!.Status == "Reserved" && p.Borrowing.Book!.AvailabilityStatus == "Reserved", "Cash reserves without borrowing");
    Check(p.Borrowing!.ReservationExpiresAtUtc == clock.GetUtcNow().UtcDateTime.AddHours(48), "Cash deadline is exactly 48 hours");
    Check(await workflow.ReserveAsync(bookId, member.UserId, "Cash") == null, "Cannot reserve an unavailable book");
    clock.Advance(TimeSpan.FromHours(24));
    Check(await workflow.CompleteAsync(cash, "Cash", null, true), "Manager confirms cash before deadline");
    p = await Read(cash);
    Check(p.Status == "Paid" && p.Borrowing!.Status == "Borrowed" && p.Borrowing.DueDate == clock.GetUtcNow().UtcDateTime.Date.AddDays(14), "Cash loan lasts 14 days from payment");
    var due = p.Borrowing!.DueDate;
    clock.Advance(TimeSpan.FromHours(1));
    Check(await workflow.CompleteAsync(cash, "Cash", null, true) && (await Read(cash)).Borrowing!.DueDate == due, "Duplicate confirmation does not restart loan");
    var expired = (await workflow.ReserveAsync(await Book(), member.UserId, "Cash"))!.Value;
    clock.Advance(TimeSpan.FromHours(48));
    Check(!await workflow.CompleteAsync(expired, "Cash", null, true), "Payment at expiry boundary is rejected");
    p = await Read(expired);
    Check(p.Status == "Failed" && p.Borrowing!.Status == "Failed" && p.Borrowing.Book!.AvailabilityStatus == "Available", "Expired reservation releases book");
    var unattended = (await workflow.ReserveAsync(await Book(), member.UserId, "Cash"))!.Value;
    clock.Advance(TimeSpan.FromHours(49));
    await workflow.ExpireAsync();
    Check((await Read(unattended)).Status == "Failed", "Unpaid reservation expires without manager");
    var card = (await workflow.ReserveAsync(await Book(), member.UserId, "Card"))!.Value;
    Check(!await workflow.CompleteAsync(card, "Cash", null, true), "Cash confirmation cannot modify card payment");
    Check(!await workflow.CompleteAsync(card, "Card", member.UserId + 1, true), "Another member cannot complete card payment");
    Check(await workflow.CompleteAsync(card, "Card", member.UserId, true), "Demo card success");
    p = await Read(card);
    Check(p.Status == "Paid" && p.Borrowing!.Book!.AvailabilityStatus == "Borrowed" && p.Borrowing.DueDate == p.Borrowing.BorrowDate.AddDays(14), "Card success starts a 14-day borrowing");
    var failed = (await workflow.ReserveAsync(await Book(), member.UserId, "Card"))!.Value;
    Check(await workflow.CompleteAsync(failed, "Card", member.UserId, false), "Card failure or cancellation");
    p = await Read(failed);
    Check(p.Status == "Failed" && p.Borrowing!.Book!.AvailabilityStatus == "Available", "Card failure releases book");
    Check(!await workflow.CompleteAsync(failed, "Card", member.UserId, true), "Failed payment cannot be replayed as success");
    Check(await workflow.ReserveAsync(await Book(), member.UserId, "Mobile Wallet") == null, "Other payment methods rejected");
    var sharedBook = await Book();
    async Task<int?> CompetingReservation()
    {
        await using var contender = new ApplicationDbContext(options);
        return await new PaymentWorkflowRepository(contender, clock).ReserveAsync(sharedBook, member.UserId, "Cash");
    }
    var competing = await Task.WhenAll(CompetingReservation(), CompetingReservation());
    Check(competing.Count(id => id.HasValue) == 1, "Concurrent requests reserve a book only once");
    var bookService = new BookService(new BookRepository(db), new CategoryRepository(db), new AuthorRepository(db));
    async Task<BookFormViewModel> EditModel(int id, string status)
    {
        var book = await db.Books.FindAsync(id);
        return new BookFormViewModel
        {
            BookId = id, ISBN = book!.ISBN, Title = book.Title, Price = book.Price,
            PublicationYear = 2026, CategoryId = book.CategoryId, AvailabilityStatus = status
        };
    }
    var editableId = await Book();
    Check(await bookService.UpdateAsync(editableId, await EditModel(editableId, "Borrowed")), "Manual status edit saves without an active borrowing");
    Check(await bookService.UpdateAsync(editableId, await EditModel(editableId, "Available")), "Manually labelled Borrowed book can be made Available");
    db.ChangeTracker.Clear();
    Check((await db.Books.FindAsync(editableId))!.AvailabilityStatus == "Available", "Availability change persists in database");
    try
    {
        await bookService.UpdateAsync(sharedBook, await EditModel(sharedBook, "Available"));
        throw new Exception("Active reservation was overridden");
    }
    catch (InvalidOperationException)
    {
        Console.WriteLine("PASS: Active reservation blocks manual availability change with an error");
    }
}
finally
{
    // This randomly named test database contains no application data.
    await db.Database.EnsureDeletedAsync();
}

sealed class TestClock : TimeProvider
{
    private DateTimeOffset now = new(2026, 9, 7, 12, 0, 0, TimeSpan.Zero);
    public override DateTimeOffset GetUtcNow() => now;
    public void Advance(TimeSpan span) => now += span;
}
