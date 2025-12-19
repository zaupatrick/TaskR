using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace TaskR.Data
{
    public class Aufgabe
    {
        [Key]
        public int AufgabeId { get; set; }

        [Required]
        [StringLength(100)]
        public string Beschreibung { get; set; } = null!;

        [ForeignKey("Todo")]
        public int TodoId { get; set; }
        public virtual Todo Todo { get; set; } = null!;

        public int UserId { get; set; }

        public bool Erledigt { get; set; } = false;

        public DateTime ErstelltDatum { get; set; }

        public DateTime? ErledigtDatum { get; set; }

        public DateTime? Deadline { get; set; }

        public virtual ICollection<Tag> Tags { get; set; } = new List<Tag>();

        public int Priorität { get; set; } = 0;

    }
}
