using Lib_System.Data;
using Lib_System.Models;
using Lib_System.Repositories.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace Lib_System.Repositories
{
    public class BookRepository : IBookRepository
    {
        private readonly ApplicationDbContext _context;

        public BookRepository(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<List<Book>> GetAllAsync(string? searchString, int? categoryId, int? authorId, bool availableOnly)
        {
            var query = _context.Books
                .Include(b => b.Category)
                .Include(b => b.BookAuthors)
                    .ThenInclude(ba => ba.Author)
                .AsQueryable();

            if (!string.IsNullOrWhiteSpace(searchString))
            {
                query = query.Where(b => b.Title.Contains(searchString) || b.ISBN.Contains(searchString));
            }

            if (categoryId.HasValue)
            {
                query = query.Where(b => b.CategoryId == categoryId.Value);
            }

            if (authorId.HasValue)
            {
                query = query.Where(b => b.BookAuthors.Any(ba => ba.AuthorId == authorId.Value));
            }

            if (availableOnly)
            {
                query = query.Where(b => b.AvailabilityStatus == "Available");
            }

            return await query.OrderBy(b => b.Title).ToListAsync();
        }

        public async Task<Book?> GetByIdAsync(int id)
        {
            return await _context.Books.FindAsync(id);
        }

        public async Task<Book?> GetByIdWithDetailsAsync(int id)
        {
            return await _context.Books
                .Include(b => b.Category)
                .Include(b => b.Manager)
                .Include(b => b.BookAuthors)
                    .ThenInclude(ba => ba.Author)
                .FirstOrDefaultAsync(b => b.BookId == id);
        }

        public async Task<Book?> GetByIdWithAuthorsAsync(int id)
        {
            return await _context.Books
                .Include(b => b.BookAuthors)
                .FirstOrDefaultAsync(b => b.BookId == id);
        }

        public async Task<Book?> GetByIdWithBorrowingsAsync(int id)
        {
            return await _context.Books
                .Include(b => b.Category)
                .Include(b => b.Borrowings)
                .FirstOrDefaultAsync(b => b.BookId == id);
        }

        public async Task<List<Book>> GetAvailableBooksAsync()
        {
            return await _context.Books
                .Where(b => b.AvailabilityStatus == "Available")
                .OrderBy(b => b.Title)
                .ToListAsync();
        }

        public async Task<bool> IsbnExistsAsync(string isbn, int? excludeBookId)
        {
            var query = _context.Books.Where(b => b.ISBN == isbn);
            if (excludeBookId.HasValue)
            {
                query = query.Where(b => b.BookId != excludeBookId.Value);
            }
            return await query.AnyAsync();
        }

        public async Task<bool> AnyByCategoryAsync(int categoryId)
        {
            return await _context.Books.AnyAsync(b => b.CategoryId == categoryId);
        }

        public async Task AddAsync(Book book)
        {
            await _context.Books.AddAsync(book);
        }

        public async Task RemoveAsync(Book book)
        {
            _context.Books.Remove(book);
            await Task.CompletedTask;
        }

        public async Task<bool> ExistsAsync(int id)
        {
            return await _context.Books.AnyAsync(b => b.BookId == id);
        }

        public async Task SaveChangesAsync()
        {
            await _context.SaveChangesAsync();
        }
    }
}