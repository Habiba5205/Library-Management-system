using Lib_System.Models;
using Lib_System.ViewModels;

namespace Lib_System.Services.Interfaces
{
    public interface IBookService
    {
        Task<List<Book>> GetAllAsync(string? searchString, int? categoryId, int? authorId, bool memberRestrictToAvailable);

        Task<Book?> GetDetailsAsync(int id);
        Task<Book?> GetForEditAsync(int id);
        Task<Book?> GetForDeleteAsync(int id);

        // "Validate then Act": call ValidateForCreateAsync, merge its errors into
        // ModelState alongside the annotation errors already there, THEN check
        // ModelState.IsValid before calling CreateAsync. Never call CreateAsync
        // without checking ModelState.IsValid first.
        Task<ServiceResult> ValidateForCreateAsync(BookFormViewModel vm);
        Task CreateAsync(BookFormViewModel vm);

        Task<ServiceResult> ValidateForEditAsync(int id, BookFormViewModel vm);
        Task<bool> UpdateAsync(int id, BookFormViewModel vm); // false = book no longer exists

        Task<bool> ExistsAsync(int id);
        Task<ServiceResult> DeleteAsync(int id);

        Task<List<Category>> GetCategoriesAsync();
        Task<List<Author>> GetAllAuthorsAsync();
    }
}