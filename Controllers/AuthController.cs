using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using TaskR.Enums;
using TaskR.Models;
using TaskR.Services;
using Task = System.Threading.Tasks.Task;

namespace TaskR.Controllers
{
    public class AuthController : Controller
    {
        private readonly AuthService _auth;
        public AuthController(AuthService auth)
        {
            _auth = auth;
        }

        #region Einloggen
        public IActionResult Logon()
        {
            return View();
        }
        [HttpPost]
        public async Task<IActionResult> Logon(string email, string passwort)
        {
            if (string.IsNullOrEmpty(email) || string.IsNullOrEmpty(passwort))
            {
                TempData["Message"] += "E-Mail-Adresse und Passwort angeben! ";
            }
            //Gibts User?
            var dbUser = await _auth.GibUserWennKannEini(email, passwort);

            //MachAnders um LastLogin zu übernehmen
            if (dbUser != null)
            {
                await _auth.MachBenutzerAndersAsync(dbUser);
            }

            if (dbUser == null)
            {
                TempData["Message"] += "Login-Daten sind nicht korrekt!";
                return RedirectToAction("Logon", "Auth");
            }

            //MachLogin
            await LassUserEiniAsync(dbUser.EMail, dbUser.Username, dbUser.UserId, dbUser.UserRole);

            //Weiterleitung je nach Rolle - JA das ist eine Switch.
            return dbUser.UserRole switch
            {
                UserRole.Admin => RedirectToAction("Index", "Admin"),
                UserRole.FreeTier => RedirectToAction("Index", "FreeUser"),
                UserRole.PremiumTier => RedirectToAction("Index", "FreeUser"),
                UserRole.None => RedirectToAction("Index", "FreeUser"),
                /* cooles default */ _ => RedirectToAction("Index", "Home"), 
            };
        }
        [NonAction]
        private async Task LassUserEiniAsync(string eMail, string? username, int userId, UserRole userRole)
        {
            //Claims erstellen
            var emailClaim = new Claim(ClaimTypes.Email, eMail);
            var userClaims = new List<Claim>();

            if (username != null)
            {
                var nameClaim = new Claim(ClaimTypes.Name, username); 
                var userIdClaim = new Claim("userid", userId.ToString());
                var roleClaim = new Claim(ClaimTypes.Role, userRole.ToString());
                //Claims sammeln
                userClaims = new List<Claim>() { emailClaim, nameClaim, userIdClaim, roleClaim };
            } else
            {
                var userIdClaim = new Claim("userid", userId.ToString());
                var roleClaim = new Claim(ClaimTypes.Role, userRole.ToString());
                //Claims sammeln ohne Username
                userClaims = new List<Claim>() { emailClaim, userIdClaim, roleClaim };
            }

            //Identity erstellen
            var claimsIdentity = new ClaimsIdentity(userClaims, CookieAuthenticationDefaults.AuthenticationScheme);
            //Principal erstellen
            var claimsPrincipal = new ClaimsPrincipal(claimsIdentity);
            //Principal einloggen
            await HttpContext.SignInAsync(claimsPrincipal);
        }
        #endregion

        #region Ausloggen
        public async Task<IActionResult> Logout()
        {
            await HttpContext.SignOutAsync();
            return RedirectToAction("Logon", "Auth");
        }
        #endregion

        #region Registrieren
        public IActionResult Register()
        {
            return View();
        }
        [HttpPost]
        public async Task<IActionResult> Register(RegAppUserVm vm)
        {
            try
            {
                //Check ob Email da
                if (string.IsNullOrWhiteSpace(vm.Email) || string.IsNullOrWhiteSpace(vm.PasswortString)) 
                {
                    throw new ArgumentException("Es muessen eine E-Mail-Adresse und ein Passwort angegeben werden."); 
                }

                //Passwortvergleich
                if (!vm.PasswortString.Equals(vm.PasswortString2)) { throw new ArgumentException("Passwortwiederholung stimmt nicht ueberein!"); }

                //User+E-Mail unique
                //Username-Güte
                bool userDa = false;
                bool usernameGut = true;

                //Schau ob Username da und gut; und gib Status
                if (vm.Username != null)
                {
                    userDa = await _auth.SchauObsUserGibtAsync(vm.Username);
                    usernameGut = await _auth.SchauObUsernameGutAsync(vm.Username);
                }
                bool emailGut = await _auth.SchauObEmailFreiAsync(vm.Email);

                //Fehler werfen
                if (userDa) { throw new ArgumentException("User bereits vorhanden!"); }
                if (!emailGut) { throw new ArgumentException("E-Mail-Adresse wird bereits verwendet!"); }
                if (!usernameGut) { throw new ArgumentException("Username muss zwischen 2 und 50 Zeichen haben!"); }
                
                //Sonst in DB reinhauen
                await _auth.HauUserReinAsync(vm);
            } catch (ArgumentException e)
            {
                //Fehler-Message geben
                TempData["Message"] += e.Message.ToString();
                return RedirectToAction("Register", "Auth");
            }
            
            return RedirectToAction("Logon");
        }
        #endregion
    }
}
