using Lib_System.Models;

namespace Lib_System.Repositories.Interfaces
{
    public interface IBookRepository
    {
        Task<List<Book>> GetAllAsync(string? searchString, int? categoryId, int? authorId, bool availableOnly);

        Task<Book?> GetByIdAsync(int id);
        Task<Book?> GetByIdWithDetailsAsync(int id);   // Category, Manager, BookAuthors.Author (Details)
        Task<Book?> GetByIdWithAuthorsAsync(int id);   // BookAuthors only (Edit)
        Task<Book?> GetByIdWithBorrowingsAsync(int id); // Category, Borrowings (Delete)

        Task<List<Book>> GetAvailableBooksAsync();

        Task<bool> IsbnExistsAsync(string isbn, int? excludeBookId);
        Task<bool> AnyByCategoryAsync(int categoryId);

        Task AddAsync(Book book);
        Task RemoveAsync(Book book);
        Task<bool> ExistsAsync(int id);
        Task SaveChangesAsync();
    }
}