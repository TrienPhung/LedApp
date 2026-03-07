using LedApp.Repositories;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace LedApp.Controllers
{
    [Authorize]
    public class AdministratorController : Controller
    {
        private readonly UserRepository userRepo;

        public AdministratorController(UserRepository userRepo)
        {
            this.userRepo = userRepo;
        }

        public IActionResult Index()
        {
            //if (HttpContext.Session.GetString("Username") == null || HttpContext.Session.GetString("Username")=="")
            //{
            //    return RedirectToAction("Login");
            //}    
            return View();
        }
        public IActionResult Login()
        {
            return View();
        }
        [HttpPost]
        public async Task<IActionResult> Login(string tendangnhap, string matkhau)
        {
            var userFromDb = await userRepo.GetNguoiDung(tendangnhap, matkhau);

            if (userFromDb == null)
            {
                ModelState.AddModelError("Login", "Invalid credentials");
                return View();
            }

            HttpContext.Session.SetString("Username", userFromDb.Username);

            return RedirectToAction("Index", "Administrator");
        }
        public ActionResult LogOut()
        {
            HttpContext.Session.Remove("Username");
            return RedirectToAction(nameof(Login));
        }
    }
}
