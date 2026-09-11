using Lib_System.Models;
using Lib_System.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Linq;

namespace Lib_System.Controllers
{
    public class AuthorController : Controller
    {
        private readonly IAuthorService _authorService;

        public AuthorController(IAuthorService authorService)
        {
            _authorService = authorService;
        }

        [Authorize(Roles = "Admin,Manager")]
        public async Task<IActionResult> Index()
        {
            return View(await _authorService.GetAllAsync());
        }

        [Authorize(Roles = "Admin,Manager")]
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null) return NotFound();

            var author = await _authorService.GetDetailsAsync(id.Value);
            if (author == null) return NotFound();

            return View(author);
        }

        [Authorize(Roles = "Manager")]
        public IActionResult Create()
        {
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Manager")]
        public async Task<IActionResult> Create([Bind("AuthorId,Name,Biography")] Author author)
        {
            if (ModelState.IsValid)
            {
                await _authorService.CreateAsync(author);
                return RedirectToAction(nameof(Index));
            }
            return View(author);
        }

        [Authorize(Roles = "Manager")]
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null) return NotFound();

            var author = await _authorService.GetForEditAsync(id.Value);
            if (author == null) return NotFound();

            return View(author);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Manager")]
        public async Task<IActionResult> Edit(int id, [Bind("AuthorId,Name,Biography")] Author author)
        {
            if (id != author.AuthorId) return NotFound();

            if (ModelState.IsValid)
            {
                try
                {
                    await _authorService.UpdateAsync(id, author);
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!await _authorService.ExistsAsync(author.AuthorId)) return NotFound();
                    else throw;
                }
                return RedirectToAction(nameof(Index));
            }
            return View(author);
        }

        [Authorize(Roles = "Manager")]
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null) return NotFound();

            var author = await _authorService.GetForDeleteAsync(id.Value);
            if (author == null) return NotFound();

            return View(author);
        }

        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Manager")]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var author = await _authorService.GetForDeleteAsync(id);
            if (author == null) return RedirectToAction(nameof(Index));
            var result = await _authorService.DeleteAsync(id);
            if (!result.Success)
            {
                // Pass the error message(s) to a dedicated blocked-delete page
                TempData["DeleteBlockedMessage"] = string.Join(" ", result.Errors.Select(e => e.Message));
                return RedirectToAction("DeleteBlocked", new { id });
            }
            return RedirectToAction(nameof(Index));
        }

        [Authorize(Roles = "Manager")]
        public async Task<IActionResult> DeleteBlocked(int? id)
        {
            if (id == null) return NotFound();

            var author = await _authorService.GetForDeleteAsync(id.Value);
            if (author == null) return NotFound();

            ViewData["DeleteBlockedMessage"] = TempData["DeleteBlockedMessage"] as string;
            return View("~/Views/Shared/DeleteBlocked.cshtml", author);
        }
    }
}
