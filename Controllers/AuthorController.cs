using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Lib_System.Data;
using Lib_System.Models;

namespace Lib_System.Controllers
{
    public class AuthorController : Controller
    {
        private readonly ApplicationDbContext _context;

        public AuthorController(ApplicationDbContext context)
        {
            _context = context;
        }

        // GET: Author
        [Microsoft.AspNetCore.Authorization.Authorize(Roles = "Admin,Manager")]
        public async Task<IActionResult> Index()
        {
            return View(await _context.Authors.ToListAsync());
        }

        // GET: Author/Details/5
        [Microsoft.AspNetCore.Authorization.Authorize(Roles = "Admin,Manager")]
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null) return NotFound();

            var author = await _context.Authors
                .Include(a => a.BookAuthors)
                    .ThenInclude(ba => ba.Book)
                .FirstOrDefaultAsync(a => a.AuthorId == id);

            if (author == null) return NotFound();

            return View(author);
        }

        // GET: Author/Create
        [Microsoft.AspNetCore.Authorization.Authorize(Roles = "Manager")]
        public IActionResult Create()
        {
            return View();
        }

        // POST: Author/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Microsoft.AspNetCore.Authorization.Authorize(Roles = "Manager")]
        public async Task<IActionResult> Create([Bind("AuthorId,Name,Biography")] Author author)
        {
            if (ModelState.IsValid)
            {
                _context.Add(author);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }
            return View(author);
        }

        // GET: Author/Edit/5
        [Microsoft.AspNetCore.Authorization.Authorize(Roles = "Manager")]
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null) return NotFound();

            var author = await _context.Authors.FindAsync(id);
            if (author == null) return NotFound();

            return View(author);
        }

        // POST: Author/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Microsoft.AspNetCore.Authorization.Authorize(Roles = "Manager")]
        public async Task<IActionResult> Edit(int id, [Bind("AuthorId,Name,Biography")] Author author)
        {
            if (id != author.AuthorId) return NotFound();

            if (ModelState.IsValid)
            {
                try
                {
                    _context.Update(author);
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!AuthorExists(author.AuthorId)) return NotFound();
                    else throw;
                }
                return RedirectToAction(nameof(Index));
            }
            return View(author);
        }

        // GET: Author/Delete/5
        [Microsoft.AspNetCore.Authorization.Authorize(Roles = "Manager")]
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null) return NotFound();

            // Include the books so the confirmation page can warn about
            // the BookAuthor links that will be removed (cascade delete).
            var author = await _context.Authors
                .Include(a => a.BookAuthors)
                    .ThenInclude(ba => ba.Book)
                .FirstOrDefaultAsync(a => a.AuthorId == id);

            if (author == null) return NotFound();

            return View(author);
        }

        // POST: Author/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        [Microsoft.AspNetCore.Authorization.Authorize(Roles = "Manager")]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var author = await _context.Authors.FindAsync(id);
            if (author != null)
            {
                // No Restrict is configured on BookAuthor->Author, so this
                // cascades and removes the BookAuthor link rows only.
                // The books themselves are not deleted.
                _context.Authors.Remove(author);
                await _context.SaveChangesAsync();
            }

            return RedirectToAction(nameof(Index));
        }

        private bool AuthorExists(int id)
        {
            return _context.Authors.Any(e => e.AuthorId == id);
        }
    }
}
