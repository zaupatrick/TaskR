using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using TaskR.Data;
using TaskR.Enums;
using TaskR.Models;
using Task = System.Threading.Tasks.Task;

namespace TaskR.Services
{
    public class AuthService
    {
        #region Dependency Injection
        private readonly MyDbContext _ctx;
        public AuthService(MyDbContext ctx)
        {
            _ctx = ctx;
        }
        #endregion
        #region Prüfen
        public async Task<bool> SchauObsUserGibtAsync(string username)
                {
                    if (username != null)
                    {
                        //User schon da?
                        bool gibtsUser = await _ctx.AppUsers
                                .Where(u => u.Username == username)
                                .AnyAsync();
                        return gibtsUser;
                    }
                    return false;
                }

        public async Task<bool> SchauObUsernameGutAsync(string username)
        {
            ArgumentNullException.ThrowIfNull(username);

            //Gibts User?
            if (await SchauObsUserGibtAsync(username))
            {
                return false;
            }

            //Username min 3 max 50 Zeichen
            if (username.Length < 3 || username.Length > 50)
            {
                return false;
            }
            return true;
        }
        public async Task<bool> SchauObEmailFreiAsync(string email)
        {
            //Email unique
            if ( await _ctx.AppUsers
                .Where(u => u.EMail == email)
                .AnyAsync())
            {
                return false;
            }
            return true;
        }
        #endregion
        #region Passwort
        private byte[] PasswortHaschen(byte[] gesalzen)
        {
            return SHA256.Create().ComputeHash(gesalzen);
        }

        private byte[] SalzString(string passwort, byte[] salz, Encoding encoding)
        {
            return encoding
                .GetBytes(passwort)
                .Concat(salz)
                .ToArray();
        }

        private byte[] MachSalz()
        {
            byte[] salz = new byte[256 / 8];
            RandomNumberGenerator.Fill(salz);
            return salz;
        }
        #endregion
        #region Login
        public async Task<AppUser?> GibUserWennKannEini(string email, string passwort)
        {
            //Todo: Anonyme Benutzer implementieren
            var dbUser = await _ctx.AppUsers
                .Where(u => u.EMail == email)
                .FirstOrDefaultAsync();

            //Gibts User?
            if (dbUser == null) { return null; }

            //Darf User?
            var loginGesalzen = SalzString(passwort, dbUser.Salt, Encoding.UTF8);
            var hasch = PasswortHaschen(loginGesalzen);
            if (hasch.SequenceEqual(dbUser.PasswordHash))
            {
                dbUser.LastLogon = DateTime.Now;
                return dbUser;
            }
            //sonst raus
            else return null;
        }

        public async Task<List<AppUser>> GibAlleBenutzersAsync()
        {
            var list = await _ctx.AppUsers
                .Include(u => u.TodoList!)
                .ThenInclude(l => l.TaskGroup)
                .ThenInclude(t => t.Tags)
                .ToListAsync();

            if (list == null)
            {
                #pragma warning disable CS8603 // Mögliche Nullverweisrückgabe.
                return list;
                #pragma warning restore CS8603 // Mögliche Nullverweisrückgabe.
                //throw new NullReferenceException("Keine Benutzer gefunden! ");
            }
            return list;
        }
        #endregion
        #region User-CRUD-Aktionen
        //Hau und Mach!
        public async Task HauUserReinAsync(RegAppUserVm vm)
        {
            //Passwort haschen
            var salz = MachSalz();
            var gesalzen = SalzString(vm.PasswortString, salz, System.Text.Encoding.UTF8);
            var hasch = PasswortHaschen(gesalzen);

            //Userrole
            UserRole userRole = UserRole.None;
            if (!_ctx.AppUsers.Any()) { userRole = UserRole.Admin; } else { userRole = UserRole.FreeTier; }

            //AppUser mappen
            AppUser user = new AppUser()
            {
                Username = vm.Username,
                EMail = vm.Email,
                Salt = salz,
                PasswordHash = hasch,
                RegisteredOn = DateTime.Now,
                UserRole = userRole
            };

            //Reinhauen
            await _ctx.AppUsers.AddAsync(user);
            await _ctx.SaveChangesAsync();
        }
        public async Task HauUserWegAsync(AppUser user)
        {
            if (user != null)
            {
                _ctx.AppUsers.Remove(user);
                await _ctx.SaveChangesAsync();
            }
        }
        public async Task MachBenutzerAndersAsync(AppUser user)
        {
            if (user != null)
            {
                var dbUser = await _ctx.AppUsers
                    .FirstOrDefaultAsync(u => u.UserId == user.UserId);

                if (dbUser != null)
                {
                    // Nur gewünschte Felder aktualisieren, Passwort nicht anfassen
                    dbUser.Username = user.Username;
                    dbUser.EMail = user.EMail;
                    dbUser.UserRole = user.UserRole;

                    await _ctx.SaveChangesAsync();
                }
            }
        }
        public async Task KaercherUserKontoAsync(AppUser user)
        {
            var tasks = await _ctx.Aufgaben
                            .Where(tk => tk.UserId == user.UserId)
                            .Include(tg => tg.Tags)
                            .ToListAsync();
            _ctx.Aufgaben.RemoveRange(tasks);
            
            var todos = await _ctx.Todos
                            .Where(td => td.UserId == user.UserId)
                            .Include(tk => tk.TaskGroup)
                            .ToListAsync();
            _ctx.Todos.RemoveRange(todos);
            await _ctx.SaveChangesAsync();
        }
        //Gib
        public async Task<AppUser?> GibUserVonIdAsync(int uid)
        {
            var user = await _ctx.AppUsers
                .Where(u => u.UserId == uid)
                .FirstOrDefaultAsync();
            return user;
        }
        public async Task<int?> GibUserIdVonNameAsync(string username)
        {
            if (username != null)
            {
                AppUser? user = await _ctx.AppUsers
                    .Where(u => u.Username == username)
                    .FirstOrDefaultAsync();
                return user?.UserId;
            } else
            {
                return null;
            }
        }
        public async Task<int?> GibUserIdVonEmailAsync(Claim email)
        {
            AppUser? user = await _ctx.AppUsers
                .Where(u => u.EMail == email.Value.ToString())
                .FirstOrDefaultAsync();
            ArgumentNullException.ThrowIfNull(user);
            return user.UserId;
        }
        //Schau
        public async Task<bool> SchauObGibtAdminAsync(AppUser user)
        {
            IEnumerable<AppUser> admins;
            //Dieser User Admin?
            //todo:debug
            if (user.UserRole != UserRole.Admin)
            {
                //Dann hol andere
                admins = await _ctx.AppUsers
                    .Where(u => u.UserRole == UserRole.Admin)
                    .Where(u => u.UserId != user.UserId)
                    .ToListAsync();
            } else
            {
                return true;
            }
            //Was da?
            if (admins.Any())
            {
                return true;
            }
            else
            {
                return false;
            }
        }
        public async Task<bool> SchauObAdminNeuAsync(AppUser user)
        {
            var dbuser = await _ctx.AppUsers
                .Where(u => u.UserId == user.UserId)
                .FirstOrDefaultAsync();
            if (dbuser!.UserRole != UserRole.Admin)
            {
                return true;
            }
            else
            {
                return false;
            }
        }

        #endregion

    }
}
