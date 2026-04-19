using LedApp.Data;
using LedApp.Hubs;
using LedApp.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using System.Diagnostics;

namespace LedApp.Controllers
{
    public class HomeController : Controller
    {
        private readonly ILogger<HomeController> _logger;
        private readonly ApplicationDBContext _context;
        private readonly IHubContext<SignalServer> _signalrHub;
      
        public HomeController(ILogger<HomeController> logger, ApplicationDBContext context, IHubContext<SignalServer> signalrHub)
        {
            _logger = logger;
            _context = context;
            _signalrHub = signalrHub;
         
        }

        public IActionResult Index()
        {
            return View();
        }
        public IActionResult cuaxuat(int id)
        {
            try
            {
                var cuaxuat = _context.CuaXuats.Where(s => s.Id == id).FirstOrDefault();
                if (cuaxuat != null)
                {
                    ViewBag.tencuaxuat = cuaxuat.Ten;
                   
                }
                else
                {
                    ViewBag.ms = "Mã cửa sổ không tồn tại";
                }
                ViewBag.id = id;
                ViewBag.cuaxuatw = _context.CauHinhs.Where(s => s.Key.Equals("CuaXuatW")).FirstOrDefault().Value;
                ViewBag.cuaxuath = _context.CauHinhs.Where(s => s.Key.Equals("CuaXuatH")).FirstOrDefault().Value;
            }
            catch (Exception)
            {
                ViewBag.ms = "Lỗi truy xuất dữ liệu";
            }
            return View();
        }
        public IActionResult tonghopxuat()
        {
            ViewBag.THXuatTen = _context.CauHinhs.Where(s => s.Key.Equals("THXuatTen")).FirstOrDefault().Value;
            ViewBag.THXuatW = _context.CauHinhs.Where(s => s.Key.Equals("THXuatW")).FirstOrDefault().Value;
            ViewBag.THXuatH = _context.CauHinhs.Where(s => s.Key.Equals("THXuatH")).FirstOrDefault().Value;

           
            return View();
        }
        public IActionResult cuanhap(int id)
        {
            if (id == null || _context.CuaNhaps == null)
            {
                ViewBag.ms = "Cửa nhập không tồn tại";
            }
            else
            {
                var cuaNhap = _context.CuaNhaps.Find(id);
                if (cuaNhap == null)
                {
                    ViewBag.ms = "Cửa nhập không tồn tại"; ;
                }
                else
                {
                    ViewBag.tencuanhap = _context.CuaNhaps.Find(id).Ten;
                }
            }
            ViewBag.id = id;
            ViewBag.cuanhapw = _context.CauHinhs.Where(s => s.Key.Equals("CuaNhapW")).FirstOrDefault().Value;
            ViewBag.cuanhaph = _context.CauHinhs.Where(s => s.Key.Equals("CuaNhapH")).FirstOrDefault().Value;
            return View();
        }
        public IActionResult tonghopnhap()
        {
            ViewBag.THNhapTen = _context.CauHinhs.Where(s => s.Key.Equals("THNhapTen")).FirstOrDefault().Value;
            ViewBag.THNhapW = _context.CauHinhs.Where(s => s.Key.Equals("THNhapW")).FirstOrDefault().Value;
            ViewBag.THNhapH = _context.CauHinhs.Where(s => s.Key.Equals("THNhapH")).FirstOrDefault().Value;
            return View();
        }
        //[HttpGet]
        //public async Task<IActionResult> GetXeXuat()
        //{
        //    var res = _context.xeXuats.Where(s => s.BienSo.Equals("29KT123.45")).ToList();
        //    return Ok(res);
        //}
        public IActionResult ManHinh()
        {
            return View();
        }
        public IActionResult Privacy()
        {
            return View();
        }
        public IActionResult tongtrungtam()
        {
            var cuaNhaps = _context.CuaNhaps.ToList();
            var cuaXuats = _context.CuaXuats.ToList();

            ViewBag.CuaNhaps = cuaNhaps;
            ViewBag.CuaXuats = cuaXuats;

            // Kích thước bảng LED6: 2944x896
            ViewBag.W = _context.CauHinhs
                .Where(c => c.Key == "Led6W")
                .Select(c => c.Value)
                .FirstOrDefault() ?? "2944";

            ViewBag.H = _context.CauHinhs
                .Where(c => c.Key == "Led6H")
                .Select(c => c.Value)
                .FirstOrDefault() ?? "896";

            return View();
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }
}