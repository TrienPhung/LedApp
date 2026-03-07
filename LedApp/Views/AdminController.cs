using Microsoft.AspNetCore.Mvc;

namespace LedApp.Views
{
    public class AdminController : Controller
    {
        public IActionResult Index()
        {
            return View();
        }
    }
}
