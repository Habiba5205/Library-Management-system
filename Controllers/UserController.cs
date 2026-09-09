using Lib_System.Services.Interfaces;
using Lib_System.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace Lib_System.Controllers
{
    public class UserController : Controller
    {
        private readonly IUserService _userService;

        public UserController(IUserService userService)
        {
            _userService = userService;
        }

        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Index()
        {
            return View(await _userService.GetAllAsync());
        }

        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null) return NotFound();

            var user = await _userService.GetDetailsAsync(id.Value);
            if (user == null) return NotFound();

            // PasswordHash is intentionally never passed to or shown by the Details view.
            return View(user);
        }

        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Create()
        {
            var vm = new UserFormViewModel();
            await PopulateRolesAsync(vm);
            return View(vm);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Create(UserFormViewModel vm)
        {
            var validation = await _userService.ValidateForCreateAsync(vm);
            foreach (var error in validation.Errors)
            {
                ModelState.AddModelError(error.Field, error.Message);
            }

            if (ModelState.IsValid)
            {
                await _userService.CreateAsync(vm);
                return RedirectToAction(nameof(Index));
            }

            await PopulateRolesAsync(vm);
            return View(vm);
        }

        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null) return NotFound();

            var user = await _userService.GetForEditAsync(id.Value);
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

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Edit(int id, UserFormViewModel vm)
        {
            if (id != vm.UserId) return NotFound();

            var validation = await _userService.ValidateForEditAsync(id, vm);
            foreach (var error in validation.Errors)
            {
                ModelState.AddModelError(error.Field, error.Message);
            }

            if (ModelState.IsValid)
            {
                bool updated;
                try
                {
                    updated = await _userService.UpdateAsync(id, vm);
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!await _userService.ExistsAsync(id)) return NotFound();
                    else throw;
                }

                if (!updated) return NotFound();
                return RedirectToAction(nameof(Index));
            }

            await PopulateRolesAsync(vm);
            return View(vm);
        }

        /// <summary>Confirmation page for changing a user's status (replaces hard delete).
        /// Lets an admin move a user to Active, Deactivated, or Suspended.</summary>
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Deactivate(int? id)
        {
            if (id == null) return NotFound();

            var user = await _userService.GetForStatusChangeAsync(id.Value);
            if (user == null) return NotFound();

            return View(user);
        }

        [HttpPost, ActionName("Deactivate")]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> DeactivateConfirmed(int id, string status)
        {
            if (id == GetCurrentUserId())
            {
                ModelState.AddModelError("", "You cannot change the status of your own account.");
                var self = await _userService.GetForStatusChangeAsync(id);
                return self == null ? NotFound() : View("Deactivate", self);
            }

            var result = await _userService.SetStatusAsync(id, status);
            if (!result.Success)
            {
                foreach (var error in result.Errors)
                {
                    ModelState.AddModelError(error.Field, error.Message);
                }
                var user = await _userService.GetForStatusChangeAsync(id);
                return user == null ? NotFound() : View("Deactivate", user);
            }

            return RedirectToAction(nameof(Index));
        }

        /// <summary>Restores a deactivated/suspended account to Active. No confirmation
        /// page needed - this is the safe, reversible direction.</summary>
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Reactivate(int id)
        {
            if (id != GetCurrentUserId())
            {
                await _userService.SetStatusAsync(id, "Active");
            }

            return RedirectToAction(nameof(Index));
        }

        private int GetCurrentUserId() =>
            int.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var id) ? id : 0;

        private async Task PopulateRolesAsync(UserFormViewModel vm)
        {
            var roles = await _userService.GetRolesAsync();
            vm.Roles = new SelectList(roles, "RoleId", "RoleName", vm.RoleId);
        }
    }
}
