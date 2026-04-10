using Microsoft.AspNetCore.Mvc;

namespace CafePos.Controllers.API
{
    public class AuthController : Controller
    {
        public IActionResult Index()
        {
            return View();
        }
    }
}
