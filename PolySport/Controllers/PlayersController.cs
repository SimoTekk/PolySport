using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PolySport.Data;
using PolySport.Models;

namespace PolySport.Controllers
{
    [Authorize]
    public class PlayersController : Controller
    {
        private readonly ApplicationDbContext _context;

        public PlayersController(ApplicationDbContext context)
        {
            _context = context;
        }

        // GET: Liste aller Spieler – aktive zuerst
        public async Task<IActionResult> Index()
        {
            return View(await _context.Players
                .OrderByDescending(p => p.IsActive)
                .ThenBy(p => p.Username)
                .ToListAsync());
        }

        // POST: Players/SetActive/5 – Spieler aktiv oder inaktiv setzen.
        // Inaktive Spieler bleiben in allen bisherigen Kadern und Toren erhalten,
        // sie stehen nur für neue Matches nicht mehr zur Auswahl (Soft-Delete).
        [HttpPost]
        [Authorize(Roles = AppRoles.Admin)]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SetActive(int id, bool active)
        {
            var player = await _context.Players.FindAsync(id);
            if (player == null) return NotFound();

            if (player.IsActive == active)
            {
                TempData["Success"] = $"{player.Username} ist bereits {(active ? "aktiv" : "inaktiv")}.";
                return RedirectToAction(nameof(Index));
            }

            player.IsActive = active;
            await _context.SaveChangesAsync();

            TempData["Success"] = active
                ? $"{player.Username} ist wieder aktiv und kann für Matches aufgestellt werden."
                : $"{player.Username} ist inaktiv und erscheint bei neuen Matches nicht mehr in der Kader-Auswahl.";

            return RedirectToAction(nameof(Index));
        }

        // GET: Formular für neuen Spieler
        [Authorize(Roles = AppRoles.Admin)]
        public IActionResult Create()
        {
            return View(new User { IsActive = true });
        }

        // POST: Neuen Spieler speichern
        [HttpPost]
        [Authorize(Roles = AppRoles.Admin)]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(User player)
        {
            if (ModelState.IsValid)
            {
                _context.Players.Add(player);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }
            return View(player);
        }
    }
}