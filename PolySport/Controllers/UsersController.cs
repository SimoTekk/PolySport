using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PolySport.Models;
using PolySport.Models.ViewModels;

namespace PolySport.Controllers
{
    // Benutzerverwaltung ist ausschließlich für Admins.
    [Authorize(Roles = AppRoles.Admin)]
    public class UsersController : Controller
    {
        private readonly UserManager<ApplicationUser> _userManager;

        public UsersController(UserManager<ApplicationUser> userManager)
        {
            _userManager = userManager;
        }

        // GET: Users (Wartende Freigaben zuerst)
        public async Task<IActionResult> Index()
        {
            var users = await _userManager.Users
                .OrderBy(u => u.IsApproved)
                .ThenBy(u => u.CreatedAt)
                .ToListAsync();

            var admins = await _userManager.GetUsersInRoleAsync(AppRoles.Admin);
            var adminIds = admins.Select(a => a.Id).ToHashSet();
            var currentUserId = _userManager.GetUserId(User);

            var viewModel = users.Select(u => new UserApprovalViewModel
            {
                Id = u.Id,
                Email = u.Email,
                DisplayName = u.DisplayName,
                FullName = $"{u.FirstName} {u.LastName}".Trim(),
                CreatedAt = u.CreatedAt,
                IsApproved = u.IsApproved,
                ApprovedAt = u.ApprovedAt,
                IsAdmin = adminIds.Contains(u.Id),
                IsCurrentUser = u.Id == currentUserId
            }).ToList();

            return View(viewModel);
        }

        // POST: Users/Approve/{id} – gibt ein Konto zur Anmeldung frei
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Approve(string id)
        {
            var user = await _userManager.FindByIdAsync(id);
            if (user == null) return NotFound();

            if (!user.IsApproved)
            {
                user.IsApproved = true;
                user.ApprovedAt = DateTime.UtcNow;
                user.LastModifiedAt = DateTime.UtcNow;
                await _userManager.UpdateAsync(user);
            }

            TempData["Success"] = $"{user.Email} ist jetzt freigegeben.";
            return RedirectToAction(nameof(Index));
        }

        // POST: Users/Revoke/{id} – entzieht die Freigabe wieder
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Revoke(string id)
        {
            var user = await _userManager.FindByIdAsync(id);
            if (user == null) return NotFound();

            if (id == _userManager.GetUserId(User))
            {
                TempData["Error"] = "Du kannst dir die eigene Freigabe nicht entziehen.";
                return RedirectToAction(nameof(Index));
            }

            user.IsApproved = false;
            user.ApprovedAt = null;
            user.LastModifiedAt = DateTime.UtcNow;
            await _userManager.UpdateAsync(user);

            // Invalidiert bestehende Cookies, sonst bleibt der User bis zum Ablauf angemeldet.
            await _userManager.UpdateSecurityStampAsync(user);

            TempData["Success"] = $"Freigabe für {user.Email} entzogen.";
            return RedirectToAction(nameof(Index));
        }

        // POST: Users/Delete/{id} – lehnt eine Registrierung endgültig ab
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(string id)
        {
            var user = await _userManager.FindByIdAsync(id);
            if (user == null) return NotFound();

            if (id == _userManager.GetUserId(User))
            {
                TempData["Error"] = "Du kannst dein eigenes Konto hier nicht löschen.";
                return RedirectToAction(nameof(Index));
            }

            if (await _userManager.IsInRoleAsync(user, AppRoles.Admin))
            {
                TempData["Error"] = "Admin-Konten können hier nicht gelöscht werden.";
                return RedirectToAction(nameof(Index));
            }

            var email = user.Email;
            var result = await _userManager.DeleteAsync(user);

            if (result.Succeeded)
                TempData["Success"] = $"Konto {email} wurde gelöscht.";
            else
                TempData["Error"] = $"Löschen fehlgeschlagen: {string.Join(", ", result.Errors.Select(e => e.Description))}";

            return RedirectToAction(nameof(Index));
        }
    }
}
