using Lib_System.Services.Interfaces;
using Lib_System.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using System.Linq;
using System.Security.Claims;

namespace Lib_System.Controllers
{
    public class BookController : Controller
    {
        private readonly IBookService _bookService;
        private readonly IWebHostEnvironment _env;

        public BookController(IBookService bookService, IWebHostEnvironment env)
        {
            _bookService = bookService;
            _env = env;
        }

        [Authorize(Roles = "Admin,Manager,Member")]
        public async Task<IActionResult> Index(string? searchString, int? categoryId, int? authorId)
        {
            bool memberOnly = User.IsInRole("Member");
            var books = await _bookService.GetAllAsync(searchString, categoryId, authorId, memberOnly);

            var categories = await _bookService.GetCategoriesAsync();
            var authors = await _bookService.GetAllAuthorsAsync();

            ViewBag.Categories = new SelectList(categories, "CategoryId", "Name", categoryId);
            ViewBag.Authors = new SelectList(authors, "AuthorId", "Name", authorId);
            ViewBag.SearchString = searchString;

            return View(books);
        }

        [Authorize(Roles = "Admin,Manager,Member")]
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null) return NotFound();

            var book = await _bookService.GetDetailsAsync(id.Value);
            if (book == null) return NotFound();

            if (User.IsInRole("Member") && book.AvailabilityStatus != "Available")
            {
                return Forbid();
            }

            // Provide a return URL so the Details view can navigate back to the caller.
            var referer = Request.Headers["Referer"].ToString();
            ViewData["ReturnUrl"] = !string.IsNullOrEmpty(referer) ? referer : Url.Action("Index");

            return View(book);
        }

        [Authorize(Roles = "Manager")]
        public async Task<IActionResult> Create()
        {
            var vm = new BookFormViewModel();
            await PopulateDropdownsAsync(vm);
            return View(vm);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Manager")]
        public async Task<IActionResult> Create(BookFormViewModel vm)
        {
            // Handle cover upload if provided
            if (vm.CoverUpload != null && vm.CoverUpload.Length > 0 && !vm.RemoveCover)
            {
                try
                {
                    vm.CoverImageUrl = await SaveCoverFileAsync(vm.CoverUpload);
                }
                catch (Exception ex)
                {
                    ModelState.AddModelError(nameof(vm.CoverUpload), ex.Message);
                }
            }
            // Always the logged-in manager - never trust a ManagerId posted from the form.
            vm.ManagerId = GetCurrentUserId();

            var validation = await _bookService.ValidateForCreateAsync(vm);
            foreach (var error in validation.Errors)
            {
                ModelState.AddModelError(error.Field, error.Message);
            }

            if (ModelState.IsValid)
            {
                await _bookService.CreateAsync(vm);
                return RedirectToAction(nameof(Index));
            }

            await PopulateDropdownsAsync(vm);
            return View(vm);
        }

        [Authorize(Roles = "Manager")]
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null) return NotFound();

            var book = await _bookService.GetForEditAsync(id.Value);
            if (book == null) return NotFound();

            var vm = new BookFormViewModel
            {
                BookId = book.BookId,
                HasCover = !string.IsNullOrEmpty(book.CoverImageUrl) || book.CoverImage != null,
                CoverImageUrl = book.CoverImageUrl,
                ISBN = book.ISBN,
                Title = book.Title,
                PublicationYear = book.PublicationYear,
                Price = book.Price,
                AvailabilityStatus = book.AvailabilityStatus,
                CategoryId = book.CategoryId,
                ManagerId = book.ManagerId,
                ManagerName = book.Manager?.Name ?? "Unassigned",
                SelectedAuthorIds = book.BookAuthors.Select(ba => ba.AuthorId).ToList()
            };

            await PopulateDropdownsAsync(vm);
            return View(vm);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Manager")]
        public async Task<IActionResult> Edit(int id, BookFormViewModel vm)
        {
            if (id != vm.BookId) return NotFound();

            var currentBook = await _bookService.GetForEditAsync(id);
            if (currentBook == null) return NotFound();

            // Keep track of the previous cover URL (if any) so we can delete the file after a successful update.
            string? previousCover = currentBook.CoverImageUrl;
            vm.HasCover = !string.IsNullOrEmpty(previousCover) || currentBook.CoverImage != null;

            // Handle cover removal or replacement. Save replacement files immediately so the service gets the new URL.
            if (vm.RemoveCover)
            {
                vm.CoverImageUrl = null;
            }
            else if (vm.CoverUpload != null && vm.CoverUpload.Length > 0)
            {
                try
                {
                    vm.CoverImageUrl = await SaveCoverFileAsync(vm.CoverUpload);
                }
                catch (Exception ex)
                {
                    ModelState.AddModelError(nameof(vm.CoverUpload), ex.Message);
                }
            }

            var validation = await _bookService.ValidateForEditAsync(id, vm);
            foreach (var error in validation.Errors)
            {
                ModelState.AddModelError(error.Field, error.Message);
            }

            if (ModelState.IsValid)
            {
                bool updated;
                try
                {
                    updated = await _bookService.UpdateAsync(id, vm);
                }
                catch (InvalidOperationException ex)
                {
                    ModelState.AddModelError(nameof(vm.AvailabilityStatus), ex.Message);
                    await PopulateDropdownsAsync(vm);
                    return View(vm);
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!await _bookService.ExistsAsync(id)) return NotFound();
                    else throw;
                }

                if (!updated) return NotFound();

                // If a previous cover existed and the URL changed (replacement or removal), delete the old file.
                if (!string.IsNullOrEmpty(previousCover) && previousCover != vm.CoverImageUrl)
                {
                    TryDeleteCoverFile(previousCover);
                }

                return RedirectToAction(nameof(Index));
            }

            await PopulateDropdownsAsync(vm);
            return View(vm);
        }

        [Authorize(Roles = "Manager")]
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null) return NotFound();

            var book = await _bookService.GetForDeleteAsync(id.Value);
            if (book == null) return NotFound();

            return View(book);
        }

        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Manager")]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var book = await _bookService.GetForDeleteAsync(id);
            if (book == null) return RedirectToAction(nameof(Index));

            var result = await _bookService.DeleteAsync(id);
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

            var book = await _bookService.GetForDeleteAsync(id.Value);
            if (book == null) return NotFound();

            ViewData["DeleteBlockedMessage"] = TempData["DeleteBlockedMessage"] as string;
            return View("~/Views/Shared/DeleteBlocked.cshtml", book);
        }

        private async Task PopulateDropdownsAsync(BookFormViewModel vm)
        {
            var categories = await _bookService.GetCategoriesAsync();
            vm.Categories = new SelectList(categories, "CategoryId", "Name", vm.CategoryId);

            vm.AllAuthors = await _bookService.GetAllAuthorsAsync();
        }

        [Authorize(Roles = "Admin,Manager,Member")]
        [ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
        public async Task<IActionResult> Cover(int id)
        {
            var book = await _bookService.GetForEditAsync(id);
            if (book?.CoverImage == null) return NotFound();
            Response.Headers.XContentTypeOptions = "nosniff";
            return File(book.CoverImage, "image/png");
        }

        private async Task PrepareCoverAsync(BookFormViewModel vm)
        {
            if (vm.CoverUpload == null) return;
            if (vm.RemoveCover)
            {
                ModelState.AddModelError(nameof(vm.CoverUpload), "Choose either a replacement image or removal of the current cover.");
                return;
            }
            try
            {
                vm.PreparedCover = await Lib_System.Services.BookCoverProcessor.PrepareAsync(vm.CoverUpload);
            }
            catch (ArgumentException ex)
            {
                ModelState.AddModelError(nameof(vm.CoverUpload), ex.Message);
            }
        }

        private int GetCurrentUserId()
        {
            var value = User.FindFirstValue(ClaimTypes.NameIdentifier);
            return int.TryParse(value, out var userId) ? userId : 0;
        }

        private async Task<string> SaveCoverFileAsync(IFormFile file)
        {
            var allowed = new[] { ".jpg", ".jpeg", ".png", ".webp" };
            var ext = Path.GetExtension(file.FileName).ToLowerInvariant();
            if (!allowed.Contains(ext)) throw new InvalidOperationException("Unsupported image format.");

            // Use the BookCoverProcessor to normalize/resize and encode as PNG.
            byte[] imageBytes;
            try
            {
                imageBytes = await Lib_System.Services.BookCoverProcessor.PrepareAsync(file);
            }
            catch
            {
                // Fall back to saving the original stream if processing fails for any reason.
                var fallbackName = $"{Guid.NewGuid()}{ext}";
                var fallbackPath = Path.Combine(_env.WebRootPath ?? "wwwroot", "images", "covers", fallbackName);
                Directory.CreateDirectory(Path.GetDirectoryName(fallbackPath) ?? "");
                using (var stream = System.IO.File.Create(fallbackPath))
                {
                    await file.CopyToAsync(stream);
                }
                return $"/images/covers/{fallbackName}";
            }

            var uploads = Path.Combine(_env.WebRootPath ?? "wwwroot", "images", "covers");
            Directory.CreateDirectory(uploads);
            var outName = $"{Guid.NewGuid()}.png";
            var outFull = Path.Combine(uploads, outName);
            await System.IO.File.WriteAllBytesAsync(outFull, imageBytes);
            return $"/images/covers/{outName}";
        }

        private void TryDeleteCoverFile(string? url)
        {
            try
            {
                if (string.IsNullOrEmpty(url)) return;
                // url expected like /images/covers/<file>
                var name = url.Replace("/", Path.DirectorySeparatorChar.ToString()).TrimStart(Path.DirectorySeparatorChar);
                var full = Path.Combine(_env.WebRootPath ?? "wwwroot", name);
                if (System.IO.File.Exists(full)) System.IO.File.Delete(full);
            }
            catch { /* best effort only */ }
        }
    }
}
