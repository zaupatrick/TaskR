using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TaskR.Data;
using TaskR.Enums;
using TaskR.Models;
using TaskR.Services;

namespace TaskR.Controllers
{
    [Authorize(Roles = "Admin")]
    public class AdminController : Controller
    {
        private readonly AuthService _authService;
        public AdminController(AuthService authService)
        {
            _authService = authService;
        }

        public IActionResult Index()
        {
            return View();
        }

        public async Task<IActionResult> BenutzerBrett()
        {
            BenutzerBrettVm vm = new()
            {
                Benutzers = await _authService.GibAlleBenutzersAsync()
                
            };

            vm.Admins = vm.Benutzers
                .Where(b => b.UserRole == UserRole.Admin);
            vm.PremiumUsers = vm.Benutzers
                .Where(b => b.UserRole == UserRole.PremiumTier);
            vm.FreeUsers = vm.Benutzers
                .Where(b => b.UserRole == UserRole.FreeTier);
            

            if (vm.Benutzers == null)
            {
                TempData["Message"] += "Keine Benutzer gefunden! ";
            } else if (vm.Admins == null)
            {
                TempData["Message"] += "Kein Admin gefunden! ";
            }

            return View(vm);
        }

        //Mach anders
        [HttpGet]
        [Route("Admin/Edit/{UserId}")]
        public async Task<IActionResult> Edit(int userId)
        {
            var user = await _authService.GibUserVonIdAsync(userId);
            return View(user);
        }
        [HttpPost]
        public async Task<IActionResult> Edit(AppUser user)
        {
            if (user == null)
            {
                TempData["Message"] += "User nicht gefunden! ";
                return RedirectToAction("Index");
            }
            //Admin neu?
            bool adminneu = false;
            if (user.UserRole == UserRole.Admin)
            {
                adminneu = await _authService.SchauObAdminNeuAsync(user);
            }
            //User kärchern for Admin
            if (adminneu)
            {
                await _authService.KaercherUserKontoAsync(user);
            }

            //Min 1 Admin
            bool gibtsAdmin = await _authService.SchauObGibtAdminAsync(user);
            if (!gibtsAdmin)
            {
                TempData["Message"] += "Es muss min. ein Admin vorhanden sein! ";
                return RedirectToAction("Benutzerbrett");
            }
            await _authService.MachBenutzerAndersAsync(user);
            return RedirectToAction("Benutzerbrett");
        }

        //Weghauen
        [Route("Admin/Delete/{UserId}")]
        public async Task<IActionResult> Delete(int userId)
        {
            var user = await _authService.GibUserVonIdAsync(userId);
            if (user != null) { return View(user); }
            return RedirectToAction("Benutzerbrett");
        }
        [HttpPost]
        [Route("Admin/Delete/{UserId}")]
        public async Task<IActionResult> Delete(AppUser user)
        {
            if (user == null)
            {
                TempData["Message"] += "User nicht gefunden! ";
                return RedirectToAction("Index");
            }

            //Min 1 Admin
            bool gibtsAndereAdmins = await _authService.SchauObGibtAdminAsync(user);

            if (!gibtsAndereAdmins)
            {
                TempData["Message"] += "Es muss min. ein Admin vorhanden sein! ";
                return RedirectToAction("Benutzerbrett");
            }

            //Weghaun
            TempData["Message"] += $"Der Benutzer {user.EMail} wurde geloescht! ";
            await _authService.HauUserWegAsync(user);
            return RedirectToAction("Benutzerbrett");
        }
    }
}
