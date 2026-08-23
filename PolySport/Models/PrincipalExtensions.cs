using System.Security.Claims;

namespace PolySport.Models
{
    public static class PrincipalExtensions
    {
        /// <summary>
        /// Darf ein Spiel leiten: Spieluhr bedienen, Tore erfassen und
        /// korrigieren, Match beenden. Gilt für Admins und Manager.
        /// </summary>
        public static bool CanManageMatches(this ClaimsPrincipal user)
            => user.IsInRole(AppRoles.Admin) || user.IsInRole(AppRoles.Manager);

        /// <summary>Darf verwalten: Matches anlegen, Saisons, Spieler, Benutzer.</summary>
        public static bool IsAdmin(this ClaimsPrincipal user)
            => user.IsInRole(AppRoles.Admin);
    }
}
