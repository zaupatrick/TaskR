using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace TaskR.Data
{
    public class Tag
    {
        [Key]
        public int TagId { get; set; }

        [Required]
        [StringLength(10)]
        public string TagName { get; set; } = null!;

        public string? ColorCode { get; set; }

        [ForeignKey("Task")]
        public int AufgabeId { get; set; }
        public virtual Aufgabe Aufgabe { get; set; } = null!;
    }
}
