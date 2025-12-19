using TaskR.Data;

namespace TaskR.Models
{
    public class TodoDetailsVm
    {
        public Todo Todo { get; set; } = null!;

        public IEnumerable<Aufgabe>? Tasks { get; set; }

        public IEnumerable<Aufgabe>? DoneTasks { get; set; }

        public IEnumerable<Tag>? Tags { get; set; }

        public int NumberOfTasksLeft { get; set; }
    }
}
