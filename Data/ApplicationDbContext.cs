using Lib_System.Models;
using Microsoft.EntityFrameworkCore;

namespace Lib_System.Data;

public class ApplicationDbContext : DbContext
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
        : base(options)
    {
    }

    public DbSet<Role> Roles => Set<Role>();
    public DbSet<User> Users => Set<User>();
    public DbSet<Book> Books => Set<Book>();
    public DbSet<Author> Authors => Set<Author>();
    public DbSet<Category> Categories => Set<Category>();
    public DbSet<BookAuthor> BookAuthors => Set<BookAuthor>();
    public DbSet<Borrowing> Borrowings => Set<Borrowing>();
    public DbSet<Payment> Payments => Set<Payment>();
    public DbSet<Fine> Fines => Set<Fine>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<Role>()
            .HasIndex(role => role.RoleName)
            .IsUnique();

        modelBuilder.Entity<User>()
            .HasIndex(user => user.Email)
            .IsUnique();

        modelBuilder.Entity<User>()
            .HasIndex(user => user.Username)
            .IsUnique();

        modelBuilder.Entity<Book>()
            .HasIndex(book => book.ISBN)
            .IsUnique();

        modelBuilder.Entity<Book>()
            .Property(book => book.Price)
            .HasPrecision(18, 2);

        modelBuilder.Entity<Payment>()
            .Property(payment => payment.Amount)
            .HasPrecision(18, 2);

        modelBuilder.Entity<Fine>()
            .HasIndex(fine => fine.BorrowingId)
            .IsUnique()
            .HasFilter("[IsAutomatic] = 1");

        modelBuilder.Entity<Fine>()
            .Property(fine => fine.Amount)
            .HasPrecision(18, 2);

        modelBuilder.Entity<BookAuthor>()
            .HasKey(bookAuthor => new { bookAuthor.BookId, bookAuthor.AuthorId });

        modelBuilder.Entity<BookAuthor>()
            .HasOne(bookAuthor => bookAuthor.Book)
            .WithMany(book => book.BookAuthors)
            .HasForeignKey(bookAuthor => bookAuthor.BookId);

        modelBuilder.Entity<BookAuthor>()
            .HasOne(bookAuthor => bookAuthor.Author)
            .WithMany(author => author.BookAuthors)
            .HasForeignKey(bookAuthor => bookAuthor.AuthorId);

        modelBuilder.Entity<Book>()
            .HasOne(book => book.Category)
            .WithMany(category => category.Books)
            .HasForeignKey(book => book.CategoryId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<Book>()
            .HasOne(book => book.Manager)
            .WithMany(user => user.ManagedBooks)
            .HasForeignKey(book => book.ManagerId)
            .OnDelete(DeleteBehavior.SetNull);

        modelBuilder.Entity<User>()
            .HasOne(user => user.Role)
            .WithMany(role => role.Users)
            .HasForeignKey(user => user.RoleId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<Borrowing>()
            .HasOne(borrowing => borrowing.Book)
            .WithMany(book => book.Borrowings)
            .HasForeignKey(borrowing => borrowing.BookId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<Borrowing>()
            .HasOne(borrowing => borrowing.User)
            .WithMany(user => user.Borrowings)
            .HasForeignKey(borrowing => borrowing.UserId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<Payment>()
            .HasOne(payment => payment.Borrowing)
            .WithMany(borrowing => borrowing.Payments)
            .HasForeignKey(payment => payment.BorrowingId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<Fine>()
            .HasOne(fine => fine.Borrowing)
            .WithMany(borrowing => borrowing.Fines)
            .HasForeignKey(fine => fine.BorrowingId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<Role>().HasData(
            new Role { RoleId = 1, RoleName = "Admin", Description = "System administrator", Permissions = "Manage all modules" },
            new Role { RoleId = 2, RoleName = "Manager", Description = "Library manager", Permissions = "Manage books, authors, categories, and borrowings" },
            new Role { RoleId = 3, RoleName = "Member", Description = "Library member", Permissions = "Borrow and return books" }
        );
    }
}
