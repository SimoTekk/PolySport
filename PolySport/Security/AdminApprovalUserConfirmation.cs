using Microsoft.AspNetCore.Identity;
using PolySport.Models;

namespace PolySport.Security
{
    /// <summary>
    /// Ersetzt die E-Mail-Bestätigung durch eine Admin-Freigabe.
    /// Identity fragt diese Klasse in <see cref="SignInManager{TUser}.CanSignInAsync"/> ab,
    /// solange SignIn.RequireConfirmedAccount aktiv ist. Ein Konto ohne Freigabe
    /// bekommt bei der Anmeldung SignInResult.NotAllowed.
    /// </summary>
    public class AdminApprovalUserConfirmation : IUserConfirmation<ApplicationUser>
    {
        public Task<bool> IsConfirmedAsync(UserManager<ApplicationUser> manager, ApplicationUser user)
            => Task.FromResult(user.IsApproved);
    }
}
