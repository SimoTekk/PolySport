using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using PolySport.Models;

namespace PolySport.Areas.Identity.Pages.Account
{
    /// <summary>
    /// Überschreibt die Standard-Login-Seite der Identity-UI, um bei fehlender
    /// Admin-Freigabe (SignInResult.NotAllowed) eine verständliche Meldung zu zeigen
    /// statt "Invalid login attempt".
    /// </summary>
    [AllowAnonymous]
    public class LoginModel : PageModel
    {
        private readonly SignInManager<ApplicationUser> _signInManager;
        private readonly ILogger<LoginModel> _logger;

        public LoginModel(SignInManager<ApplicationUser> signInManager, ILogger<LoginModel> logger)
        {
            _signInManager = signInManager;
            _logger = logger;
        }

        [BindProperty]
        public InputModel Input { get; set; } = new();

        public IList<AuthenticationScheme> ExternalLogins { get; set; } = new List<AuthenticationScheme>();

        public string? ReturnUrl { get; set; }

        [TempData]
        public string? ErrorMessage { get; set; }

        public class InputModel
        {
            [Required(ErrorMessage = "E-Mail ist erforderlich.")]
            [EmailAddress(ErrorMessage = "Keine gültige E-Mail-Adresse.")]
            [Display(Name = "E-Mail")]
            public string Email { get; set; } = string.Empty;

            [Required(ErrorMessage = "Passwort ist erforderlich.")]
            [DataType(DataType.Password)]
            [Display(Name = "Passwort")]
            public string Password { get; set; } = string.Empty;

            [Display(Name = "Angemeldet bleiben?")]
            public bool RememberMe { get; set; }
        }

        public async Task OnGetAsync(string? returnUrl = null)
        {
            if (!string.IsNullOrEmpty(ErrorMessage))
                ModelState.AddModelError(string.Empty, ErrorMessage);

            ReturnUrl = returnUrl;

            // Eventuell hängengebliebenes externes Cookie aufräumen.
            await HttpContext.SignOutAsync(IdentityConstants.ExternalScheme);

            ExternalLogins = (await _signInManager.GetExternalAuthenticationSchemesAsync()).ToList();
        }

        public async Task<IActionResult> OnPostAsync(string? returnUrl = null)
        {
            returnUrl ??= Url.Content("~/");
            ReturnUrl = returnUrl;
            ExternalLogins = (await _signInManager.GetExternalAuthenticationSchemesAsync()).ToList();

            if (!ModelState.IsValid)
                return Page();

            var result = await _signInManager.PasswordSignInAsync(
                Input.Email, Input.Password, Input.RememberMe, lockoutOnFailure: false);

            if (result.Succeeded)
            {
                _logger.LogInformation("Benutzer {Email} hat sich angemeldet.", Input.Email);
                return LocalRedirect(returnUrl);
            }

            if (result.RequiresTwoFactor)
                return RedirectToPage("./LoginWith2fa", new { ReturnUrl = returnUrl, Input.RememberMe });

            if (result.IsLockedOut)
            {
                _logger.LogWarning("Konto {Email} ist gesperrt.", Input.Email);
                return RedirectToPage("./Lockout");
            }

            if (result.IsNotAllowed)
            {
                // Kommt von AdminApprovalUserConfirmation: Konto existiert, ist aber nicht freigegeben.
                ModelState.AddModelError(string.Empty,
                    "Dein Konto wurde noch nicht von einem Administrator freigegeben. Bitte warte auf die Freigabe.");
                return Page();
            }

            ModelState.AddModelError(string.Empty, "E-Mail oder Passwort ist falsch.");
            return Page();
        }
    }
}
