using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using Lib_System.Data;
using Lib_System.Models;
using Lib_System.ViewModels;

namespace Lib_System.Controllers
{
    public class UserController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly PasswordHasher<User> _passwordHasher = new();

        public UserController(ApplicationDbContext context)
        {
            _context = context;
        }

        // GET: User
        public async Task<IActionResult> Index()
        {
            var users = await _context.Users
                .Include(u => u.Role)
                .OrderBy(u => u.Name)
                .ToListAsync();

            return View(users);
        }

        // GET: User/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null) return NotFound();

            var user = await _context.Users
                .Include(u => u.Role)
                .Include(u => u.ManagedBooks)
                .Include(u => u.Borrowings)
                    .ThenInclude(b => b.Book)
                .FirstOrDefaultAsync(u => u.UserId == id);

            if (user == null) return NotFound();

            // PasswordHash is intentionally never passed to or shown by the Details view.
            return View(user);
        }

        // GET: User/Create
        public async Task<IActionResult> Create()
        {
            var vm = new UserFormViewModel();
            await PopulateRolesAsync(vm);
            return View(vm);
        }

        // POST: User/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(UserFormViewModel vm)
        {
            if (string.IsNullOrWhiteSpace(vm.Password))
            {
                ModelState.AddModelError(nameof(vm.Password), "Password is required.");
            }

            if (await _context.Users.AnyAsync(u => u.Email == vm.Email))
            {
                ModelState.AddModelError(nameof(vm.Email), "This email is already registered.");
            }

            if (await _context.Users.AnyAsync(u => u.Username == vm.Username))
            {
                ModelState.AddModelError(nameof(vm.Username), "This username is already taken.");
            }

            if (ModelState.IsValid)
            {
                var user = new User
                {
                    Name = vm.Name,
                    Email = vm.Email,
                    Username = vm.Username,
                    Phone = vm.Phone,
                    Address = vm.Address,
                    Status = vm.Status,
                    RoleId = vm.RoleId,
                    RegistrationDate = DateTime.Today,
                    CreatedDate = DateTime.Now
                };

                // Hash the plaintext password entered in the form. The
                // plaintext itself is discarded - only the hash is stored.
                user.PasswordHash = _passwordHasher.HashPassword(user, vm.Password!);

                _context.Add(user);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }

            await PopulateRolesAsync(vm);
            return View(vm);
        }

        // GET: User/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null) return NotFound();

            var user = await _context.Users.FindAsync(id);
            if (user == null) return NotFound();

            var vm = new UserFormViewModel
            {
                UserId = user.UserId,
                Name = user.Name,
                Email = user.Email,
                Username = user.Username,
                Phone = user.Phone,
                Address = user.Address,
                Status = user.Status,
                RoleId = user.RoleId
                // Password intentionally left blank - it means "keep current password" on submit.
            };

            await PopulateRolesAsync(vm);
            return View(vm);
        }

        // POST: User/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, UserFormViewModel vm)
        {
            if (id != vm.UserId) return NotFound();

            // Password is optional here - blank means "don't change it" -
            // so we don't require it, but we do enforce the same min length
            // via [StringLength] on the view model if something was typed.

            if (await _context.Users.AnyAsync(u => u.Email == vm.Email && u.UserId != id))
            {
                ModelState.AddModelError(nameof(vm.Email), "This email is already registered.");
            }

            if (await _context.Users.AnyAsync(u => u.Username == vm.Username && u.UserId != id))
            {
                ModelState.AddModelError(nameof(vm.Username), "This username is already taken.");
            }

            if (ModelState.IsValid)
            {
                var user = await _context.Users.FindAsync(id);
                if (user == null) return NotFound();

                user.Name = vm.Name;
                user.Email = vm.Email;
                user.Username = vm.Username;
                user.Phone = vm.Phone;
                user.Address = vm.Address;
                user.Status = vm.Status;
                user.RoleId = vm.RoleId;

                if (!string.IsNullOrWhiteSpace(vm.Password))
                {
                    user.PasswordHash = _passwordHasher.HashPassword(user, vm.Password);
                }

                try
                {
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!await _context.Users.AnyAsync(u => u.UserId == id)) return NotFound();
                    else throw;
                }

                return RedirectToAction(nameof(Index));
            }

            await PopulateRolesAsync(vm);
            return View(vm);
        }

        // GET: User/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null) return NotFound();

            var user = await _context.Users
                .Include(u => u.Role)
                .Include(u => u.Borrowings)
                .Include(u => u.ManagedBooks)
                .FirstOrDefaultAsync(u => u.UserId == id);

            if (user == null) return NotFound();

            return View(user);
        }

        // POST: User/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var user = await _context.Users
                .Include(u => u.Role)
                .Include(u => u.Borrowings)
                .Include(u => u.ManagedBooks)
                .FirstOrDefaultAsync(u => u.UserId == id);

            if (user == null) return RedirectToAction(nameof(Index));

            if (user.Borrowings.Any())
            {
                ModelState.AddModelError(string.Empty,
                    "This user cannot be deleted because they have borrowing history. Resolve or remove the related borrowings first.");
                return View("Delete", user);
            }

            // Books this user manages are not blocked - Book.ManagerId is
            // configured with SetNull, so those books just lose their manager.
            _context.Users.Remove(user);
            await _context.SaveChangesAsync();

            return RedirectToAction(nameof(Index));
        }

        private async Task PopulateRolesAsync(UserFormViewModel vm)
        {
            vm.Roles = new SelectList(
                await _context.Roles.OrderBy(r => r.RoleName).ToListAsync(),
                "RoleId", "RoleName", vm.RoleId);
        }
    }
}
