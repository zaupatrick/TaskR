using TaskR.Data;

namespace TaskR.Models
{
    public class BenutzerBrettVm
    {
        public IEnumerable<AppUser>? Benutzers { get; set; }

        public IEnumerable<AppUser> Admins { get; set; } = null!;

        public IEnumerable<AppUser>? PremiumUsers { get; set; }
        
        public IEnumerable<AppUser>? FreeUsers { get; set; }

    }
}
