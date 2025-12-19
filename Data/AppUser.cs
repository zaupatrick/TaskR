using System.ComponentModel.DataAnnotations;
using System.Security.Cryptography.X509Certificates;
using TaskR.Enums;

namespace TaskR.Data
{
    public class AppUser
    {
        [Key]
        public int UserId { get; set; }

        public string? Username { get; set; }

        [Required]
        [EmailAddress]
        public string EMail { get; set; } = null!;
        
        public byte[] PasswordHash { get; set; } = null!;
        
        public byte[] Salt { get; set; } = null!;

        public DateTime RegisteredOn { get; set; }
        
        [EnumDataType(typeof(UserRole))]
        public virtual UserRole UserRole { get; set; }
        
        public DateTime LastLogon { get; set; }

        public List<Todo>? TodoList { get; set; }

    }
}
