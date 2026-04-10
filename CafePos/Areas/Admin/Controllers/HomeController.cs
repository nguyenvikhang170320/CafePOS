using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;

namespace CafePos.Areas.Admin.Controllers
{
    [Area("Admin")] // CỰC KỲ QUAN TRỌNG: Phải khai báo Area ở đây
    [Authorize(Roles = "Admin")] // Chặn luôn: Chỉ ai đăng nhập quyền Admin mới vào được
    public class HomeController : Controller
    {
        public IActionResult Index()
        {
            return View();
        }
    }
}