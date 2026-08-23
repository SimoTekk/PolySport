using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PolySport.Models;
using PolySport.Models.ViewModels;
using PolySport.Services;

namespace PolySport.Controllers
{
    // Aktualisierungen sind Sache des Admins.
    [Authorize(Roles = AppRoles.Admin)]
    public class UpdateController : Controller
    {
        private readonly IUpdateService _updateService;

        public UpdateController(IUpdateService updateService)
        {
            _updateService = updateService;
        }

        // GET: Update
        public async Task<IActionResult> Index()
        {
            // Immer frisch prüfen: wer diese Seite öffnet, will den aktuellen
            // Stand sehen und nicht das Ergebnis der letzten Hintergrundprüfung.
            var info = await _updateService.CheckAsync();

            return View(new UpdatePageViewModel
            {
                Info = info,
                Status = _updateService.GetStatus(),
                PendingChanges = await _updateService.GetPendingChangesAsync(),
                CanInstall = _updateService.CanInstallUpdates,
                RepositoryUrl = _updateService.RepositoryUrl
            });
        }

        // POST: Update/Install
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Install()
        {
            var info = _updateService.Cached.CheckedAt == null
                ? await _updateService.CheckAsync()
                : _updateService.Cached;

            if (!info.IsUpdateAvailable || string.IsNullOrEmpty(info.LatestVersion))
            {
                TempData["Error"] = "Es liegt keine neuere Version vor.";
                return RedirectToAction(nameof(Index));
            }

            if (!_updateService.CanInstallUpdates)
            {
                TempData["Error"] = "Diese Installation kann sich nicht selbst aktualisieren. "
                    + "Bitte deploy/update.sh auf dem Server ausführen.";
                return RedirectToAction(nameof(Index));
            }

            if (await _updateService.RequestInstallAsync(info.LatestVersion))
                TempData["Success"] = $"Update auf {info.LatestVersion} wurde gestartet.";
            else
                TempData["Error"] = "Das Update konnte nicht angefordert werden. Siehe Protokoll.";

            return RedirectToAction(nameof(Index));
        }

        // POST: Update/ClearStatus – hängengebliebene Meldung wegräumen
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult ClearStatus()
        {
            if (_updateService.ClearStatus())
                TempData["Success"] = "Statusmeldung zurückgesetzt.";
            else
                TempData["Error"] = "Die Statusmeldung konnte nicht entfernt werden.";

            return RedirectToAction(nameof(Index));
        }

        // GET: Update/Status – für die Fortschrittsanzeige per JavaScript
        [HttpGet]
        public IActionResult Status()
        {
            var status = _updateService.GetStatus();
            return Json(new
            {
                state = status.State.ToString().ToLowerInvariant(),
                version = status.Version,
                message = status.Message,
                updatedAt = status.UpdatedAt
            });
        }
    }
}
