using Lib_System.Models;
using Lib_System.Repositories.Interfaces;
using Lib_System.Services.Interfaces;

namespace Lib_System.Services
{
    public class CategoryService : ICategoryService
    {
        private readonly ICategoryRepository _categoryRepository;
        private readonly IBookRepository _bookRepository;

        public CategoryService(ICategoryRepository categoryRepository, IBookRepository bookRepository)
        {
            _categoryRepository = categoryRepository;
            _bookRepository = bookRepository;
        }

        public Task<List<Category>> GetAllAsync() => _categoryRepository.GetAllAsync();

        public Task<Category?> GetDetailsAsync(int id) => _categoryRepository.GetByIdWithBooksAsync(id);

        public Task<Category?> GetForEditAsync(int id) => _categoryRepository.GetByIdAsync(id);

        public Task<Category?> GetForDeleteAsync(int id) => _categoryRepository.GetByIdAsync(id);

        public async Task CreateAsync(Category category)
        {
            await _categoryRepository.AddAsync(category);
            await _categoryRepository.SaveChangesAsync();
        }

        public async Task UpdateAsync(int id, Category category)
        {
            _categoryRepository.Update(category);
            await _categoryRepository.SaveChangesAsync();
            // DbUpdateConcurrencyException, if any, bubbles up to the controller,
            // which catches it and calls ExistsAsync - same as the original code.
        }

        public Task<bool> ExistsAsync(int id) => _categoryRepository.ExistsAsync(id);

        public async Task<ServiceResult> DeleteAsync(int id)
        {
            var result = new ServiceResult();

            var category = await _categoryRepository.GetByIdAsync(id);
            if (category == null) return result;

            bool hasBooks = await _bookRepository.AnyByCategoryAsync(id);
            if (hasBooks)
            {
                result.AddError(
                    "This category cannot be deleted because it still has books assigned to it. Reassign or delete those books first.");
                return result;
            }

            await _categoryRepository.RemoveAsync(category);
            await _categoryRepository.SaveChangesAsync();
            return result;
        }
    }
}