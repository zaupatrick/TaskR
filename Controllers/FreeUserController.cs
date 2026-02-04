using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.IdentityModel.Tokens;
using System.Drawing;
using System.Security.Claims;
using TaskR.Data;
using TaskR.Models;
using TaskR.Services;

namespace TaskR.Controllers
{
    [Authorize(Roles = "FreeTier, PremiumTier")] //"None" removed
    public class FreeUserController : Controller
    {

        #region Dependency Injection
        private readonly AufgabenService _aufgabenService;
        private readonly TodoService _todoService;
        private readonly TagService _tagService;
        private readonly AuthService _authService;

        public FreeUserController(AufgabenService aufgabenService, TodoService todoService, TagService tagService, AuthService authService)
        {
            _aufgabenService = aufgabenService;
            _todoService = todoService;
            _tagService = tagService;
            _authService = authService;
        }
        #endregion

        #region Todos
        // Herzeigen
        [HttpGet]
        public async Task<IActionResult> Index()
        {
            //Todos von Bekannten laden
            var userName = User.Identity?.Name;
            //ArgumentNullException.ThrowIfNull(userName);
#pragma warning disable CS8604 // Mögliches Nullverweisargument.
            var userId = await _authService.GibUserIdVonNameAsync(userName);
#pragma warning restore CS8604 // Mögliches Nullverweisargument.
            //Maximale Todos anhand der Benutzerrolle festlegen
            int maxTodos = 0;
            if (User.IsInRole("FreeTier")) { maxTodos = 5; }
            if (User.IsInRole("PremiumTier")) { maxTodos = 100; }

            if (userId != null)
            {
                FreeUserIndexVm vm = new()
                {
                    Todos = await _todoService.GibAlleUserTodosAsync(userId),
                    Tags = await _tagService.GibAlleUserTagsAsync(userId),
                    AnzTasksDone = await _aufgabenService.GibAnzahlAlleFertigTasksVonUserAsync(userId),
                    AnzTasksOpen = await _aufgabenService.GibAnzahlAlleOffenTasksVonUserAsync(userId),
                    AnzAllTasks = await _aufgabenService.GibAnzahlAlleTasksVonUserAsync(userId),
                    NumberTodosLeft = await _todoService.GibAnzahlRestTodosAsync(maxTodos, (int)userId)
                };
                return View(vm);
                //var todos = await _todoService.GibAlleUserTodosAsync(userId);
                //return View(todos);
            }

            //Todos von Unbekannten laden
            var email = User.Claims.FirstOrDefault(claim => claim.Type.Contains(ClaimTypes.Email));
            if (email != null)
            {
                userId = await _authService.GibUserIdVonEmailAsync(email); //Anonymer Benutzer sieht alle Anonymen Einträge

                FreeUserIndexVm vm = new()
                {
                    Todos = await _todoService.GibAlleUserTodosAsync(userId),
                    Tags = await _tagService.GibAlleUserTagsAsync(userId),
                    AnzTasksDone = await _aufgabenService.GibAnzahlAlleFertigTasksVonUserAsync(userId),
                    AnzTasksOpen = await _aufgabenService.GibAnzahlAlleOffenTasksVonUserAsync(userId),
                    AnzAllTasks = await _aufgabenService.GibAnzahlAlleTasksVonUserAsync(userId),
                    NumberTodosLeft = await _todoService.GibAnzahlRestTodosAsync(maxTodos, (int)userId)
                };

                return View(vm);
            }
            else
            {
                TempData["Message"] = "Zugriff verweigert! Bitte melden Sie sich an!";
                return RedirectToAction("Logon", "Auth");
            }
        }
        [HttpPost]
        public async Task<IActionResult> Index(string? searchTaskQuery, string? searchTodoQuery, string? filterRadio)
        {
            if (!string.IsNullOrEmpty(searchTodoQuery) || !string.IsNullOrEmpty(filterRadio) || !string.IsNullOrEmpty(searchTaskQuery))
            {
                //Todos von Unbekannten laden
                int maxTodos = 0;
                if (User.IsInRole("FreeTier")) { maxTodos = 5; }
                if (User.IsInRole("PremiumTier")) { maxTodos = 100; }
                var email = User.Claims.FirstOrDefault(claim => claim.Type.Contains(ClaimTypes.Email));
                int? userId;

                if (email != null)
                {
                    userId = await _authService.GibUserIdVonEmailAsync(email); //Anonymer Benutzer sieht alle Anonymen Einträge

                    FreeUserIndexVm vm = new()
                    {
                        Todos = await _todoService.GibAlleFilterTodosAsync(userId, searchTaskQuery, searchTodoQuery, filterRadio),
                        Tags = await _tagService.GibAlleUserTagsAsync(userId),
                        AnzTasksDone = await _aufgabenService.GibAnzahlAlleFertigTasksVonUserAsync(userId),
                        AnzTasksOpen = await _aufgabenService.GibAnzahlAlleOffenTasksVonUserAsync(userId),
                        AnzAllTasks = await _aufgabenService.GibAnzahlAlleTasksVonUserAsync(userId),
                        NumberTodosLeft = await _todoService.GibAnzahlRestTodosAsync(maxTodos, (int)userId)
                        
                    };

                    ////todo: Seitliche Summen pro Todo anpassen
                    //foreach (var todo in vm.Todos)
                    //{
                    //    var alleTodoTasks = await _aufgabenService.GibAlleTasksVonTodoIdAsync(todo.TodoId);
                    //    if (alleTodoTasks != null)
                    //    {
                    //        todo.AnzAllTasks = alleTodoTasks
                    //            .Count();
                    //        todo.AnzTasksDone = alleTodoTasks
                    //            .Where(t => t.Erledigt)
                    //            .Count();
                    //        todo.AnzTasksUndone = alleTodoTasks
                    //            .Where(t => !t.Erledigt)
                    //            .Count();
                    //        todo.AnzTasksUrgend = alleTodoTasks
                    //            .Count();
                    //    }
                    //}

                    return View(vm);
                }
                else
                {
                    TempData["Message"] = "Zugriff verweigert! Bitte melden Sie sich an!";
                    return RedirectToAction("Logon", "Auth");
                }
            }
            return RedirectToAction("Index", new { searchTodoQuery,  filterRadio });
        }

        //Reinhauen
        [HttpPost]
        public async Task<IActionResult> CreateTodo(string todoName)
        {
            try
            {
                if (string.IsNullOrEmpty(todoName))
                {
                    throw new ArgumentNullException(nameof(todoName));
                }
                else
                {
                    ArgumentNullException.ThrowIfNull(User.Identity);
                    if (User.Identity.IsAuthenticated)
                    {
                        //UserId des angemeldeten Benutzers abfragen
                        var userName = User.Identity.Name;
                        int? userId;

                        if (userName != null)
                        {
                            userId = await _authService.GibUserIdVonNameAsync(userName);
                        } else
                        {
                            var email = User.Claims.FirstOrDefault(claim => claim.Type.Contains(ClaimTypes.Email));
                            userId = await _authService.GibUserIdVonEmailAsync(email!);
                        }

                        if (userId != null)
                        {
                            //Maximale Todos anhand der Benutzerrolle festlegen
                            int maxTodos = 0;
                            if (User.IsInRole("FreeTier")) { maxTodos = 5; }
                            if (User.IsInRole("PremiumTier")) { maxTodos = 100; }

                            //Anzahl der Todos überprüfen
                            if (await _todoService.SchauObAnzTodosGutAsync(maxTodos, (int)userId))
                            {
                                await _todoService.HauTodoEiniAsync((int)userId, todoName);
                                int restTodos = await _todoService.GibAnzahlRestTodosAsync(maxTodos, (int)userId);

                                TempData["Message"] += $"ToDo-Liste wurde gespeichert. Sie koennen noch {restTodos} ToDo-Listen speichern. ";
                                var todo = await _todoService.GibTodoVonNameAsync(todoName);

                                return RedirectToAction("TodoDetails", new { todo!.TodoId }); //Weiterleitung funktioniert nicht (ID?)
                            }
                            else
                            {
                                //Anzahl max. ToDos passt nicht
                                TempData["Message"] += $"Maximale Anzahl an ToDo-Listen erreicht! Um mehr ToDo-Listen speichern zu koennen, upgraden Sie bitte auf einen Premium-Account. ";
                                return RedirectToAction("Index");
                            }
                        }
                        else
                        {
                            TempData["Message"] += "Eintrag wurde nicht gespeichert! UserId nicht gefunden. ";
                            return RedirectToAction("CreateTodo");
                        }
                    }
                    else
                    {
                        TempData["Message"] += "Nur eingeloggte Benutzer koennen eine ToDo-Liste erstellen! ";
                        return RedirectToAction("Logon", "Auth");
                    }
                }
            }
            catch (ArgumentNullException)
            {
                //Exception bei leerer Eingabe
                TempData["Message"] += "Es muss eine Beschreibung angegeben werden! ";
                return RedirectToAction(nameof(Index));
            }
        }

        //Mehr zeigen
        [HttpGet]
        public async Task<IActionResult> TodoDetails(int todoId)
        {
            var todo = await _todoService.GibTodoVonIdAsync(todoId);

            //Hol Tags
            var tags = await _tagService.GibAlleTagsVonTodoId(todoId);
            
            //Check max Aufgaben für Tier
            int maxTasks = 0;
            if (User.IsInRole("FreeTier"))
            {
                maxTasks = 20;
            }
            if (User.IsInRole("PremiumTier"))
            {
                maxTasks = 1000;
            }
            //Und gib Rest
            int anzRestTasks = await _aufgabenService.GibAnzahlRestTasks(todoId, maxTasks);
            
            if (todo != null)
            {
                //Schau ob User darf
                var email = User.Claims.FirstOrDefault(claim => claim.Type.Contains(ClaimTypes.Email))!;
                int? userId = await _authService.GibUserIdVonEmailAsync(email);
                if (!todo.UserId.Equals(userId))
                {
                    TempData["Message"] += "Zugriff verweigert! ";
                    return RedirectToAction("Index");
                }

                //Mach Vm
                TodoDetailsVm vm = new()
                {
                    Todo = todo,
                    Tags = tags,
                    Tasks = (IEnumerable<Aufgabe>)todo.TaskGroup,
                    DoneTasks = (IEnumerable<Aufgabe>)todo.TaskGroup
                        .Where(t => t.Erledigt)
                        .ToList(),
                    NumberOfTasksLeft = anzRestTasks
                };
                ViewBag.todoName = todo.TodoName;
                return View(vm);
            } else
            {
                TempData["Message"] += "ToDo-Liste nicht gefunden!";
                return RedirectToAction("Index");
            }
        }
        [HttpPost]
        public async Task<IActionResult> TodoDetails(Aufgabe task)
        {
            ArgumentNullException.ThrowIfNull(task);

            await _aufgabenService.MachTaskAndersAsync(task);
            TempData["Message"] += "Aufgabe wurde geaendert. ";

            return RedirectToAction("TodoDetails", new { task.TodoId });
        }

        //Mach anders
        [HttpPost]
        public async Task<IActionResult> RenameTodo(int todoId, string todoName)
        {
            var todo = await _todoService.GibTodoVonIdAsync(todoId);
            ArgumentNullException.ThrowIfNull(todo);

            if (string.IsNullOrEmpty(todoName) || todoName.Length > 30)
            {
                TempData["Message"] += "Listenname muss zwischen 1 und 30 Zeichen betragen! ";
                return RedirectToAction("TodoDetails", new { todoId });
            }

            todo.TodoName = todoName;
            await _todoService.MachTodoAndersAsync(todo);
            TempData["Message"] += "ToDo-Liste wurde umbenannt. ";

            return RedirectToAction("TodoDetails", new { todoId });
        }
        //Weghauen
        [HttpPost]
        public async Task<IActionResult> DeleteTodo(int todoId)
        {
            var todo = await _todoService.GibTodoVonIdAsync(todoId);
            ArgumentNullException.ThrowIfNull(todo);

            if (todo.TaskGroup.Any())
            {
                TempData["Message"] += "Bitte entfernen Sie vor dem loeschen alle Aufgaben der ToDo-Liste! ";
                return RedirectToAction("TodoDetails", new { todoId });
            }
            else
            {
                await _todoService.HauTodoWegAsync(todo);
                TempData["Message"] += "ToDo-Liste wurde geloescht! ";

                return RedirectToAction("Index");
            }
        }
        #endregion

        #region Tasks
        //Hau rein
        [HttpGet]
        public async Task<IActionResult> CreateTask(int todoId)
        {
            var userName = User.Identity?.Name;
            //ArgumentNullException.ThrowIfNull(userName);
            int? userId = null;

            if (userName != null)
            {
                userId = await _authService.GibUserIdVonNameAsync(userName);
            }
            else
            {
                var email = User.Claims.FirstOrDefault(claim => claim.Type.Contains(ClaimTypes.Email));
                userId = await _authService.GibUserIdVonEmailAsync(email!);
            }
            var todo = await _todoService.GibTodoVonIdAsync(todoId);
            ArgumentNullException.ThrowIfNull(todo);

            ////Anzahl Aufgaben User
            //int maxTasks = 0;
            //if (User.IsInRole("FreeTier"))
            //{
            //    maxTasks = 20;
            //}
            //if (User.IsInRole("PremiumTier"))
            //{
            //    maxTasks = 1000;
            //}
            //ViewBag.anzRestTasks = await _aufgabenService.GibAnzahlRestTasks(todoId, maxTasks);

            ViewBag.UserId = userId;
            ViewBag.TodoId = todoId;
            ViewBag.TodoName = todo.TodoName;
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> CreateTask(Aufgabe task)
        {
            ArgumentNullException.ThrowIfNull(task);
            

            //Hau Tags rein !!! AUCH BEI EditTask !!!
            if (!task.Beschreibung.IsNullOrEmpty())
            {
                //Datum = Jetzt
                task.ErstelltDatum = DateTime.Now;

                task = _aufgabenService.MachErledigtOderUnerledigt(task);
                if (task.ErledigtDatum != null) task.Erledigt = true;

                int userId;
                //Check Anzahl max Tasks für User
                if (User.Identity!.Name != null)
                {
                    userId = (int)await _authService.GibUserIdVonNameAsync(User.Identity.Name.ToString());
                } else
                {
                    var email = User.Claims
                        .FirstOrDefault(claim => claim.Type.Contains(ClaimTypes.Email));
                    userId = (int)await _authService.GibUserIdVonEmailAsync(email!);

                }
                int maxTasks = await _aufgabenService.GibAnzahlMaxTasksVonUserIdAsync(userId);

                if (maxTasks <= 0)
                {
                    TempData["Message"] += "Maximale Anzahl an Aufgaben erreicht. Erwerben Sie einen Premium-Account um 1.000 Aufgaben in einer Liste speichern zu koennen. ";
                    return RedirectToAction("TodoDetails", new { task.TodoId });
                }

                //Schau nach Tags
                var vorhTags = _tagService.GibTagsVonTaskBeschreibung(task.Beschreibung);

                string tagString = "";
                string failTags = "Keine Tags";

                if (!vorhTags.IsNullOrEmpty())
                {
                    foreach (var t in vorhTags!)
                    {
                        //Schau ob da
                        if (await _tagService.SchauObGibtsTagVonUserAsync(task.UserId, t))
                        {
                            //Wording
                            if (failTags != "Keine Tags")
                            {
                                failTags += t + ", ";
                            }
                            else
                            {
                                failTags = t + ", ";
                            }
                        }
                        else
                        {
                            //Schau ob Tag passt rein
                            if (t.Length > 10)
                            {
                                TempData["Message"] += "Tag-Name darf maximal 10 zeichen betragen! ";
                                return RedirectToAction("CreateTask", task);
                            }

                            //Standardfarbe festlegen und hexen
                            Color color = Color.Pink;
                            string pinkHex = $"#{color.R:X2}{color.G:X2}{color.B:X2}";


                            //Tags dazu
                            task.Tags.Add(new Tag
                            {
                                TagName = t.ToString(),
                                Aufgabe = task,
                                ColorCode = pinkHex
                            });
                            tagString += t.ToString() + ", ";
                        }
                    }
                    TempData["Message"] += $"Sie haben den/die Tag(s) {tagString} registriert. ";
                    TempData["Message"] += $"{failTags} war(en) davon bereits vorhanden. ";
                }

                //Hau Aufgabe rein
                await _aufgabenService.HauTaskEiniAsync(task);

                //Gib Message und weiter
                TempData["Message"] += "Aufgabe wurde erstellt. ";
                return RedirectToAction("TodoDetails", new { task.TodoId });
            }
            else
            {
                TempData["Message"] += "Ungueltige Beschreibung! ";
                return RedirectToAction("CreateTask", new { task.TodoId });
            }
        }

        //Mach anders
        [HttpGet]
        public async Task<IActionResult> EditTask(int taskId)
        {
            
            var email = User.Claims.FirstOrDefault(claim => claim.Type.Contains(ClaimTypes.Email))!;
            int? userId = await _authService.GibUserIdVonEmailAsync(email);
            var task = await _aufgabenService.GibTaskVonIdAsync(taskId);
            
            if (task != null)
            {
                //Check ob User darf
                if (!task.UserId.Equals(userId))
                {
                    TempData["Message"] += "Zugriff verweigert! ";
                    return RedirectToAction("Index");
                }

                var todo = await _todoService.GibTodoVonIdAsync(task.TodoId);
                ArgumentNullException.ThrowIfNull(todo);

                var allTodos = await _todoService.GibAlleUserTodosAsync(userId);
               
                ViewBag.AllTodos = allTodos.Select(todo => new SelectListItem
                {
                    Value = todo.TodoId.ToString(),
                    Text = todo.TodoName  // Or whatever property represents the display name
                }).ToList();

                ViewBag.TodoName = todo.TodoName;
                

                return View(task);
            }
            return RedirectToAction("Index");
        }
        [HttpPost]
        public async Task<IActionResult> EditTask(Aufgabe task)
        {
            ArgumentNullException.ThrowIfNull(task);

            //Do or undo
            task = _aufgabenService.MachErledigtOderUnerledigt(task);
 
            //Hau raus wenn erledigt
            if (task.Erledigt)
            {
                TempData["Message"] += "Nur offene Aufgaben koennen bearbeitet werden! ";
                return RedirectToAction("TodoDetails", new { task.TodoId });
            }

            //Schau nach Tags
            var vorhTags = _tagService.GibTagsVonTaskBeschreibung(task.Beschreibung);

            //Hau Tags rein
            if (!vorhTags.IsNullOrEmpty())
            {
                string tagString = "";
                string failTags = "Keine";

                foreach (var t in vorhTags!)
                {
                    if (await _tagService.SchauObGibtsTagVonUserAsync(task.UserId, t))
                    {
                        if (failTags != "Keine")
                        {
                            failTags += t + ", ";
                        }
                        else
                        {
                            failTags = t + ", ";
                        }
                    }
                    else
                    {
                        Color color = Color.Pink;
                        string pinkHex = $"#{color.R:X2}{color.G:X2}{color.B:X2}";

                        task.Tags.Add(new Tag
                        {
                            TagName = t.ToString(),
                            Aufgabe = task,
                            ColorCode = pinkHex
                        });
                        tagString += t.ToString() + ", ";
                    }
                }
                TempData["Message"] += $"Sie haben den/die Tag(s) {tagString} registriert. ";
                TempData["Message"] += $"{failTags} war(en) davon bereits vorhanden. ";
            }
            //Check neu erledigt
            if (!task.Erledigt && task.ErledigtDatum.HasValue)
            {
                task.Erledigt = true;
            }
            //Neuen Task reinhauen
            await _aufgabenService.MachTaskAndersAsync(task);
            TempData["Message"] += "Aufgabe wurde geaendert. ";
            return RedirectToAction("TodoDetails", new { task.TodoId });
        }
        //Prio raufhaun
        [HttpPost]
        public async Task<IActionResult> IncreaseTaskPriority(int aufgabeId)
        {
            var task = await _aufgabenService.GibTaskVonIdAsync(aufgabeId);
            ArgumentNullException.ThrowIfNull(task);

            task.Priorität++;

            await _aufgabenService.MachTaskAndersAsync(task);

            return RedirectToAction("TodoDetails", new { task.TodoId });
        }
        //Fertig machen
        [HttpPost]
        public async Task<IActionResult> DoOrUndoTask(int aufgabeId)
        {
            var task = await _aufgabenService.GibTaskVonIdAsync(aufgabeId);
            ArgumentNullException.ThrowIfNull(task);

            if (!task.Erledigt)
            {
                task.Erledigt = true;
                task.ErledigtDatum = DateTime.Now;
            } else if (task.Erledigt)
            {
                task.Erledigt = false;
                task.ErledigtDatum = null;
            }

            await _aufgabenService.MachTaskAndersAsync(task);

            return RedirectToAction("TodoDetails", new { task.TodoId });
        }

        //Weghauen
        [HttpGet]
        public async Task<IActionResult> DeleteTask(int taskId)
        {
            var task = await _aufgabenService.GibTaskVonIdAsync(taskId);

            return View(task);
        }
        [HttpPost]
        public async Task<IActionResult> DeleteTask(Aufgabe task)
        {
            ArgumentNullException.ThrowIfNull(task);
            int todoId = task.TodoId;

            await _aufgabenService.HauTaskWegAsync(task);

            TempData["Message"] += "Aufgabe wurde geloescht. ";
            return RedirectToAction("TodoDetails", new { todoId });
        }
        //Obsolet?
        //[HttpGet]
        //public async Task<IActionResult> DeleteAllDoneTodoTasks(int todoId)
        //{
        //    var doneTasks = await _aufgabenService.GibAlleFertigTasksVonTodoAsync(todoId);


        //    //Sonst gemma
        //    var todo = await _todoService.GibTodoVonIdAsync(todoId);
        //    ArgumentNullException.ThrowIfNull(todo);

        //    ViewBag.todoId = todoId;
        //    ViewBag.todoName = todo.TodoName;
        //    return View(doneTasks);
        //}
        [HttpPost]
        public async Task<IActionResult> DeleteAllDoneTodoTasks(int todoId, IEnumerable<Aufgabe> aufgabes)
        {
            var doneTasks = await _aufgabenService.GibAlleFertigTasksVonTodoAsync(todoId);
            
            //Wenn keine dann raus
            if (!doneTasks.Any())
            {
                TempData["Message"] += "Es sind keine erledigten Aufgaben in dieser ToDo-Liste! ";

                return RedirectToAction("TodoDetails", new { todoId });
            }
            
            await _aufgabenService.HauDieseTasksWegAsync(doneTasks);
            TempData["Message"] += "Alle erledigten Aufgaben dieser Liste wurden geloescht. ";

            return RedirectToAction("TodoDetails", new {todoId} );
        }
        #endregion

        #region Tags
        //Reinhaun
        [HttpGet]
        public IActionResult CreateTag(int aufgabeId)
        {
            return View(aufgabeId);
        }
        public async Task<IActionResult> CreateTag(int aufgabeId, int todoId, string tagName, string colorCode)
        {
            var task = await _aufgabenService.GibTaskVonIdAsync(aufgabeId);
            
            //Tag schon da?
            bool gibtsTag4User = await _tagService.SchauObGibtsTagVonUserAsync(task!.UserId, tagName);
            
            //Anzahl Tags pro User
            int anzUserTags = await _tagService.GibAnzahlUserTagsAsync(task!.UserId);

            //Check und sag was
            if (string.IsNullOrEmpty(tagName) || tagName.Length > 10)
            {
                TempData["Message"] += "Tag-Name muss angegeben werden und darf maximal 10 Zeichen betragen! ";
                return RedirectToAction("TodoDetails", new { todoId });
            } else if (anzUserTags >= 20)
            {
                TempData["Message"] += "Maximale Anzahl an Tags erreich! Der Tag wurde nicht gespeichert. ";
                return RedirectToAction("TodoDetails", new { todoId } );
            }
            {
                //Mach
                Tag tag = new()
                {
                    AufgabeId = aufgabeId,
                    TagName = tagName,
                    ColorCode = colorCode
                };

                //Gibts nicht?
                if (!gibtsTag4User)
                {
                    //Reinhaun
                    await _tagService.HauTagReinAsync(tag);
                    TempData["Message"] += "Tag wurde gespeichert. ";
                } else
                {
                    TempData["Message"] += "Dieser Tag wird bereits verwendet! ";
                }

                return RedirectToAction("TodoDetails", new { todoId });
            }
        }

        //Mach anders
        [HttpPost]
        public async Task<IActionResult> EditTag(int tagId, int todoId, string tagName, string colorCode)
        {
            var tag = await _tagService.GibTagVonIdAsync(tagId);

            //Check
            if (string.IsNullOrEmpty(tagName))
            {
                TempData["Message"] += "Tag-Name muss angegeben werden! ";
                return RedirectToAction("TodoDetails", new { todoId });
            }
            else
            {
                ArgumentNullException.ThrowIfNull(tag);

                if (!tag.TagName.Equals(tagName))
                {
                    tag.TagName = tagName;
                } else if ((tag.ColorCode != null && !tag.ColorCode.Equals(colorCode)) || tag.ColorCode == null)
                {
                    tag.ColorCode = colorCode;
                }
                
                await _tagService.MachTagAndersAsync(tag);
                return RedirectToAction("TodoDetails", new { todoId });
            }
        }

        //Weghaun
        [HttpPost]
        public async Task<IActionResult> DeleteTag(int todoId, int tagId)
        {
            await _tagService.HauTagWegVonIdAsync(tagId);
            TempData["Message"] += "Tag wurde geloescht! ";
            return RedirectToAction("TodoDetails", new { todoId });
        }
        #endregion
    }
}
