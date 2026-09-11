using Lib_System.Models;
using Lib_System.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Linq;

namespace Lib_System.Controllers
{
    public class CategoryController : Controller
    {
        private readonly ICategoryService _categoryService;

        public CategoryController(ICategoryService categoryService)
        {
            _categoryService = categoryService;
        }

        [Authorize(Roles = "Admin,Manager")]
        public async Task<IActionResult> Index()
        {
            return View(await _categoryService.GetAllAsync());
        }

        [Authorize(Roles = "Admin,Manager")]
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null) return NotFound();

            var category = await _categoryService.GetDetailsAsync(id.Value);
            if (category == null) return NotFound();

            return View(category);
        }

        [Authorize(Roles = "Manager")]
        public IActionResult Create()
        {
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Manager")]
        public async Task<IActionResult> Create([Bind("CategoryId,Name,Description")] Category category)
        {
            if (ModelState.IsValid)
            {
                await _categoryService.CreateAsync(category);
                return RedirectToAction(nameof(Index));
            }
            return View(category);
        }

        [Authorize(Roles = "Manager")]
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null) return NotFound();

            var category = await _categoryService.GetForEditAsync(id.Value);
            if (category == null) return NotFound();

            return View(category);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Manager")]
        public async Task<IActionResult> Edit(int id, [Bind("CategoryId,Name,Description")] Category category)
        {
            if (id != category.CategoryId) return NotFound();

            if (ModelState.IsValid)
            {
                try
                {
                    await _categoryService.UpdateAsync(id, category);
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!await _categoryService.ExistsAsync(category.CategoryId)) return NotFound();
                    else throw;
                }
                return RedirectToAction(nameof(Index));
            }
            return View(category);
        }

        [Authorize(Roles = "Manager")]
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null) return NotFound();

            var category = await _categoryService.GetForDeleteAsync(id.Value);
            if (category == null) return NotFound();

            return View(category);
        }

        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Manager")]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var category = await _categoryService.GetForDeleteAsync(id);
            if (category == null) return RedirectToAction(nameof(Index));

            var result = await _categoryService.DeleteAsync(id);
            if (!result.Success)
            {
                TempData["DeleteBlockedMessage"] = string.Join(" ", result.Errors.Select(e => e.Message));
                return RedirectToAction("DeleteBlocked", new { id });
            }

            return RedirectToAction(nameof(Index));
        }

        [Authorize(Roles = "Manager")]
        public async Task<IActionResult> DeleteBlocked(int? id)
        {
            if (id == null) return NotFound();

            var category = await _categoryService.GetForDeleteAsync(id.Value);
            if (category == null) return NotFound();

            ViewData["DeleteBlockedMessage"] = TempData["DeleteBlockedMessage"] as string;
            return View("~/Views/Shared/DeleteBlocked.cshtml", category);
        }
    }
}