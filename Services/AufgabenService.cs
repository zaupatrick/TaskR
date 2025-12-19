using Microsoft.EntityFrameworkCore;
using TaskR.Data;

namespace TaskR.Services
{
    public class AufgabenService
    {
        #region Dependency Injection
        private readonly MyDbContext _ctx;
        private readonly TodoService _todoService;
        public AufgabenService(MyDbContext ctx, TodoService todoService)
        {
            _ctx = ctx;
            _todoService = todoService;
        }
        #endregion

        //Hau
        public async Task HauTaskEiniAsync(Aufgabe task)
        {
            ArgumentNullException.ThrowIfNull(nameof(task));
            
            _ctx.Add(task);
            await _ctx.SaveChangesAsync();
        }
        public async Task HauTaskWegAsync(Aufgabe task)
        {
            ArgumentNullException.ThrowIfNull(task);
            _ctx.Aufgaben.Remove(task);
            await _ctx.SaveChangesAsync();
        }
        public async Task HauDieseTasksWegAsync(IEnumerable<Aufgabe> doneTasks)
        {
            ArgumentNullException.ThrowIfNull(doneTasks);
            _ctx.Aufgaben.RemoveRange(doneTasks);
            await _ctx.SaveChangesAsync();
        }

        //Mach
        public Aufgabe MachErledigtOderUnerledigt(Aufgabe task)
        {
            //Check ob Datum okay
            if (task.Erledigt && task.ErledigtDatum == null)
            {
                task.Erledigt = false;
                task.ErledigtDatum = DateTime.Now;
            }
            //else if (!task.Erledigt && task.ErledigtDatum != null)
            //{
            //    task.ErledigtDatum = null;
            //}
            return task;
        }
        public async Task MachTaskAndersAsync(Aufgabe task)
        {
            ArgumentNullException.ThrowIfNull(task);
            _ctx.Aufgaben.Update(task);
            await _ctx.SaveChangesAsync();
        }

        //Gib
        public async Task<Aufgabe?> GibTaskVonIdAsync(int taskId)
        {
            return await _ctx.Aufgaben
                .Where(t => t.AufgabeId == taskId)
                .Include(t => t.Todo)
                .Include(t => t.Tags)
                .FirstOrDefaultAsync();
        }
        public async Task<IEnumerable<Aufgabe>?> GibAlleTasksVonTodoIdAsync(int todoId)
        {
            return await _ctx.Aufgaben
                .Where(a => a.TodoId == todoId)
                .Include(t => t.Tags)
                .ToListAsync();
        }
        public async Task<IEnumerable<Aufgabe>> GibAlleFertigTasksVonTodoAsync(int todoId)
        {
            return await _ctx.Aufgaben
                .Where(a => a.TodoId == todoId)
                .Where(a => a.Erledigt)
                .OrderByDescending(a => a.ErstelltDatum)
                .ToListAsync();
        }

        //Schau
        public async Task<int> GibAnzahlRestTasks(int todoId, int maxTasks)
        {
            int anzTasks = await GibAnzahlTasksVonTodoIdAsync(todoId);
            return maxTasks - anzTasks;
        }
        public async Task<int> GibAnzahlAlleFertigTasksVonUserAsync(int? userId)
        {
            ArgumentNullException.ThrowIfNull(userId);
            return await _ctx.Aufgaben
                .Where(a => a.UserId == userId)
                .Where(a => a.Erledigt)
                .CountAsync();
        }
        public async Task<int> GibAnzahlAlleOffenTasksVonUserAsync(int? userId)
        {
            ArgumentNullException.ThrowIfNull(userId);
            return await _ctx.Aufgaben
                .Where(a => a.UserId == userId)
                .Where(a => !a.Erledigt)
                .CountAsync();
        }
        public async Task<int> GibAnzahlAlleTasksVonUserAsync(int? userId)
        {
            ArgumentNullException.ThrowIfNull(userId);
            return await _ctx.Aufgaben
                .Where(a => a.UserId == userId)
                .CountAsync();
        }
        public async Task<int> GibAnzahlTasksVonTodoIdAsync(int? todoId)
        {
            ArgumentNullException.ThrowIfNull(todoId);
            return await _ctx.Aufgaben
                .Where(a => a.TodoId == todoId)
                .CountAsync();
        }
        public async Task<int> GibAnzahlMaxTasksVonUserIdAsync(int userId)
        {
            var user = await _ctx.AppUsers
                .Where(u => u.UserId == userId)
                .FirstOrDefaultAsync();
            ArgumentNullException.ThrowIfNull(user);

            int anzTasks = await GibAnzahlAlleTasksVonUserAsync(user.UserId);
            int maxTasks = 0;
            if (user.UserRole.ToString().Equals("FreeTier"))
            {
                maxTasks = 20;
            }
            if (user.UserRole.ToString().Equals("PremiumTier"))
            {
                maxTasks = 1000;
            }
            return maxTasks-anzTasks;
        }


    }
}
