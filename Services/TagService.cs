using Microsoft.EntityFrameworkCore;
using TaskR.Data;

namespace TaskR.Services
{
    public class TagService
    {
        #region Dependency Injection
        private readonly MyDbContext _ctx;
        private readonly AufgabenService _aufgabenService;

        public TagService(MyDbContext ctx, AufgabenService aufgabenService)
        {
            _ctx = ctx;
            _aufgabenService = aufgabenService;
        }
        #endregion

        //Gib
        public async Task<IEnumerable<Tag>?> GibAlleUserTagsAsync(int? userId)
        {
            return await _ctx.Tags
                .Where(t => t.Aufgabe.UserId == userId)
                .ToListAsync();
        }
        public IEnumerable<string>? GibTagsVonTaskBeschreibung(string? beschreibung)
        {
            if (string.IsNullOrEmpty(beschreibung))
            {
                return null;
            }
            else
            {
                return beschreibung.Split(" ")
                    .Where(w => w.StartsWith("#"))
                    .Select(w => w.Substring(1))
                    .Where(w => w.Length >= 4 && w.Length <= 20)
                    .Where(word => word.All(c => char.IsLetter(c)))
                    .ToList();
            }
        }
        public async Task<bool> SchauObGibtsTagVonUserAsync(int userId, string tagString)
        {
            return await _ctx.Tags
                 .Where(t => t.Aufgabe.UserId == userId)
                 .Where(t => t.TagName == tagString)
                 .AnyAsync();
        }
        public async Task<IEnumerable<Tag>?> GibAlleTagsVonTodoId(int todoId)
        {
            var tasks = await _aufgabenService.GibAlleTasksVonTodoIdAsync(todoId);
            List<Tag> tags = new();

            if (tasks != null)
            {
                foreach (var task in tasks)
                {
                    if (task.Tags.Any())
                    {
                        tags.AddRange(task.Tags);
                    }
                }

            }
            return tags;
        }
        public async Task<Tag?> GibTagVonIdAsync(int tagId)
        {
            return await _ctx.Tags
                .Where(t => t.TagId == tagId)
                .FirstOrDefaultAsync();
        }
        public async Task<int> GibAnzahlUserTagsAsync(int userId)
        {
            var userTags = await GibAlleUserTagsAsync(userId);
            ArgumentNullException.ThrowIfNull(userTags);
            return userTags.Count();
        }

        //Reinhaun
        public async Task HauTagReinAsync(Tag tag)
        {
            _ctx.Tags.Add(tag);
            await _ctx.SaveChangesAsync();
        }

        //Mach anders
        public async Task MachTagAndersAsync(Tag? tag)
        {
            if (tag != null)
            {
                _ctx.Tags.Update(tag);
                await _ctx.SaveChangesAsync();
            }
        }

        //Weghaun
        public async Task HauTagWegVonIdAsync(int tagId)
        {
            var tag = await _ctx.Tags
                .Where(t => t.TagId == tagId)
                .FirstOrDefaultAsync();
            _ctx.Tags.Remove(tag!);
            await _ctx.SaveChangesAsync();
        }

    }
}
