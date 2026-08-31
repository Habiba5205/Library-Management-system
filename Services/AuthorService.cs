using Lib_System.Models;
using Lib_System.Repositories.Interfaces;
using Lib_System.Services.Interfaces;

namespace Lib_System.Services
{
    public class AuthorService : IAuthorService
    {
        private readonly IAuthorRepository _authorRepository;

        public AuthorService(IAuthorRepository authorRepository)
        {
            _authorRepository = authorRepository;
        }

        public Task<List<Author>> GetAllAsync() => _authorRepository.GetAllAsync();

        public Task<Author?> GetDetailsAsync(int id) => _authorRepository.GetByIdWithBooksAsync(id);

        public Task<Author?> GetForEditAsync(int id) => _authorRepository.GetByIdAsync(id);

        public Task<Author?> GetForDeleteAsync(int id) => _authorRepository.GetByIdWithBooksAsync(id);

        public async Task CreateAsync(Author author)
        {
            await _authorRepository.AddAsync(author);
            await _authorRepository.SaveChangesAsync();
        }

        public async Task UpdateAsync(int id, Author author)
        {
            _authorRepository.Update(author);
            await _authorRepository.SaveChangesAsync();
        }

        public Task<bool> ExistsAsync(int id) => _authorRepository.ExistsAsync(id);

        public async Task DeleteAsync(int id)
        {
            // No block here, matching the original: BookAuthor -> Author cascades
            // on delete, so this only removes the link rows, not the books.
            var author = await _authorRepository.GetByIdAsync(id);
            if (author != null)
            {
                await _authorRepository.RemoveAsync(author);
                await _authorRepository.SaveChangesAsync();
            }
        }
    }
}