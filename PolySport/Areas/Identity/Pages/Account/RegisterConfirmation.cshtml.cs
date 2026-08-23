using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace PolySport.Areas.Identity.Pages.Account
{
    /// <summary>
    /// Überschreibt die Standard-Seite der Identity-UI. Statt "bitte E-Mail bestätigen"
    /// erklärt sie, dass ein Admin das Konto freigeben muss.
    /// </summary>
    [AllowAnonymous]
    public class RegisterConfirmationModel : PageModel
    {
        public string? Email { get; set; }

        public void OnGet(string? email = null)
        {
            Email = email;
        }
    }
}
