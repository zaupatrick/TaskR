using Microsoft.EntityFrameworkCore;
using TaskR.Data;

namespace TaskR.Services
{
    public class TodoService
    {
        private readonly MyDbContext _ctx;
        public TodoService(MyDbContext ctx)
        {
            _ctx = ctx;
        }

        //Einihaun
        public async Task HauTodoEiniAsync(int userId, string todoName)
        {
            Todo todo = new()
            {
                UserId = userId,
                TodoName = todoName,
                ErstellDatum = DateTime.Now,
            };

            _ctx.Todos.Add(todo);
            await _ctx.SaveChangesAsync();
        }

        //Gibst du
        public async Task<IEnumerable<Todo>> GibAlleTodosAsync()
        {
            return await _ctx.Todos
                .Include(t => t.TaskGroup)
                .Include(t => t.AppUser)
                .ToListAsync();
        }
        public async Task<Todo?> GibTodoVonIdAsync(int todoId)
        {
            var todo = await _ctx.Todos
                .Where(t => t.TodoId == todoId)
                .Include(t => t.TaskGroup)
                .FirstOrDefaultAsync();

            if (todo?.TaskGroup != null)
            {
                var allTodoTasks = todo.TaskGroup.ToList();

                var todoDone = allTodoTasks
                    .Where(t => t.Erledigt)
                    .OrderByDescending(t => t.ErstelltDatum)
                    .ThenByDescending(t => t.ErledigtDatum)
                    .ToList();
                var todoOpen = allTodoTasks
                    .Where(t => !t.Erledigt)
                    .OrderByDescending(t => t.Priorität)
                    .ThenBy(t => t.Deadline)
                    .ThenBy(t => t.ErstelltDatum)
                    .ToList();

                allTodoTasks = todoOpen;
                allTodoTasks.AddRange(todoDone);
                todo.TaskGroup = allTodoTasks;
            }
            return todo;
        }
        public async Task<Todo?> GibTodoVonNameAsync(string name)
        {
            return await _ctx.Todos
                .Where(t => t.TodoName == name)
                .FirstOrDefaultAsync();
        }

        public async Task<IEnumerable<Todo>> GibAlleUserTodosAsync(int? userId)
        {
            var todos =  await _ctx.Todos
                .Include(t => t.TaskGroup)
                .Include(t => t.AppUser)
                .Where(t => t.UserId == userId)
                .OrderByDescending(t => t.ErstellDatum)
                .ToListAsync();

            foreach(var todo in todos)
            {
                if (todo?.TaskGroup != null)
                {
                    var allTodoTasks = todo.TaskGroup.ToList();

                    var todoOpen = allTodoTasks
                        .Where(t => !t.Erledigt)
                        .OrderByDescending(t => t.Priorität)
                        .ThenBy(t => t.Deadline)
                        .ThenBy(t => t.ErstelltDatum)
                        .ToList();

                    var todoDone = allTodoTasks
                        .Where(t => t.Erledigt)
                        .OrderByDescending(t => t.ErledigtDatum)
                        .ThenByDescending(t => t.ErstelltDatum)
                        .ToList();

                    allTodoTasks = todoOpen;
                    allTodoTasks.AddRange(todoDone);
                    todo.TaskGroup = allTodoTasks;

                    //Hilfsflag für Sortierung
                    todo.AllTasksDone = !todo.TaskGroup.Any(t => !t.Erledigt);
                } else
                {
                    todo!.AllTasksDone = false;
                }
            }

            //Sortierung der Todo-Listen zur Anzeige
            todos = todos
                .OrderBy(t => t.AllTasksDone)                  // false (nicht alle erledigt) vor true (alle erledigt)
                .ThenByDescending(t => t.ErstellDatum)         // innerhalb der Gruppen nach ErstellDatum
                .ToList();

            return todos;
        }
        public async Task<IEnumerable<Todo>> GibAlleFilterTodosAsync(int? userId, string? searchTaskQuery, string? searchTodoQuery, string? radioFilter)
        {
            //Query-Basis holen
            var query = _ctx.Todos
                .Include(t => t.AppUser)
                .Include(t => t.TaskGroup)
                .AsQueryable();

            //Alle User-Todos
            if (userId.HasValue) { query = query.Where(t => t.UserId == userId); }

            //Suchkriterien queryn
            if (!string.IsNullOrWhiteSpace(searchTodoQuery))
            {
                query = query
                    .Where(t => t.TodoName.ToLower().Contains(searchTodoQuery.ToLower()));
            }
            if (!string.IsNullOrWhiteSpace(searchTaskQuery))
            {
                query = query
                    .Where(t => t.TaskGroup
                        .Any(tg => tg.Beschreibung.ToLower().Contains(searchTaskQuery.ToLower())));
            }
            switch (radioFilter)
            {
                case "AllDone":
                    query = query
                        .Where(t => t.TaskGroup
                            .Any(tg => tg.Erledigt));
                    break;
                case "AllUndone":
                    query = query
                        .Where(t => t.TaskGroup
                            .Any(tg => !tg.Erledigt));
                    break;
                case "Urgent":
                    query = query
                        .Where(t => t.TaskGroup
                            .Any(tg => tg.Deadline <= DateTime.Now.AddDays(7) && !tg.Erledigt));
                    break;
            }

            //Query ausführen - Holen
            var todos = await query
                .OrderByDescending(t => t.ErstellDatum)
                .ToListAsync();

            //Sortieren
            foreach (var todo in todos)
            {
                if (todo?.TaskGroup != null)
                {
                    var orderedOpen = todo.TaskGroup
                        .Where(t => !t.Erledigt)
                        .OrderByDescending(t => t.Priorität)
                        .ThenBy(t => t.Deadline)
                        .ThenBy(t => t.ErstelltDatum)
                        .ToList();

                    var orderedDone = todo.TaskGroup
                        .Where(t => t.Erledigt)
                        .OrderByDescending(t => t.ErledigtDatum)
                        .ThenByDescending(t => t.ErstelltDatum)
                        .ToList();

                    todo.TaskGroup = orderedOpen.Concat(orderedDone).ToList();
                    todo.AllTasksDone = !todo.TaskGroup.Any(t => !t.Erledigt);
                }
                else
                {
                    todo!.AllTasksDone = false;
                }
            }

            // Gesamtsortierung
            return todos
                .OrderBy(t => t.AllTasksDone)
                .ThenByDescending(t => t.ErstellDatum)
                .ToList();

            //Obsolet!
            //List<Todo>? todos = new();
            //bool? erledigt = null;
            //bool? dringend = null;

            ////Was sagt Radio?
            //if (radioFilter != null)
            //{
            //    if (radioFilter == "AllDone") { erledigt = true; }
            //    else if (radioFilter == "AllUndone") { erledigt = false; }
            //    else { erledigt = null; }

            //    if (radioFilter == "Urgent") { dringend = true; }
            //    else { dringend = false; }

            //}

            ////(Un-)Erledigt mit/ohne Suchterm
            //if (searchTaskQuery == null && searchTodoQuery == null)
            //{
            //    todos = await _ctx.Todos
            //        .Include(t => t.TaskGroup
            //            .Where(tg => tg.Erledigt == erledigt))
            //        .Include(t => t.AppUser)
            //        .Where(t => t.UserId == userId)
            //        .OrderByDescending(t => t.ErstellDatum)
            //        .ToListAsync();
            //}
            //if (!string.IsNullOrEmpty(searchTaskQuery))
            //{
            //    todos = await _ctx.Todos
            //        .Include(t => t.TaskGroup
            //            .Where(tg => tg.Beschreibung.ToLower().Contains(searchTaskQuery.ToLower())))
            //        .Include(t => t.AppUser)
            //        .Where(t => t.UserId == userId)
            //        .OrderByDescending(t => t.ErstellDatum)
            //        .ToListAsync();

            //    if (radioFilter != null)
            //    {
            //        var destilled = todos
            //            .Where(t => t.TaskGroup.Any(tg => tg.Erledigt == erledigt))
            //            .ToList();
            //        todos = destilled;
            //    }
            //}
            //if (!string.IsNullOrEmpty(searchTodoQuery))
            //{
            //    if (string.IsNullOrEmpty(searchTaskQuery))
            //    {
            //        todos = await _ctx.Todos
            //            .Include(t => t.TaskGroup
            //                .Where(tg => tg.Erledigt == erledigt))
            //            .Include(t => t.AppUser)
            //            .Where(t => t.UserId == userId && t.TodoName.ToLower().Contains(searchTodoQuery!.ToLower()) )
            //            .OrderByDescending(t => t.ErstellDatum)
            //            .ToListAsync();

            //    } else
            //    {
            //        todos = await _ctx.Todos
            //            .Include(t => t.TaskGroup
            //                .Where(tg => tg.Beschreibung.ToLower().Contains(searchTaskQuery.ToLower()) ))
            //            .Include(t => t.AppUser)
            //            .Where(t => t.UserId == userId && t.TodoName.ToLower().Contains(searchTodoQuery!.ToLower()))
            //            .OrderByDescending(t => t.ErstellDatum)
            //            .ToListAsync();

            //        if (radioFilter != null)
            //        {
            //            var destilled = todos
            //                .Where(t => t.TaskGroup.Any(tg => tg.Erledigt == erledigt))
            //                .ToList();
            //            todos = destilled;
            //        }
            //    }
            //}

            ////Sortierung
            //foreach (var todo in todos)
            //    {
            //        if (todo?.TaskGroup != null)
            //        {
            //            var allTodoTasks = todo.TaskGroup.ToList();

            //            var todoOpen = allTodoTasks
            //                .Where(t => !t.Erledigt)
            //                .OrderByDescending(t => t.Priorität)
            //                .ThenBy(t => t.Deadline)
            //                .ThenBy(t => t.ErstelltDatum)
            //                .ToList();

            //            var todoDone = allTodoTasks
            //                .Where(t => t.Erledigt)
            //                .OrderByDescending(t => t.ErledigtDatum)
            //                .ThenByDescending(t => t.ErstelltDatum)
            //                .ToList();

            //            allTodoTasks = todoOpen;
            //            allTodoTasks.AddRange(todoDone);
            //            todo.TaskGroup = allTodoTasks;

            //            //Hilfsflag für Sortierung
            //            todo.AllTasksDone = !todo.TaskGroup.Any(t => !t.Erledigt);
            //        }
            //        else
            //        {
            //            todo!.AllTasksDone = false;
            //        }
            //    }

            ////Sortierung der Todo-Listen zur Anzeige
            //todos = todos
            //    .OrderBy(t => t.AllTasksDone)                  // false (nicht alle erledigt) vor true (alle erledigt)
            //    .ThenByDescending(t => t.ErstellDatum)         // innerhalb der Gruppen nach ErstellDatum
            //    .ToList();

            //return todos;
        }

        //Machst du anders
        public async Task MachTodoAndersAsync(Todo todo)
        {
            _ctx.Todos.Update(todo);
            await _ctx.SaveChangesAsync();
        }

        //Weghaun
        public async Task HauTodoWegAsync(Todo todo)
        {
            _ctx.Todos.Remove(todo);
            await _ctx.SaveChangesAsync();
        }

        //Zeig her
        public async Task<bool> SchauObAnzTodosGutAsync(int maxTodos, int userId)
        {
            int nrTodos = await _ctx.Todos
                .Where(t => t.UserId == userId)
                .CountAsync();

            if (nrTodos >= maxTodos)
            {
                return false;
            }
            else
            {
                return true;
            }
        }

        public async Task<int> GibAnzahlRestTodosAsync(int maxTodos, int userId)
        {
            return maxTodos - await _ctx.Todos
                .Where(_ctx => _ctx.UserId == userId)
                .CountAsync();
        }

    }
}
