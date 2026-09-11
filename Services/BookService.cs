using Lib_System.Models;
using Lib_System.Repositories.Interfaces;
using Lib_System.Services.Interfaces;
using Lib_System.ViewModels;

namespace Lib_System.Services
{
    public class BookService : IBookService
    {
        private readonly IBookRepository _bookRepository;
        private readonly ICategoryRepository _categoryRepository;
        private readonly IAuthorRepository _authorRepository;

        public BookService(
            IBookRepository bookRepository,
            ICategoryRepository categoryRepository,
            IAuthorRepository authorRepository)
        {
            _bookRepository = bookRepository;
            _categoryRepository = categoryRepository;
            _authorRepository = authorRepository;
        }

        public Task<List<Book>> GetAllAsync(string? searchString, int? categoryId, int? authorId, bool memberRestrictToAvailable)
        {
            return _bookRepository.GetAllAsync(searchString, categoryId, authorId, memberRestrictToAvailable);
        }

        public Task<Book?> GetDetailsAsync(int id) => _bookRepository.GetByIdWithDetailsAsync(id);

        public Task<Book?> GetForEditAsync(int id) => _bookRepository.GetByIdWithAuthorsAsync(id);

        public Task<Book?> GetForDeleteAsync(int id) => _bookRepository.GetByIdWithBorrowingsAsync(id);

        public async Task<ServiceResult> ValidateForCreateAsync(BookFormViewModel vm)
        {
            var result = new ServiceResult();

            if (vm.SelectedAuthorIds == null || vm.SelectedAuthorIds.Count == 0)
            {
                result.AddError("SelectedAuthorIds", "Select at least one author.");
            }

            if (await _bookRepository.IsbnExistsAsync(vm.ISBN, null))
            {
                result.AddError("ISBN", "This ISBN is already in use.");
            }

            return result;
        }

        public async Task CreateAsync(BookFormViewModel vm)
        {
            var book = new Book
            {
                ISBN = vm.ISBN,
                Title = vm.Title,
                CoverImage = vm.PreparedCover,
                PublicationYear = vm.PublicationYear,
                Price = vm.Price,
                AvailabilityStatus = "Available",
                CategoryId = vm.CategoryId,
                ManagerId = vm.ManagerId
            };

            foreach (var authorId in vm.SelectedAuthorIds!)
            {
                book.BookAuthors.Add(new BookAuthor { AuthorId = authorId });
            }

            await _bookRepository.AddAsync(book);
            await _bookRepository.SaveChangesAsync();
        }

        public async Task<ServiceResult> ValidateForEditAsync(int id, BookFormViewModel vm)
        {
            var result = new ServiceResult();
            var statusError = await ValidateAvailabilityAsync(id, vm.AvailabilityStatus);
            if (statusError != null)
                result.AddError(nameof(vm.AvailabilityStatus), statusError);

            if (vm.SelectedAuthorIds == null || vm.SelectedAuthorIds.Count == 0)
            {
                result.AddError("SelectedAuthorIds", "Select at least one author.");
            }

            if (await _bookRepository.IsbnExistsAsync(vm.ISBN, id))
            {
                result.AddError("ISBN", "This ISBN is already in use.");
            }

            return result;
        }

        public async Task<bool> UpdateAsync(int id, BookFormViewModel vm)
        {
            var statusError = await ValidateAvailabilityAsync(id, vm.AvailabilityStatus);
            if (statusError != null)
                throw new InvalidOperationException(statusError);

            var book = await _bookRepository.GetByIdWithAuthorsAsync(id);
            if (book == null) return false;

            book.ISBN = vm.ISBN;
            book.Title = vm.Title;
            if (vm.PreparedCover != null) book.CoverImage = vm.PreparedCover;
            else if (vm.RemoveCover) book.CoverImage = null;
            book.PublicationYear = vm.PublicationYear;
            book.Price = vm.Price;
            book.AvailabilityStatus = vm.AvailabilityStatus;
            book.CategoryId = vm.CategoryId;
            book.ManagerId = vm.ManagerId;

            var toRemove = book.BookAuthors
                .Where(ba => !vm.SelectedAuthorIds!.Contains(ba.AuthorId))
                .ToList();
            foreach (var ba in toRemove)
            {
                book.BookAuthors.Remove(ba);
            }

            var existingIds = book.BookAuthors.Select(ba => ba.AuthorId).ToList();
            foreach (var authorId in vm.SelectedAuthorIds!.Except(existingIds))
            {
                book.BookAuthors.Add(new BookAuthor { AuthorId = authorId, BookId = book.BookId });
            }

            await _bookRepository.SaveChangesAsync();
            return true;
        }

        private async Task<string?> ValidateAvailabilityAsync(int id, string status)
        {
            if (status is not ("Available" or "Unavailable" or "Reserved" or "Borrowed"))
                return "Select a valid availability status.";

            var book = await _bookRepository.GetByIdWithBorrowingsAsync(id);
            if (book != null && book.AvailabilityStatus != status &&
                book.Borrowings.Any(b => b.Status is "Reserved" or "Borrowed" or "Early Return Requested"))
                return "This book has an active reservation or borrowing. Complete the payment, expiry, or return process before changing its availability.";

            return null;
        }

        public Task<bool> ExistsAsync(int id) => _bookRepository.ExistsAsync(id);

        public async Task<ServiceResult> DeleteAsync(int id)
        {
            var result = new ServiceResult();

            var book = await _bookRepository.GetByIdWithBorrowingsAsync(id);
            if (book == null) return result;

            if (book.AvailabilityStatus is "Borrowed" or "Reserved")
            {
                result.AddError("This book cannot be deleted while its availability status is Borrowed or Reserved.");
                return result;
            }

            if (book.Borrowings.Any())
            {
                result.AddError(
                    "This book cannot be deleted because it has borrowing history. Resolve or remove the related borrowings first.");
                return result;
            }

            await _bookRepository.RemoveAsync(book);
            await _bookRepository.SaveChangesAsync();
            return result;
        }

        public Task<List<Category>> GetCategoriesAsync() => _categoryRepository.GetAllAsync();

        public Task<List<Author>> GetAllAuthorsAsync() => _authorRepository.GetAllAsync();
    }
}
