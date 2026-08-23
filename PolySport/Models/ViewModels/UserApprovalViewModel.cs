namespace PolySport.Models.ViewModels
{
    public class UserApprovalViewModel
    {
        public string Id { get; set; } = string.Empty;
        public string? Email { get; set; }
        public string? DisplayName { get; set; }
        public string FullName { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }

        public bool IsApproved { get; set; }
        public DateTime? ApprovedAt { get; set; }

        /// <summary>Admins dürfen sich selbst nicht sperren oder löschen.</summary>
        public bool IsCurrentUser { get; set; }
        public bool IsAdmin { get; set; }

        /// <summary>Darf Spiele leiten: Uhr bedienen und Tore erfassen.</summary>
        public bool IsManager { get; set; }

        public string RoleLabel => IsAdmin ? "Admin" : IsManager ? "Manager" : "Mitglied";
    }
}
