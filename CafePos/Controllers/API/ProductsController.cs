using Microsoft.AspNetCore.Mvc;

namespace CafePos.Controllers.API
{
    public class ProductsController : Controller
    {
        public IActionResult Index()
        {
            return View();
        }
    }
}
