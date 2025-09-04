using Microsoft.AspNetCore.Mvc;

namespace Dashboard.Controllers
{
    public class QAController : Controller
    {
        public IActionResult Index()
        {
            return View();
        }
    }
}
