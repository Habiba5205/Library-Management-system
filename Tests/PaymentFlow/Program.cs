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
    var authorService = new AuthorService(new AuthorRepository(db));
    var emptyAuthor = new Author { Name = "Unlinked author" };
    await authorService.CreateAsync(emptyAuthor);
    Check((await authorService.DeleteAsync(emptyAuthor.AuthorId)).Success &&
        !await db.Authors.AnyAsync(a => a.AuthorId == emptyAuthor.AuthorId), "Author without books can be deleted");
    var linkedAuthor = new Author { Name = "Linked author" };
    await authorService.CreateAsync(linkedAuthor);
    db.BookAuthors.Add(new BookAuthor { AuthorId = linkedAuthor.AuthorId, BookId = editableId });
    await db.SaveChangesAsync();
    Check(!(await authorService.DeleteAsync(linkedAuthor.AuthorId)).Success &&
        await db.BookAuthors.AnyAsync(ba => ba.AuthorId == linkedAuthor.AuthorId) &&
        await db.Books.AnyAsync(b => b.BookId == editableId), "Linked author deletion is blocked and book links remain");
    var categoryService = new CategoryService(new CategoryRepository(db), new BookRepository(db));
    var emptyCategory = new Category { Name = "Empty category" };
    await categoryService.CreateAsync(emptyCategory);
    Check((await categoryService.DeleteAsync(emptyCategory.CategoryId)).Success &&
        !await db.Categories.AnyAsync(c => c.CategoryId == emptyCategory.CategoryId), "Empty category can be deleted");
    Check(!(await categoryService.DeleteAsync(category.CategoryId)).Success &&
        await db.Categories.AnyAsync(c => c.CategoryId == category.CategoryId), "Category with books cannot be deleted");
    var protectedBookId = await Book();
    foreach (var status in new[] { "Reserved", "Borrowed" })
    {
        await bookService.UpdateAsync(protectedBookId, await EditModel(protectedBookId, status));
        Check(!(await bookService.DeleteAsync(protectedBookId)).Success &&
            await db.Books.AnyAsync(b => b.BookId == protectedBookId), status + " book without history cannot be deleted");
    }
    await bookService.UpdateAsync(protectedBookId, await EditModel(protectedBookId, "Available"));
    Check((await bookService.DeleteAsync(protectedBookId)).Success &&
        !await db.Books.AnyAsync(b => b.BookId == protectedBookId), "Available book without history can be deleted");
    var historicalBookId = (await Read(expired)).Borrowing!.BookId;
    Check(!(await bookService.DeleteAsync(historicalBookId)).Success, "Available book with borrowing history cannot be deleted");
    var overdueFines = new OverdueFineRepository(db, clock);
    async Task<Borrowing> Loan(string status, int dueDaysAgo)
    {
        var loan = new Borrowing
        {
            BookId = await Book(), UserId = member.UserId,
            BorrowDate = clock.GetLocalNow().Date.AddDays(-14 - dueDaysAgo),
            DueDate = clock.GetLocalNow().Date.AddDays(-dueDaysAgo), Status = status
        };
        db.Borrowings.Add(loan);
        await db.SaveChangesAsync();
        return loan;
    }
    var dueToday = await Loan("Borrowed", 0);
    var overdue = await Loan("Borrowed", 3);
    var earlyRequested = await Loan("Early Return Requested", 1);
    var unpaidReservation = await Loan("Reserved", 3);
    var failedLoan = await Loan("Failed", 3);
    var manual = new Fine { BorrowingId = overdue.BorrowingId, Amount = 7, Reason = "Damage", Status = "Unpaid" };
    db.Fines.Add(manual);
    await db.SaveChangesAsync();
    await overdueFines.SynchronizeAsync();
    Check(!await db.Fines.AnyAsync(f => f.BorrowingId == dueToday.BorrowingId && f.IsAutomatic), "No fine on due date");
    var automatic = await db.Fines.SingleAsync(f => f.BorrowingId == overdue.BorrowingId && f.IsAutomatic);
    Check(automatic.Amount == 15, "Three overdue days cost 15 EGP");
    Check(await db.Fines.AnyAsync(f => f.BorrowingId == earlyRequested.BorrowingId && f.IsAutomatic && f.Amount == 5), "Early return request does not stop fines");
    Check(!await db.Fines.AnyAsync(f => (f.BorrowingId == unpaidReservation.BorrowingId || f.BorrowingId == failedLoan.BorrowingId) && f.IsAutomatic), "Reservations and failed loans are excluded");
    Check(manual.Amount == 7 && !manual.IsAutomatic, "Manual fine is preserved");
    clock.Advance(TimeSpan.FromDays(2));
    await overdueFines.SynchronizeAsync();
    Check(automatic.Amount == 25 && await db.Fines.CountAsync(f => f.BorrowingId == overdue.BorrowingId && f.IsAutomatic) == 1, "Catch-up increases one fine without duplication");
    var fineService = new FineService(new FineRepository(db), new BorrowingRepository(db));
    try
    {
        await fineService.UpdateAsync(automatic.FineId, new FineFormViewModel { Status = "Paid" });
        throw new Exception("Unreturned book fine accepted as paid");
    }
    catch (InvalidOperationException) { Console.WriteLine("PASS: Automatic fine cannot be paid before return"); }
    overdue.Status = "Returned";
    overdue.ReturnDate = clock.GetLocalNow().Date;
    await db.SaveChangesAsync();
    await overdueFines.SynchronizeAsync(overdue.BorrowingId);
    clock.Advance(TimeSpan.FromDays(4));
    await overdueFines.SynchronizeAsync();
    Check(automatic.Amount == 25, "Returned book fine stops growing");
    Check(await fineService.UpdateAsync(automatic.FineId, new FineFormViewModel { Status = "Paid", Amount = 1 }), "Manager can settle returned book fine");
    Check(automatic.Status == "Paid" && automatic.Amount == 25, "Automatic amount cannot be overridden");
    try
    {
        await fineService.DeleteAsync(automatic.FineId);
        throw new Exception("Automatic fine was deleted");
    }
    catch (InvalidOperationException) { Console.WriteLine("PASS: Automatic fine deletion is blocked"); }
    var concurrentLoan = await Loan("Borrowed", 2);
    async Task RefreshFines()
    {
        await using var workerDb = new ApplicationDbContext(options);
        await new OverdueFineRepository(workerDb, clock).SynchronizeAsync();
    }
    await Task.WhenAll(RefreshFines(), RefreshFines());
    Check(await db.Fines.CountAsync(f => f.BorrowingId == concurrentLoan.BorrowingId && f.IsAutomatic) == 1, "Concurrent checks create only one automatic fine");
    var dashboardRepository = new DashboardRepository(db);
    var memberDashboard = await dashboardRepository.GetDashboardAsync(true, member.UserId);
    var staffDashboard = await dashboardRepository.GetDashboardAsync(false, member.UserId);
    Check(memberDashboard.BookCount == staffDashboard.BookCount &&
        memberDashboard.BookCount > memberDashboard.AvailableBookCount,
        "Member total includes reserved and borrowed books");
    Check(memberDashboard.AvailableBookCount == staffDashboard.AvailableBookCount,
        "Members and staff see the same available book count");
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
