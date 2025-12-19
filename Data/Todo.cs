using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace TaskR.Data
{
    public class Todo
    {
        [Key]
        public int TodoId { get; set; }

        [Required]
        [StringLength(50)]
        public string TodoName { get; set; } = null!;

        public DateTime ErstellDatum { get; set; } = DateTime.MinValue;

        public virtual ICollection<Aufgabe> TaskGroup { get; set; } = new List<Aufgabe>();

        [ForeignKey(nameof(AppUser))]
        public int UserId { get; set; }
        public virtual AppUser AppUser { get; set; } = null!;

        [NotMapped]
        public bool AllTasksDone { get; set; } = false;

        [NotMapped]
        public int AnzTasksDone { get; set; } = 0;

        [NotMapped]
        public int AnzTasksUndone { get; set; } = 0;

        [NotMapped]
        public int AnzAllTasks { get; set; } = 0;
        
        [NotMapped]
        public int AnzTasksUrgend { get; set; } = 0;
    }
}
