using Microsoft.AspNetCore.Mvc;

namespace CafePos.Controllers.API
{
    public class OrdersController : Controller
    {
        public IActionResult Index()
        {
            return View();
        }
    }
}
