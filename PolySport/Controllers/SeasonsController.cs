using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PolySport.Data;
using PolySport.Models;

namespace PolySport.Controllers
{
    public class SeasonsController : Controller
    {
        private readonly ApplicationDbContext _context;

        public SeasonsController(ApplicationDbContext context)
        {
            _context = context;
        }

        // GET: Liste aller Saisons
        public async Task<IActionResult> Index()
        {
            return View(await _context.Seasons.ToListAsync());
        }

        // GET: Formular für neue Saison
        public IActionResult Create()
        {
            return View();
        }

        // POST: Neue Saison speichern
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Season season)
        {
            if (ModelState.IsValid)
            {
                // Wenn die neue Saison aktiv ist, alle anderen auf inaktiv setzen
                if (season.IsActive)
                {
                    var activeSeasons = _context.Seasons.Where(s => s.IsActive);
                    foreach (var s in activeSeasons) s.IsActive = false;
                }

                _context.Seasons.Add(season);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }
            return View(season);
        }
    }
}