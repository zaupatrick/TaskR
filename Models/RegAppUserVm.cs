using System.ComponentModel.DataAnnotations;

namespace TaskR.Models
{
    public class RegAppUserVm
    {
        [Required]
        public string Email { get; set; } = string.Empty;

        public string? Username { get; set; } = string.Empty;
        [Required]
        public string PasswortString { get; set; } = string.Empty;
        [Required]
        public string PasswortString2 { get; set; } = string.Empty;

        public IFormFile? UserImage { get; set; }
    }
}
