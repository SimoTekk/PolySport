using Microsoft.AspNetCore.Identity;

namespace PolySport.Models
{
    public class ApplicationUser : IdentityUser
    {
        public string? FirstName { get; set; }
        public string? LastName { get; set; }
        public string? DisplayName { get; set; }
        public DateTime? DateOfBirth { get; set; }
        public string? ProfileImageUrl { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? LastModifiedAt { get; set; }

        /// <summary>
        /// Freigabe durch einen Admin. Ohne Freigabe ist keine Anmeldung möglich
        /// (siehe AdminApprovalUserConfirmation).
        /// </summary>
        public bool IsApproved { get; set; }
        public DateTime? ApprovedAt { get; set; }
    }
}
