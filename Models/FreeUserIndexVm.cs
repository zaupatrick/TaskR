using System.Collections;
using System.ComponentModel.DataAnnotations.Schema;
using TaskR.Data;

namespace TaskR.Models
{
    public class FreeUserIndexVm
    {
        public IEnumerable<Todo>? Todos { get; set; }

        public IEnumerable<Tag>? Tags { get; set; }

        public int AnzTasksDone { get; set; }

        public int AnzTasksOpen { get; set; }

        public int AnzAllTasks { get; set; }

        public int NumberTodosLeft { get; set; }

        public string? FilterRadio { get; set; }

    }
}
