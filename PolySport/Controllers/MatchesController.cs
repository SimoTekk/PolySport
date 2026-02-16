using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using PolySport.Data;
using PolySport.Models;
using PolySport.Models.ViewModels;

namespace PolySport.Controllers
{
    public class MatchesController : Controller
    {
        private readonly ApplicationDbContext _context;

        public MatchesController(ApplicationDbContext context)
        {
            _context = context;
        }

        // GET: Matches/Create (Lädt das leere Formular)
        public IActionResult Create()
        {
            var viewModel = new CreateMatchViewModel
            {
                // Lade NUR aktive Saisons für das Dropdown
                AvailableSeasons = _context.Seasons
                    .Where(s => s.IsActive)
                    .Select(s => new SelectListItem { Value = s.Id.ToString(), Text = s.Name })
                    .ToList(),

                // Lade NUR aktive Spieler (Soft-Delete) für die Kader-Auswahl
                AvailablePlayers = _context.Players
                    .Where(u => u.IsActive)
                    .Select(u => new SelectListItem { Value = u.Id.ToString(), Text = u.Username })
                    .ToList()
            };

            return View(viewModel);
        }

        // POST: Matches/Create (Wird beim Klick auf Speichern aufgerufen)
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(CreateMatchViewModel viewModel)
        {
            if (ModelState.IsValid)
            {
                // 1. Das eigentliche Match-Objekt erstellen
                var match = new Match
                {
                    SeasonId = viewModel.SeasonId,
                    OpponentName = viewModel.OpponentName,
                    MatchDate = viewModel.MatchDate,
                    OpponentScore = 0 // Startet bei 0
                };

                _context.Matches.Add(match);

                // Wir speichern das Match zuerst, damit der SQL Server eine Match.Id generiert!
                await _context.SaveChangesAsync();

                // 2. Jetzt die ausgewählten Spieler als Kader (MatchPlayer) verknüpfen
                if (viewModel.SelectedPlayerIds != null && viewModel.SelectedPlayerIds.Any())
                {
                    foreach (var playerId in viewModel.SelectedPlayerIds)
                    {
                        var matchPlayer = new MatchPlayer
                        {
                            MatchId = match.Id,
                            UserId = playerId
                        };
                        _context.MatchPlayers.Add(matchPlayer);
                    }
                    await _context.SaveChangesAsync(); // Kader speichern
                }

                // Nach dem Speichern zurück zur Übersicht (Index) leiten
                return RedirectToAction(nameof(Index));
            }

            // Falls jemand das Formular falsch ausfüllt (z.B. Gegner vergessen):
            // Dropdowns neu laden, bevor die Seite mit Fehlermeldung neu angezeigt wird
            viewModel.AvailableSeasons = _context.Seasons.Where(s => s.IsActive).Select(s => new SelectListItem { Value = s.Id.ToString(), Text = s.Name });
            viewModel.AvailablePlayers = _context.Players.Where(u => u.IsActive).Select(u => new SelectListItem { Value = u.Id.ToString(), Text = u.Username });

            return View(viewModel);
        }

        // GET: Matches/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null) return NotFound();

            // Lade das Match inklusive aller verknüpften Daten aus der Datenbank
            var match = await _context.Matches
                .Include(m => m.Season)
                .Include(m => m.MatchPlayers)
                    .ThenInclude(mp => mp.User) // Lade die User-Daten des Kaders
                .Include(m => m.Goals)
                    .ThenInclude(g => g.Scorer) // Lade den Namen des Torschützen
                .Include(m => m.Goals)
                    .ThenInclude(g => g.Assist) // Lade den Namen des Assist-Gebers
                .FirstOrDefaultAsync(m => m.Id == id);

            if (match == null) return NotFound();

            // Befülle das ViewModel für die saubere Anzeige
            var viewModel = new MatchDetailsViewModel
            {
                MatchId = match.Id,
                SeasonName = match.Season?.Name ?? "Unbekannte Saison",
                OpponentName = match.OpponentName,
                MatchDate = match.MatchDate,
                OpponentScore = match.OpponentScore,
                OurScore = match.Goals.Count, // Hier berechnen wir automatisch eure Tore!

                // Liste der Spielernamen im Kader
                RosterNames = match.MatchPlayers.Select(mp => mp.User!.Username).ToList(),

                // Liste der Tore
                Goals = match.Goals.Select(g => new GoalDisplay
                {
                    ScorerName = g.Scorer!.Username,
                    AssistName = g.Assist?.Username // Null-sicher, falls es keinen Assist gab
                }).ToList()
            };

            return View(viewModel);
        }

        // POST: Matches/AddOpponentGoal/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AddOpponentGoal(int id)
        {
            var match = await _context.Matches.FindAsync(id);
            if (match == null) return NotFound();

            // Gegner-Tore um 1 erhöhen
            match.OpponentScore++;

            await _context.SaveChangesAsync();

            // Zurück zur Detailseite des Matches
            return RedirectToAction(nameof(Details), new { id = match.Id });
        }

        // GET: Matches/Index (Die Übersicht aller Spiele)
        public async Task<IActionResult> Index()
        {
            // Lade alle Matches und inkludiere die Saison-Daten für die Anzeige
            var matches = await _context.Matches
                .Include(m => m.Season)
                .OrderByDescending(m => m.MatchDate) // Neueste Spiele zuerst
                .ToListAsync();

            return View(matches);
        }
    }
}

